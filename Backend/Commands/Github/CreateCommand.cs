using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Backend.Extensions;

namespace Backend.Github;

internal class CreateCommand : GithubInteractor, IArgCommand
{
    [method: JsonConstructor]
    private class GitNewRepoItem(
        string name, 
        string description, 
        string homepageUrl, 
        bool isPrivate, 
        bool isTemplate)
    {
        [JsonPropertyName("name")] 
        public string Name { get; } = name;

        [JsonPropertyName("description")] 
        public string Description { get; } = description;

        [JsonPropertyName("homepage")]
        public string HomepageUrl { get; } = homepageUrl;

        [JsonPropertyName("private")]
        public bool IsPrivate { get; } = isPrivate;
        
        [JsonPropertyName("is_template")]
        public bool IsTemplate { get; } = isTemplate;
    }
    
    private static string BaseUrl = "https://api.github.com/user/repos";

    private readonly GitNewRepoItem _repository;
    
    private CreateCommand(GitNewRepoItem repository) => _repository = repository;

    public static IArgCommand FromArgs(string[] args)
    {
        string name = string.Empty;
        string description = string.Empty;
        string homepageUrl = string.Empty;
        bool isPrivate = false;
        bool isTemplate = false;
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--name":
                    name = args[i + 1];
                    i += 1;
                    break;
                case "--desc":
                    description = args[i + 1];
                    i += 1;
                    break;
                case "--url":
                    homepageUrl = args[i + 1];
                    i += 1;
                    break;
                case "--private":
                    isPrivate = true;
                    break;
                case "--template":
                    isTemplate = true;
                    break;
                default:
                    throw new ArgParsingException($"Unrecognized argument '{args[i]}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgParsingException("A name for the repository is required.");
        }
        
        return new CreateCommand(
            new GitNewRepoItem(name, description, homepageUrl, isPrivate, isTemplate));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);

        using HttpRequestMessage request = BuildRequestTemplate(HttpMethod.Post, new Uri(BaseUrl));
        request.Content = JsonContent.Create<GitNewRepoItem>(this._repository);
        
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        await response.ThrowIfErrorAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            Console.WriteLine("Repository created successfully.");
            
            Console.WriteLine("Run the following commands in your local repository:");
            Console.WriteLine($"1. git remote add origin https://github.com/{this.Configuration.UserName}/{this._repository.Name}.git");
            Console.WriteLine("2. git push -u origin main");
        }
        
        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            Console.WriteLine("Repository already exists.");
        }
        
        await SaveAsync(cancellationToken);
    }
}