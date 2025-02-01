namespace Backend;

/// <summary>
/// Enables the user to move faster in the file system to known places 
/// </summary>
public class MoveCommand : IArgCommand, IDescriptionProvider
{
    private HistoryStore HistoryStore { get; }
    
    private ConfigStore ConfigStore { get; }
    
    private string LocationShortcut { get; }

    private MoveCommand(HistoryStore historyStore, ConfigStore configStore, string locationShortcut)
    {
        if (string.IsNullOrWhiteSpace(locationShortcut))
        {
            throw new ArgumentNullException("Please specify a valid location identifier!");
        }
        
        this.HistoryStore = historyStore;
        this.ConfigStore = configStore;
        this.LocationShortcut = locationShortcut;
    }
    
    public static IArgCommand FromArgs(string[] args)
    {
        string locationShortcut = string.Empty;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-n":
                    locationShortcut = args[i + 1];
                    i += 1;
                    break;
                default:
                    throw new ArgParsingException($"Unkown argument: {args[i]}");
            }
        }
        
        return new MoveCommand(
            StoreHelper.History, 
            StoreHelper.Config,
            locationShortcut);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        string path = this.ConfigStore.FindSetting(this.LocationShortcut);
        this.HistoryStore.Push(path);
        Console.WriteLine(path);
        await Task.CompletedTask;
    }

    public static string CreateDescription()
    {
        return """
               Move - Gets the user to the location associated with the provided name
               Args:
                -n  - The location name
               
               Example:
               Rexer.exe move -n tmp
               """;
    }
}