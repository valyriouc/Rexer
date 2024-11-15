using System.Reflection.PortableExecutable;
using System.Text;

namespace Backend;

internal class HistoryStore
{
    private static HistoryStore? _instance;
    private static string _filePath = Path.Combine(App.BasePath, "config", "history.txt");
    
    private readonly Stack<string> _history;
    
    private HistoryStore() 
    {
        if (!File.Exists(_filePath))
        {
            File.Create(_filePath).Dispose();
        }
        
        _history = new Stack<string>();    
    }
    
    /// <summary>
    /// Pops the last history entry out of the stack and returns it
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string Pop()
    {
        if (_history.Count == 0)
        {
            throw new Exception($"History is empty!");
        }

        return _history.Pop();
    }
    
    /// <summary>
    /// Pushes the specified path to the history
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Push(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentNullException(
                "Item must be a valid folder path!");
        }

        if (_history.Contains(path))
        {
            return;
        }
        
        _history.Push(path);    
    }
    
    /// <summary>
    /// Stores the history to its appropriate file 
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StoreAsync(CancellationToken cancellationToken)
    {
        StringBuilder sb = new();

        while (this._history.Count > 0)
        {
            string trimmed = this._history.Pop();
            sb.AppendLine(trimmed);
        }

        string history = sb.ToString();
        await File.WriteAllTextAsync(_filePath, history, cancellationToken);         
    }

    /// <summary>
    /// Loads the history from the appropriate file
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        string[] lines = await File.ReadAllLinesAsync(_filePath, cancellationToken);
        
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            string trimmed = lines[i].Trim();
            this._history.Push(trimmed);
        }
    }
    
    /// <summary>
    /// Get the instance of the history store
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<HistoryStore> GetInstanceAsync(CancellationToken cancellationToken)
    {
        if (_instance is null)
        {
            _instance = new HistoryStore();
            await _instance.LoadAsync(cancellationToken);
        }

        return _instance;
    }
}

internal class ConfigStore
{
    private static ConfigStore? _instance;
    private static string _filePath = Path.Combine(App.BasePath, "config", "move.txt");
    
    private readonly Dictionary<string, string> _config;
    
    private ConfigStore()
    {
        if (!File.Exists(_filePath))
        {
            File.Create(_filePath).Dispose();
        }
        
        _config = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// Saves a new setting for the move command
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TrySaveSetting(string key, string value)
    {
        if (_config.ContainsKey(key))
        {
            return false;
        }
        
        _config[key] = value;
        return true;
    }
    
    
    /// <summary>
    /// Deletes an existing setting
    /// </summary>
    /// <param name="key"></param>
    public void DeleteSetting(string key)
    {
        if (_config.ContainsKey(key))
        {
            _config.Remove(key);
        }
    }

    /// <summary>
    /// Finds a setting associated with key and returns the value of it
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string FindSetting(string key)
    {
        if (!_config.TryGetValue(key, out var setting))
        {
            throw new Exception($"No configuration for {key}");
        } 
        
        return setting;
    }
    
    /// <summary>
    /// Loads the configuration from the file
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <exception cref="Exception"></exception>
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        string[] lines = await File.ReadAllLinesAsync(_filePath, cancellationToken);
        int lineCounter = 0;
        foreach (string line in lines)
        {
            lineCounter += 1;
            var split = line.Split("=");

            if (split.Length != 2)
            {
                throw new Exception(
                    $"Invalid path mapping at line {lineCounter}: expected name:path!");
            }
            
            string key = split[0].Trim();
            string value = split[1].Trim();

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                throw new Exception($"Invalid path mapping at line {lineCounter}: key and value can't be empty!");
            }
            
            this._config.Add(key, value);
        }
    }

    /// <summary>
    /// Stores the configuration to the appropriate file 
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StoreAsync(CancellationToken cancellationToken)
    {
        StringBuilder sb = new();

        foreach (KeyValuePair<string, string> pair in this._config)
        {
            sb.AppendLine($"{pair.Key}={pair.Value}");
        }

        string content = sb.ToString();
        await File.WriteAllTextAsync(_filePath, content, cancellationToken);
    }

    public static async Task<ConfigStore> GetInstanceAsync(CancellationToken cancellationToken)
    {
        if (_instance is null)
        {
            _instance = new ConfigStore();
            await _instance.LoadAsync(cancellationToken);
        }

        return _instance;
    }
}

internal static class StoreHelper
{
    public static HistoryStore History { get; private set; }

    public static ConfigStore Config { get; private set; }

    public static async Task InitAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(Path.Combine(App.BasePath, "config")))
        {
            Directory.CreateDirectory(Path.Combine(App.BasePath, "config"));
        }
        
        History = await HistoryStore.GetInstanceAsync(cancellationToken);
        Config = await ConfigStore.GetInstanceAsync(cancellationToken);
    }

    public static async Task SaveAsync(CancellationToken cancellationToken)
    {
        await History.StoreAsync(cancellationToken);
        await Config.StoreAsync(cancellationToken);
    }
}