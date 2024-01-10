namespace MoonSharp.Interpreter;

public static class LexerGlobalOptions
{
    public static InvalidEscapeHandling IgnoreInvalid  { get; set; }

    public static UnexpectedSymbolHandling UnexpectedSymbolHandling { get; set; }
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
