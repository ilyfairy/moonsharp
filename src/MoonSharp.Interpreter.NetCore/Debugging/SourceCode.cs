using System;
using System.Collections.Generic;
using System.Text;

namespace MoonSharp.Interpreter.Debugging
{
	/// <summary>
	/// Class representing the source code of a given script
	/// </summary>
	public class SourceCode : IScriptPrivateResource
	{
		/// <summary>
		/// Gets the name of the source code
		/// </summary>
		public string Name { get; private set; }
		/// <summary>
		/// Gets the source code as a string
		/// </summary>
		public ReadOnlyMemory<char> Code { get; private set; }
		/// <summary>
		/// Gets the source code lines.
		/// </summary>
		public ReadOnlyMemory<char>[] Lines { get; private set; }
		/// <summary>
		/// Gets the script owning this resource.
		/// </summary>
		public Script OwnerScript { get; private set; }
		/// <summary>
		/// Gets the source identifier inside a script
		/// </summary>
		public int SourceID { get; private set; }

		internal List<SourceRef> Refs { get; private set; }

		internal SourceCode(string name, ReadOnlyMemory<char> code, int sourceID, Script ownerScript)
		{
			Refs = new List<SourceRef>();

			List<ReadOnlyMemory<char>> lines = new();

			Name = name;
			Code = code;

			if (ownerScript.DebuggerEnabled)
			{
				lines.Add(string.Format("-- Begin of chunk : {0} ", name).AsMemory());
			}

			foreach (var range in Code.Span.Split('\n'))
			{
				lines.AddRange(Code[range]);
			}

			Lines = lines.ToArray();

			OwnerScript = ownerScript;
			SourceID = sourceID;
		}
	}
}
