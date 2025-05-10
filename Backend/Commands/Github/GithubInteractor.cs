using System.Net.Http.Headers;
using System.Text.Json;

namespace Backend.Github;

// todo: encrypt access token 

internal abstract class GithubInteractor : IDisposable
{
    private static readonly string _configFile = Path.Combine(
        StoreHelper.ConfigDirectory,
        "github.json");

    protected GithubConfiguration Configuration = new(string.Empty, string.Empty);

    protected readonly HttpClient httpClient;
    
    protected GithubInteractor()
    {
        if (!File.Exists(_configFile))
        {
            File.Create(_configFile);
        }

        httpClient = new HttpClient();
    }

    protected HttpRequestMessage BuildRequestTemplate(HttpMethod method, Uri url)
    {
        HttpRequestMessage request = new HttpRequestMessage(method, url);

        if (string.IsNullOrWhiteSpace(this.Configuration.AuthenticationToken) || 
            string.IsNullOrWhiteSpace(this.Configuration.UserName))
        {
            throw new Exception("Configure your access token before calling the REST api.");
        }
        
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", this.Configuration.AuthenticationToken);
        request.Headers.Add("Accept", "application/vnd.github+json");
        request.Headers.Add("User-Agent", this.Configuration.UserName);
        
        return request;
    }
    
    protected async Task LoadAsync(CancellationToken cancellationToken)
    {
        string json = await File.ReadAllTextAsync(_configFile, cancellationToken);
        
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new Exception(
                $"Expected to find github configuration in file '{_configFile}' but it was empty.");
        }
        
        this.Configuration = JsonSerializer.Deserialize<GithubConfiguration>(json) ?? throw new InvalidOperationException("Github is not configured.");
    }

    protected async Task SaveAsync(CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(this.Configuration);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new Exception(
                "Something went wrong when saving the github configuration.");
        }
        
        await File.WriteAllTextAsync(_configFile, json, cancellationToken);
    }

    protected void ChangeAccessToken(string accessToken) =>
        this.Configuration.AuthenticationToken = accessToken;

    protected void ChangeUserName(string userName) 
    {
        if (string.IsNullOrWhiteSpace(userName) &&
            !string.IsNullOrWhiteSpace(this.Configuration.UserName))
        {
            return;
        }
        
        this.Configuration.UserName = userName;
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}