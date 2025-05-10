using System.Text;
using Backend.Github;

namespace Backend;

file enum SubCommand
{
    Create,
    List,
    Config,
    Clone,
    Fire
}

/// <summary>
/// Command-line tool to interact with github
/// </summary>
public class GitCommand : IArgCommand, IDescriptionProvider
{
    
    private readonly IArgCommand _subCommand;
    
    private GitCommand(IArgCommand subCommand)
    {
        this._subCommand = subCommand;    
    }
    
    public static IArgCommand FromArgs(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgParsingException("Git command expects a sub command.");
        }

        SubCommand subCommand = Enum.Parse<SubCommand>(args[0], true);
        IArgCommand command = subCommand switch
        {
            SubCommand.Create => CreateCommand.FromArgs(args[1..]),
            SubCommand.List => ListCommand.FromArgs(args[1..]),
            SubCommand.Config => ConfigCommand.FromArgs(args[1..]),
            SubCommand.Clone => CloneCommand.FromArgs(args[1..]),
            SubCommand.Fire => GitFireCommand.FromArgs(args[1..]),
            _ => throw new ArgParsingException($"SubCommand '{args[0]}' is not supported.")
        };

        return new GitCommand(command);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await this._subCommand.ExecuteAsync(cancellationToken);
        }
        finally
        {
            if (this._subCommand is GithubInteractor interactor)
            {
                interactor.Dispose();
            }
        }
    }

    public static string CreateDescription()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("This command allows to perform different operation on your github account.");
        foreach (var command in Enum.GetValues<SubCommand>())
        {
            sb.AppendLine($"{command.ToString()} - {command.ToDescription()}");
        }
        
        return sb.ToString();
    }
}

file static class SubCommandExtensions
{
    public static string ToDescription(this SubCommand self) => self switch
    {
        SubCommand.Create => "Creates a new github repository for the configured account",
        SubCommand.List => "Lists all repositories",
        SubCommand.Config => "Configures the github account that should be used",
        SubCommand.Clone => "Clones the specified github repository to the source directory",
        SubCommand.Fire => "Makes a git add, commit and push",
        _ => throw new ArgParsingException($"SubCommand '{self}' is not supported."),
    };
}
