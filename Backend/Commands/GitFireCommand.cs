namespace Backend;

/// <summary>
/// Makes git add, commit und push
/// </summary>
public class GitFireCommand : IArgCommand
{
    private readonly string _message;
    
    public GitFireCommand(string message) => _message = message;
    
    public static IArgCommand FromArgs(string[] args)
    {
        string? message = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-m":
                    message = args[i + 1];
                    i += 1;
                    break;
                default:
                    throw new ArgParsingException($"Not a valid git fire argument: {args[i]} ");
            }
        }

        if (message is null)
        {
            throw new ArgParsingException($"Git message is required.");
        }
        
        return new GitFireCommand(message);
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"git add * && git commit -m {_message} && git push");
        return Task.CompletedTask;
    }
}