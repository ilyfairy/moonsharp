﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoonSharp.Interpreter.DataStructs;

namespace MoonSharp.Interpreter.Execution.VM
{
	sealed partial class Processor
	{
		private void NilifyBlockData(Instruction I)
		{
			int from = I.NumVal;
			int to = I.NumVal2;

			var array = this.m_ExecutionStack.Peek().LocalScope;

			if (to >= 0 && from >= 0)
			{
				for (int i = from; i <= to; i++)
					array[i] = DynValue.NewNil();
			}
		}

		private void ClearBlockData(Instruction I)
		{
			int from = I.NumVal;
			int to = I.NumVal2;

			var array = this.m_ExecutionStack.Peek().LocalScope;

			if (to >= 0 && from >= 0)
			{
				Array.Clear(array, from, to - from + 1);
			}
		}


		public DynValue GetGenericSymbol(SymbolRef symref)
		{
			switch (symref.i_Type)
			{
				case SymbolRefType.Global:
					return GetGlobalSymbol(GetGenericSymbol(symref.i_Env), symref.i_Name);
				case SymbolRefType.Local:
					return m_ExecutionStack.Peek().LocalScope[symref.i_Index];
				case SymbolRefType.Upvalue:
					return m_ExecutionStack.Peek().ClosureScope[symref.i_Index];
				default:
					throw new InternalErrorException("Unexpected {0} LRef at resolution: {1}", symref.i_Type, symref.i_Name);
			}
		}

		private DynValue GetGlobalSymbol(DynValue dynValue, string name)
		{
			if (dynValue.Type != DataType.Table)
				throw new InvalidOperationException(string.Format("_ENV is not a table but a {0}", dynValue.Type));

			return dynValue.Table[name];
		}

		private void SetGlobalSymbol(DynValue dynValue, string name, DynValue value)
		{
			if (dynValue.Type != DataType.Table)
				throw new InvalidOperationException(string.Format("_ENV is not a table but a {0}", dynValue.Type));

			dynValue.Table[name] = value ?? DynValue.Nil;
		}


		public void AssignGenericSymbol(SymbolRef symref, DynValue value)
		{
			switch (symref.i_Type)
			{
				case SymbolRefType.Global:
					SetGlobalSymbol(GetGenericSymbol(symref.i_Env), symref.i_Name, value);
					break;
				case SymbolRefType.Local:
					{
						var stackframe = m_ExecutionStack.Peek();

						DynValue v = stackframe.LocalScope[symref.i_Index];
						if (v == null)
							stackframe.LocalScope[symref.i_Index] = v = DynValue.NewNil();

						v.Assign(value);
					}
					break;
				case SymbolRefType.Upvalue:
					{
						var stackframe = m_ExecutionStack.Peek();

						DynValue v = stackframe.ClosureScope[symref.i_Index];
						if (v == null)
							stackframe.ClosureScope[symref.i_Index] = v = DynValue.NewNil();

						v.Assign(value);
					}
					break;
				default:
					throw new InternalErrorException("Unexpected {0} LRef at resolution: {1}", symref.i_Type, symref.i_Name);
			}
		}


		public SymbolRef FindRefByName(string name)
		{
			var stackframe = m_ExecutionStack.Peek();

			if (stackframe.Debug_Symbols != null)
			{
				for (int i = stackframe.Debug_Symbols.Length - 1; i >= 0; i--)
				{
					var l = stackframe.Debug_Symbols[i];

					if (l.i_Name == name && stackframe.LocalScope[i] != null)
						return l;
				}
			}

			
			var closure = stackframe.ClosureScope;

			if (closure != null)
			{
				for (int i = 0; i < closure.Symbols.Length; i++)
					if (closure.Symbols[i] == name)
						return SymbolRef.Upvalue(name, i);
			}

			if (name != WellKnownSymbols.ENV)
			{
				SymbolRef env = FindRefByName(WellKnownSymbols.ENV);
				return SymbolRef.Global(name, env);
			}

			return null;
		}

	}
}