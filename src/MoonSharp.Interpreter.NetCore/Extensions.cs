using System.Collections.Generic;
using MoonSharp.Interpreter.Execution.VM;

namespace MoonSharp.Interpreter.NetCore;

internal static class Extensions
{
    private static readonly Dictionary<char, string> _charsCache = new();

    static Extensions()
    {
        for (char i = '\0'; i <= 256; i++)
        {
            _charsCache[i] = i.ToString();
        }
    }

    public static string CharToString(this char c)
    {
        return c switch
        {
            >= (char)0 and <= (char)256 => _charsCache[c],
            _ => c.ToString()
        };
    }
}
