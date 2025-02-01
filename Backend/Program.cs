// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Backend;

internal enum CommandVariant
{
    Move,
    Save,
    Back,
    Git,
    Web
}

internal static class App
{
    
    internal static string BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                                      ?? throw new DirectoryNotFoundException("Executing assembly must have a location!");

    internal static List<string> CommandHelps =
    [
        SaveCommand.CreateDescription(),
        MoveCommand.CreateDescription(),
        WebCommand.CreateDescription(),
    ];
    
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return;
        }
        
        CommandVariant variant = Enum.Parse<CommandVariant>(args[0], true);
        using CancellationTokenSource source = new CancellationTokenSource();
        await StoreHelper.InitAsync(source.Token);

        try
        {
            IArgCommand command = variant switch
            {
                CommandVariant.Move => MoveCommand.FromArgs(args[1..]),
                CommandVariant.Save => SaveCommand.FromArgs(args[1..]),
                CommandVariant.Git => GitCommand.FromArgs(args[1..]),
                CommandVariant.Back => BackCommand.FromArgs(args[1..]),
                CommandVariant.Web => WebCommand.FromArgs(args[1..]),
                _ => throw new ArgParsingException()
            };

            await command.ExecuteAsync(source.Token);
        }
        catch (ArgParsingException ex)
        {
            PrintHelp(ex);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);
        }
        finally
        {
            await StoreHelper.SaveAsync(source.Token);
        }
    }

    private static void PrintHelp(ArgParsingException? ex)
    {
        PrintHelp();
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(ex.Message);
        Console.ResetColor();
    }

    private static void PrintHelp()
    {
        string banner =
            """
            |##########||   €€##########    \\\    ///   €€##########  |##########||
            |_|        |||  €€               \\\  ///    €€            |_|        |||
            |_|        |||  €€                \\\///     €€            |_|        |||
            |_|########||   €€#####           /\\\/      €€#####       |_|########||
            |_|  ||||       €€               ///\\\      €€            |_|  ||||
            |_|   ||||      €€              ///  \\\     €€            |_|   ||||
            |_|    ||||     €€##########   ///    \\\    €€##########  |_|    ||||

            Rexer is a tool suite which helps the user to simply different redundant tasks. 
            The goal of the tool is increasing the work speed. the main target audience are software developers 

            Commands:

            """;
        
        Console.WriteLine(banner);

        foreach (var commandHelp in CommandHelps)
        {
            Console.WriteLine(commandHelp);
            Console.WriteLine();
        }
    }
}