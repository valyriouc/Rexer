namespace Backend;

internal class SaveCommand : IArgCommand, IDescriptionProvider
{
    private readonly HistoryStore _historyStore;
    private readonly ConfigStore _configStore;
    
    public string Name { get; }

    public string Location { get; }

    public SaveCommand(
        HistoryStore historyStore,
        ConfigStore configStore,
        string name, 
        string location)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentNullException(nameof(location));
        }

        if (!Directory.Exists(location))
        {
            throw new DirectoryNotFoundException($"Location {location} not found!");
        }
        
        Name = name;
        Location = location;
        this._historyStore = historyStore;
        this._configStore = configStore;
    }
    
    public static IArgCommand FromArgs(string[] args)
    {
        string name = string.Empty;
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-n":
                    name = args[i + 1];
                    i += 1;
                    break;
                default:
                    throw new ArgParsingException($"Unkown argument: {args[i]}");
            }    
        }

        string currentDir = Environment.CurrentDirectory;
        return new SaveCommand(
            StoreHelper.History,
            StoreHelper.Config,
            name, 
            currentDir);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!this._configStore.TrySaveSetting(Name, Location))
        {
            throw new Exception($"It seems that there is already a entry with key '{Name}'!");
        }
        
        this._historyStore.Push(this.Location);
        await Task.CompletedTask; 
    }

    public static string CreateDescription()
    {
        return """
               Save - Stores a shortcut name associated with the current location.
               Args:
                -n  - The shortcut name of the location.

               Example:
               Rexer.exe save -n tmp
               """;
    }
}
