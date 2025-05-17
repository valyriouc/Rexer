using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Backend.Extensions;

namespace Backend.Github;

file enum CreateArgument
{
    Name,
    Description,
    Url,
    Private,
    Template,
    Help
}

/// <summary>
/// Creates a new GitHub repository 
/// </summary>
internal class CreateCommand : GithubInteractor, IArgCommand, IDescriptionProvider
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
            switch (args[i].ToEnum())
            {
                case CreateArgument.Help:
                    string help = CreateDescription();
                    Console.WriteLine(help);
                    break;  
                case CreateArgument.Name:
                    name = args[i + 1];
                    i += 1;
                    break;
                case CreateArgument.Description:
                    description = args[i + 1];
                    i += 1;
                    break;
                case CreateArgument.Url:
                    homepageUrl = args[i + 1];
                    i += 1;
                    break;
                case CreateArgument.Private:
                    isPrivate = true;
                    break;
                case CreateArgument.Template:
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

    public static string CreateDescription()
    {
        StringBuilder sb = new();

        string start =
            """
            This command will create a new github repository for the configured user.
            """;
        
        sb.AppendLine(start);
        sb.AppendLine("Arguments: ");
        
        foreach (CreateArgument value in Enum.GetValues<CreateArgument>())
        {
            sb.AppendLine($"{value.ToArgumentString()} - {value.GetDescription()}");
        }
        
        return sb.ToString();
    }
}

file static class LocalExtensions
{
    public static CreateArgument ToEnum(this string arg)
    {
        switch (arg)
        {
            case "--help":
                return CreateArgument.Help;                
            case "--name":
                return CreateArgument.Name;
            case "--desc":
                return CreateArgument.Description;
            case "--url":
                return CreateArgument.Url;
            case "--private":
                return CreateArgument.Private;    
            case "--template":
                return CreateArgument.Template;
            default:
                throw new ArgParsingException($"Unrecognized argument '{arg}'.");
        }
    }

    public static string ToArgumentString(this CreateArgument arg)
    {
        return arg switch
        {
            CreateArgument.Name => "--name",
            CreateArgument.Description => "--desc",
            CreateArgument.Url => "--url",
            CreateArgument.Private => "--private",
            CreateArgument.Template => "--template",
            CreateArgument.Help => "--help",
            _ => throw new NotSupportedException($"Unrecognized argument '{arg}'.")
        };
    }
    
    public static string GetDescription(this CreateArgument arg)
    {
        return arg switch
        {
            CreateArgument.Name => "The name of the repository",
            CreateArgument.Description => "The description of the repository",
            CreateArgument.Url => "The homepage url of the repository",
            CreateArgument.Private => "Whether the repository is private/public",
            CreateArgument.Template => "Whether the repository is a template",
            _ => throw new NotSupportedException($"Unrecognized argument '{arg}'.")
        };
    }
}