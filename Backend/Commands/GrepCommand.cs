using System.Text.RegularExpressions;

namespace Backend;

public class GrepCommand : IArgCommand
{
    private readonly string _pattern;

    private GrepCommand(string pattern)
    {
        _pattern = pattern;
    }
    public static IArgCommand FromArgs(string[] args)
    {
        string? pattern = null;
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-p":
                    pattern = args[i + 1];
                    i += 1;
                    break;
                default:
                    throw new ArgParsingException($"Unkown argument: {args[i]}");
                    break;
            }
        }

        if (pattern is null)
        {
            throw new ArgParsingException("No pattern provided!");
        }

        return new GrepCommand(pattern);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        int lineNumber = 0;
        
        string? line = string.Empty;
        while ((line = Console.ReadLine()) != null)
        {
            lineNumber += 1;
            if (!Regex.IsMatch(line, _pattern) || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            
            Console.WriteLine($"{lineNumber}: {line}");
        }
        
        await Task.CompletedTask;
    }
}