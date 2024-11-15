﻿using System.Text;

namespace Backend;

public interface IArgCommand
{
    public static abstract IArgCommand FromArgs(string[] args);

    public Task ExecuteAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Enables the user to move faster in the file system to known places 
/// </summary>
public class MoveCommand : IArgCommand
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
}

internal class SaveCommand : IArgCommand
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
}

internal class BackCommand : IArgCommand
{
    public static IArgCommand FromArgs(string[] args)
    {
        throw new NotImplementedException();
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

internal class ForthCommand : IArgCommand
{
    public static IArgCommand FromArgs(string[] args)
    {
        throw new NotImplementedException();
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}