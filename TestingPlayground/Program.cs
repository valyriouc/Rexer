// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Reflection;

internal static class Program
{
    private static string SecretFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "secret.txt");
    
    public static async Task Main()
    {
        // TestingPathMethods();
        await TestingGitClone();
    }

    private static void TestingPathMethods()
    {
        var name = Path.GetFileNameWithoutExtension("https://github.com/valyriouc/Arduino.git");
        Console.WriteLine(name);
    }
    
    private static async Task TestingGitClone()
    {
        string directory = "C:\\Users\\Valarius\\H4ck3r\\tmp\\Arduino";
        string repoUrl = "https://github.com/valyriouc/Arduino.git";

        
        
        Process process = new Process();
        process.StartInfo.FileName = "git.exe";
        process.StartInfo.Arguments = $"clone {repoUrl} {directory}";

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        bool result = process.Start();

        while (!process.HasExited)
        {
            
        }
        
        process.Dispose();

    }
    
    private static async Task TestingGithubListRepos()
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