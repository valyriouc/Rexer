namespace Backend;

public interface IArgCommand 
{
    public static abstract IArgCommand FromArgs(string[] args);

    public Task ExecuteAsync(CancellationToken cancellationToken);
}

public interface IDescriptionProvider
{
    public static abstract string CreateDescription();
}