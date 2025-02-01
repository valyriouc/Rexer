namespace Backend;

internal class BackCommand : IArgCommand
{
    private readonly HistoryStore _historyStore;

    public BackCommand(HistoryStore historyStore)
    {
        _historyStore = historyStore;
    }
    
    public static IArgCommand FromArgs(string[] args) => 
        new BackCommand(StoreHelper.History);

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        string current = Environment.CurrentDirectory;
        _historyStore.Pop();
        string last = _historyStore.Pop();
        _historyStore.Push(current);
        Console.WriteLine(last);
        await Task.CompletedTask;
    }
}