﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Execution.VM;

namespace MoonSharp.Debugger
{
	public partial class MainForm : Form, IDebugger
	{
		List<string> m_Watches = new List<string>();

		public MainForm()
		{
			InitializeComponent();
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			m_Ctx = SynchronizationContext.Current;
			MoonSharpInterpreter.WarmUp();
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Title = "Load script";
			ofd.DefaultExt = "lua";
			ofd.Filter = "Lua files (*.lua)|*.lua|All files (*.*)|*.*";

			if (ofd.ShowDialog() == DialogResult.OK)
			{
				DebugScript(ofd.FileName);
				openToolStripMenuItem.Enabled = false;
			}
		}

		Script m_Script;
		SynchronizationContext m_Ctx;

		RValue Print(RValue[] values)
		{
			string prn = string.Join(" ", values.Select(v => v.AsString()).ToArray());
			Console_WriteLine("{0}", prn);
			return RValue.Nil;
		}

		RValue Assert(RValue[] values)
		{
			if (!values[0].TestAsBoolean())
				Console_WriteLine("ASSERT FAILED!");

			return RValue.Nil;
		}

		RValue XAssert(RValue[] values)
		{
			if (!values[1].TestAsBoolean())
				Console_WriteLine("ASSERT FAILED! : {0}", values[0].ToString());

			return RValue.Nil;
		}

		private void Console_WriteLine(string fmt, params object[] args)
		{
			fmt = string.Format(fmt, args);

			m_Ctx.Post(str =>
			{
				txtOutput.Text = txtOutput.Text + fmt.ToString() + "\n";
				txtOutput.SelectionStart = txtOutput.Text.Length - 1;
				txtOutput.SelectionLength = 0;
				txtOutput.ScrollToCaret();
			}, fmt);
		}


		private void DebugScript(string filename)
		{
			m_Script = MoonSharpInterpreter.LoadFromFile(filename);
			m_Script.AttachDebugger(this);

			Thread m_Debugger = new Thread(DebugMain);
			m_Debugger.Name = "Moon# Execution Thread";
			m_Debugger.IsBackground = true;
			m_Debugger.Start();

		}

		void IDebugger.SetSourceCode(Chunk byteCode, string[] code)
		{
			string[] source = new string[byteCode.Code.Count];

			for (int i = 0; i < byteCode.Code.Count; i++)
			{
				source[i] = string.Format("{0:X8}  {1}", i, byteCode.Code[i]);
			}

			codeView.SourceCode = source;
		}

		DebuggerAction m_NextAction;
		AutoResetEvent m_WaitLock = new AutoResetEvent(false);
		AutoResetEvent m_WaitBack = new AutoResetEvent(false);

		DebuggerAction IDebugger.GetAction(int ip)
		{
			m_Ctx.Post(o =>
			{
				codeView.ActiveLine = ip;
			}, null);

			m_WaitLock.WaitOne();

			DebuggerAction action = m_NextAction;
			m_NextAction = null;

			m_WaitBack.Set();

			return action;
		}

		void DebugAction(DebuggerAction action)
		{
			m_NextAction = action;
			m_WaitLock.Set();

			if (!m_WaitBack.WaitOne(1000))
				MessageBox.Show(this, "Operation timed out", "Timeout");
		}


		void DebugMain()
		{
			Table T = new Table();
			T["print"] = new RValue(new CallbackFunction(Print));
			T["assert"] = new RValue(new CallbackFunction(Assert));
			T["xassert"] = new RValue(new CallbackFunction(XAssert));

			try
			{
				m_Script.Execute(T);
			}
			catch (Exception ex)
			{
				Console_WriteLine("Guest raised unhandled CLR exception: {0}\n{1}\n", ex.GetType(), ex.ToString());
			}
		}

		private void StepIN()
		{
			DebugAction(new DebuggerAction() { Action = DebuggerAction.ActionType.StepIn });
		}

		private void StepOVER()
		{
			DebugAction(new DebuggerAction() { Action = DebuggerAction.ActionType.StepOver });
		}

		private void GO()
		{
			DebugAction(new DebuggerAction() { Action = DebuggerAction.ActionType.Run });
		}


		void IDebugger.Update(WatchType watchType, List<WatchItem> items)
		{
			if (watchType == WatchType.CallStack)
				m_Ctx.Post(UpdateCallStack, items);
			if (watchType == WatchType.Watches)
				m_Ctx.Post(UpdateWatches, items);
			if (watchType == WatchType.VStack)
				m_Ctx.Post(UpdateVStack, items);
		}
		void UpdateVStack(object o)
		{
			List<WatchItem> items = (List<WatchItem>)o;

			lvVStack.BeginUpdate();
			lvVStack.Items.Clear();

			foreach (var item in items)
			{
				lvVStack.Add(
					item.Address.ToString("X4"),
					(item.Value != null) ? item.Value.Type.ToString() : "(undefined)",
					(item.Value != null) ? item.Value.ToString() : "(undefined)"
					).Tag = item.Value;
			}

			lvVStack.EndUpdate();

		}


		void UpdateWatches(object o)
		{
			List<WatchItem> items = (List<WatchItem>)o;

			lvWatches.BeginUpdate();
			lvWatches.Items.Clear();

			foreach (var item in items)
			{
				lvWatches.Add(
					item.Name ?? "(???)",
					(item.Value != null) ? item.Value.Type.ToLuaTypeString() : "(undefined)",
					(item.Value != null) ? item.Value.ToString() : "(undefined)",
					(item.LValue != null) ? item.LValue.ToString() : "(undefined)"
					).Tag = item.Value;
			}

			lvWatches.EndUpdate();

		}

		void UpdateCallStack(object o)
		{
			List<WatchItem> items = (List<WatchItem>)o;

			lvCallStack.BeginUpdate();
			lvCallStack.Items.Clear();
			foreach (var item in items)
			{
				lvCallStack.Add(
					item.Address.ToString("X8"),
					item.Name ?? "(???)",
					item.RetAddress.ToString("X8"),
					item.BasePtr.ToString("X8")
					).Tag = item.Address;
			}

			lvCallStack.Add("---", "(main)", "---", "---");

			lvCallStack.EndUpdate();
		}




		List<string> IDebugger.GetWatchItems()
		{
			return m_Watches;
		}

		private void btnAddWatch_Click(object sender, EventArgs e)
		{
			string text = WatchInputDialog.GetNewWatchName();

			if (!string.IsNullOrEmpty(text))
			{
				m_Watches.AddRange(text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
				DebugAction(new DebuggerAction() { Action = DebuggerAction.ActionType.Refresh });
			}
		}

		private void btnRemoveWatch_Click(object sender, EventArgs e)
		{
			HashSet<string> itemsToRemove = new HashSet<string>(lvWatches.SelectedItems.OfType<ListViewItem>().Select(lvi => lvi.Text));

			int i = m_Watches.RemoveAll(w => itemsToRemove.Contains(w));

			if (i != 0)
				DebugAction(new DebuggerAction() { Action = DebuggerAction.ActionType.Refresh });
		}
		private void stepInToolStripMenuItem_Click(object sender, EventArgs e)
		{
			StepIN();
		}

		private void btnOpenFile_Click(object sender, EventArgs e)
		{
			openToolStripMenuItem.PerformClick();
		}

		private void stepOverToolStripMenuItem_Click(object sender, EventArgs e)
		{
			StepOVER();
		}

		private void toolGO_Click(object sender, EventArgs e)
		{
			GO();
		}

		private void gOToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GO();
		}
		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			StepIN();
		}

		private void toolStepOver_Click(object sender, EventArgs e)
		{
			StepOVER();
		}

		private void btnViewVStk_Click(object sender, EventArgs e)
		{
			ValueBrowser.StartBrowse(lvVStack.SelectedItems.OfType<ListViewItem>().Select(lvi => lvi.Tag).Cast<RValue>().FirstOrDefault());
		}

		private void lvVStack_MouseDoubleClick(object sender, MouseEventArgs e)
		{

		}

		private void btnViewWatch_Click(object sender, EventArgs e)
		{
			ValueBrowser.StartBrowse(lvWatches.SelectedItems.OfType<ListViewItem>().Select(lvi => lvi.Tag).Cast<RValue>().FirstOrDefault());
		}

		private void lvWatches_SelectedIndexChanged(object sender, EventArgs e)
		{
			ValueBrowser.StartBrowse(lvWatches.SelectedItems.OfType<ListViewItem>().Select(lvi => lvi.Tag).Cast<RValue>().FirstOrDefault());
		}


	}
}