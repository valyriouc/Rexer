using System.Diagnostics;

namespace Backend.Github;

internal class CloneCommand : IArgCommand
{
    private static readonly string CloneDirectory = "C:\\Users\\Valarius\\H4ck3r\\source";

    private readonly string _repository;
    private readonly string _directory;
    
    private CloneCommand(string repository)
    {
        if (string.IsNullOrWhiteSpace(repository))
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _repository = repository;
        _directory = Path.Combine(CloneDirectory, Path.GetFileNameWithoutExtension(_repository));
    }
    
    public static IArgCommand FromArgs(string[] args)
    {
        string repository = string.Empty;

        for (int i = 0; i < args.Length; i++)
        {
            switch(args[i])
            {
                case "--url":
                    repository = args[++i];
                    break;
                default:
                    throw new ArgParsingException($"Unexpected argument '{args[i]}'.");
            }
        }

        return new CloneCommand(repository);
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Cloning {_repository} into {_directory}");
        
        using Process process = new Process();
        process.StartInfo.FileName = "git.exe";
        process.StartInfo.Arguments = $"clone {_repository} {_directory}";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.Start();

        while (!process.HasExited)
        {
            
        }

        await Task.CompletedTask;
    }
}
