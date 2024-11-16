namespace Backend;

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