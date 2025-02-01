namespace Backend;

internal class BackCommand : IArgCommand, IDescriptionProvider
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
            
        string lastMove = _historyStore.Pop();
        string last = _historyStore.Pop();
        
        _historyStore.Push(lastMove);
        _historyStore.Push(current);
        _historyStore.Push(last);
        
        Console.WriteLine(last);
        await Task.CompletedTask;
    }

    public static string CreateDescription() =>
        """
        Back - Gets back to the last location

        Example:
        Rexer.exe back
        """;
}