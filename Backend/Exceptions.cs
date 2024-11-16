namespace Backend;

public class ArgParsingException : Exception
{
    public ArgParsingException()
    {
    }

    public ArgParsingException(string message) : base(message)
    {
    }

    public ArgParsingException(string message, Exception inner) : base(message, inner)
    {
    }
}
