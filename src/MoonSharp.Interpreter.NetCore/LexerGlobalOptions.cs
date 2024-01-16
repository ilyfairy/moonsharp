using MoonSharp.Interpreter.CoreLib.StringLib;

namespace MoonSharp.Interpreter;

public static class LexerGlobalOptions
{
    public static InvalidEscapeHandling IgnoreInvalid  { get; set; }

    public static UnexpectedSymbolHandling UnexpectedSymbolHandling { get; set; }

    public static int PatternMaxCalls { get => KopiLua_StringLib.MAXCCALLS; set => KopiLua_StringLib.MAXCCALLS = value; }
}

public enum InvalidEscapeHandling
{
    Throw = default,    // throw exception
    Ignore,    // ignore invalid escape character
    Keep      // keep invalid escape character
}

public enum UnexpectedSymbolHandling
{
    Throw = default,    // throw exception
    Ignore,    // ignore unexpected symbol
}
