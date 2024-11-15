// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Backend;

internal enum CommandVariant
{
    Move,
    Save,
    Back,
    Forth
}

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

internal static class App
{
    
    internal static string BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                                      ?? throw new DirectoryNotFoundException("Executing assembly must have a location!");
    
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
                _ => throw new ArgParsingException()
            };

            await command.ExecuteAsync(source.Token);
        }
        catch (ArgParsingException ex)
        {
            PrintHelp();
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

    private static void PrintHelp()
    {
        
    }
}