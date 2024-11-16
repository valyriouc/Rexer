// See https://aka.ms/new-console-template for more information

using System.Reflection;

internal static class Program
{
    private static string SecretFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "secret.txt");
    
    public static async Task Main()
    {
        string accessToken = await File.ReadAllTextAsync(SecretFile);
        
        using HttpClient client = new HttpClient();
        
        using HttpRequestMessage message =
            new HttpRequestMessage(HttpMethod.Get, new Uri("https://api.github.com/users/valyriouc/repos"));

        message.Headers.Add("Accept", "application/vnd.github+json");
        message.Headers.Add("User-Agent", "valyriouc");
        message.Headers.Add("Authorization", $"Bearer {accessToken}");
        
        using HttpResponseMessage response = await client.SendAsync(message);
        
        string content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
    }
}