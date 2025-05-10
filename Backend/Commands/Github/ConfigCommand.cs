namespace Backend.Github;

/// <summary>
/// Configures the GitHub account
/// </summary>
internal class ConfigCommand : GithubInteractor, IArgCommand
{
    private readonly string _accessToken;
    private readonly string _userName;
    
    private ConfigCommand(string accessToken, string userName)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentNullException(nameof(accessToken));
        }
        
        this._accessToken = accessToken;
        this._userName = userName;
    }
    
    public static IArgCommand FromArgs(string[] args)
    {
        string accessToken = string.Empty;
        string userName = string.Empty;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--accessToken":
                    accessToken = args[i + 1];
                    i += 1;
                    break;
                case "--userName":
                    userName = args[i + 1];
                    i += 1;
                    break;
                default:
                    throw new ArgParsingException(
                        $"Git config command does not support argument '{args[i]}'.");
            }
        }
        
        return new ConfigCommand(accessToken, userName);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await LoadAsync(cancellationToken);
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            ChangeAccessToken(accessToken: this._accessToken);
            ChangeUserName(userName: this._userName);
        
            await SaveAsync(cancellationToken);   
        }
    }
}
