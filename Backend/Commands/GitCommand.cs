﻿using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend;

/// <summary>
/// Command-line tool to interact with github
/// </summary>
public class GitCommand : IArgCommand
{
    private enum SubCommand
    {
        Create,
        List,
        Config,
        Clone
    }

    private readonly IArgCommand _subCommand;
    
    private GitCommand(IArgCommand subCommand)
    {
        this._subCommand = subCommand;    
    }
    
    public static IArgCommand FromArgs(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgParsingException("Git command expects a sub command.");
        }

        SubCommand subCommand = Enum.Parse<SubCommand>(args[0], true);
        IArgCommand command = subCommand switch
        {
            SubCommand.Create => GitCreateCommand.FromArgs(args[1..]),
            SubCommand.List => GitListCommand.FromArgs(args[1..]),
            SubCommand.Config => GitConfigCommand.FromArgs(args[1..]),
            SubCommand.Clone => GitCloneCommand.FromArgs(args[1..]),
            _ => throw new ArgParsingException($"SubCommand '{args[0]}' is not supported.")
        };

        return new GitCommand(command);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await this._subCommand.ExecuteAsync(cancellationToken);
        }
        finally
        {
            if (this._subCommand is GithubInteractor interactor)
            {
                interactor.Dispose();
            }
        }
    }
}

file class GitCloneCommand : IArgCommand
{
    private static string CloneDirectory = "C:\\Users\\Valarius\\H4ck3r\\source";

    private readonly string _repository;
    private readonly string _directory;
    
    private GitCloneCommand(string repository)
    {
        if (string.IsNullOrWhiteSpace(repository))
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _repository = repository;
        _directory = Path.Combine(CloneDirectory, Path.GetFileNameWithoutExtension(_repository));
    }
    
    public static IArgCommand FromArgs(string[] args)
    {
        string repository = string.Empty;

        for (int i = 0; i < args.Length; i++)
        {
            switch(args[i])
            {
                case "--url":
                    repository = args[++i];
                    break;
                default:
                    throw new ArgParsingException($"Unexpected argument '{args[i]}'.");
            }
        }

        return new GitCloneCommand(repository);
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Cloning {_repository} into {_directory}");
        
        using Process process = new Process();
        process.StartInfo.FileName = "git.exe";
        process.StartInfo.Arguments = $"clone {_repository} {_directory}";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.Start();

        while (!process.HasExited)
        {
            
        }

        await Task.CompletedTask;
    }
}

// todo: encrypt access token 

file class GithubInteractor : IDisposable
{
    private static string ConfigFile = Path.Combine(
        StoreHelper.ConfigDirectory,
        "github.json");

    protected GithubConfiguration configuration = 
        new GithubConfiguration(string.Empty, string.Empty);

    protected readonly HttpClient httpClient;
    
    protected GithubInteractor()
    {
        if (!File.Exists(ConfigFile))
        {
            File.Create(ConfigFile);
        }

        httpClient = new HttpClient();
    }

    protected HttpRequestMessage BuildRequestTemplate(HttpMethod method, Uri url)
    {
        HttpRequestMessage request = new HttpRequestMessage(method, url);

        if (string.IsNullOrWhiteSpace(this.configuration.AuthenticationToken) || 
            string.IsNullOrWhiteSpace(this.configuration.UserName))
        {
            throw new Exception("Configure your access token before calling the REST api.");
        }
        
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", this.configuration.AuthenticationToken);
        request.Headers.Add("Accept", "application/vnd.github+json");
        request.Headers.Add("User-Agent", this.configuration.UserName);
        
        return request;
    }
    
    protected async Task LoadAsync(CancellationToken cancellationToken)
    {
        string json = await File.ReadAllTextAsync(ConfigFile, cancellationToken);
        
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new Exception(
                $"Expected to find github configuration in file '{ConfigFile}' but it was empty.");
        }
        
        this.configuration = JsonSerializer.Deserialize<GithubConfiguration>(json);
    }

    protected async Task SaveAsync(CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(this.configuration);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new Exception(
                "Something went wrong when saving the github configuration.");
        }
        
        await File.WriteAllTextAsync(ConfigFile, json, cancellationToken);
    }

    protected void ChangeAccessToken(string accessToken) =>
        this.configuration.AuthenticationToken = accessToken;

    protected void ChangeUserName(string userName) 
    {
        if (string.IsNullOrWhiteSpace(userName) &&
            !string.IsNullOrWhiteSpace(this.configuration.UserName))
        {
            return;
        }
        
        this.configuration.UserName = userName;
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}

[method: JsonConstructor]
internal class GithubConfiguration(string authenticationToken, string userName)
{
    [JsonPropertyName("authentication_token")]
    public string AuthenticationToken { get; set; } = authenticationToken;

    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = userName;
}

file class GitCreateCommand : GithubInteractor, IArgCommand
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
    
    private GitCreateCommand(GitNewRepoItem repository)
    {
        _repository = repository;
    }
    
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
        
        return new GitCreateCommand(
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
            Console.WriteLine($"- git remote add origin https://github.com/{this.configuration.UserName}/{this._repository.Name}.git");
            Console.WriteLine("- git push -u origin main");
        }
        
        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            Console.WriteLine("Repository already exists.");
        }
        
        await SaveAsync(cancellationToken);
    }
}

/// <summary>
/// List all repositories of the specified user 
/// </summary>
file class GitListCommand : GithubInteractor, IArgCommand
{
    private enum TypeQueryParameter
    {
        All,
        Owner, 
        Member
    }

    private enum SortQueryParameter
    {
        Created,
        Updated,
        Pushed, 
        FullName
    }

    private enum DirectionQueryParameter
    {
        Asc,
        Desc,
    }

    [method: JsonConstructor]
    private struct GithubListRepositoryItem(string name, string fullName)
    {
        [JsonPropertyName("name")]
        public string Name { get; } = name;
        
        [JsonPropertyName("full_name")]
        public string FullName { get; } = fullName;
    }

    private static string BaseUrl => "https://api.github.com/users/";
    
    private readonly TypeQueryParameter _typeQueryParameter;
    private readonly SortQueryParameter _sortQueryParameter;
    private readonly DirectionQueryParameter _directionQueryParameter;
    private readonly uint _itemsPerPage;
    private readonly uint _pageNumber;
    
    private GitListCommand(
        TypeQueryParameter typeQuery,
        SortQueryParameter sortQuery,
        DirectionQueryParameter directionQuery,
        uint itemsPerPage,
        uint pageNumber)
    {
        _typeQueryParameter = typeQuery;
        _sortQueryParameter = sortQuery;
        _directionQueryParameter = directionQuery;
        _itemsPerPage = itemsPerPage;
        _pageNumber = pageNumber;
    }
    
    public static IArgCommand FromArgs(string[] args)
    {
        TypeQueryParameter typeQuery = TypeQueryParameter.Owner;
        SortQueryParameter sortQuery = SortQueryParameter.FullName;
        DirectionQueryParameter directionQuery = DirectionQueryParameter.Asc;
        uint itemsPerPage = 30;
        uint pageNumber = 1;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--type":
                    typeQuery = Enum.Parse<TypeQueryParameter>(args[i + 1], true);
                    i += 1;
                    break;
                case "--sort":
                    sortQuery = Enum.Parse<SortQueryParameter>(args[i + 1], true);
                    i += 1;
                    break;
                case "--direction":
                    directionQuery = Enum.Parse<DirectionQueryParameter>(args[i + 1], true);
                    i += 1;
                    break;
                case "--page":
                    pageNumber = uint.Parse(args[i + 1]);
                    i += 1;
                    break;
                case "--page-size":
                    itemsPerPage = uint.Parse(args[i + 1]);

                    if (itemsPerPage > 100)
                    {
                        throw new ArgParsingException("Github only supports pages up to 100 items!");
                    }

                    i += 1;
                    break;
                default:
                    throw new ArgParsingException($"Unknown arg: '{args[i]}'.");
            }
        }

        return new GitListCommand(
            typeQuery,
            sortQuery,
            directionQuery,
            itemsPerPage,
            pageNumber);
    }

    private Uri BuildUrl()
    {
        StringBuilder sb = new StringBuilder(BaseUrl);

        sb.Append($"{this.configuration.UserName}/repos");
        
        string typeQuery = _typeQueryParameter switch
        {
            TypeQueryParameter.All => "?type=all",
            TypeQueryParameter.Owner => "?type=owner",
            TypeQueryParameter.Member => "?type=member",
            _ => throw new ArgumentException("Invalid type query parameter.")
        };

        sb.Append(typeQuery);

        string sortQuery = _sortQueryParameter switch
        {
            SortQueryParameter.Created => "&sort=created",
            SortQueryParameter.Updated => "&sort=updated",
            SortQueryParameter.Pushed => "&sort=pushed",
            SortQueryParameter.FullName => "&sort=full_name",
            _ => throw new ArgumentException("Invalid sort query parameter.")
        };
        
        sb.Append(sortQuery);

        string directionQuery = _directionQueryParameter switch
        {
            DirectionQueryParameter.Asc => "&direction=asc",
            DirectionQueryParameter.Desc => "&direction=desc",
            _ => throw new ArgumentException("Invalid direction query parameter.")
        };
        
        sb.Append(directionQuery);
        sb.Append($"&per_page={_itemsPerPage}");
        sb.Append($"&page={_pageNumber}");
        
        return new Uri(sb.ToString());
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);

        Uri url = BuildUrl();
        
        using HttpRequestMessage request = BuildRequestTemplate(HttpMethod.Get, url);
        using HttpResponseMessage response = await this.httpClient.SendAsync(request, cancellationToken);
        
        await response.ThrowIfErrorAsync(cancellationToken);

        List<GithubListRepositoryItem>? items =
            await response.Content.ReadFromJsonAsync<List<GithubListRepositoryItem>>(
                cancellationToken: cancellationToken);

        if (items is null)
        {
            throw new Exception("Something went wrong when retrieving repositories from the response body!");
        }

        foreach (GithubListRepositoryItem item in items)
        {
            Console.WriteLine($"{item.Name} - https://github.com/{item.FullName}");
        }

        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($"On this page are {items.Count} repositories of {this.configuration.UserName}");        
        Console.ForegroundColor = ConsoleColor.White;
        
        await SaveAsync(cancellationToken);
    }
}

file class GitConfigCommand : GithubInteractor, IArgCommand
{
    private readonly string _accessToken;
    private readonly string _userName;
    
    private GitConfigCommand(string accessToken, string userName)
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
        
        return new GitConfigCommand(accessToken, userName);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await LoadAsync(cancellationToken);
        }
        catch (Exception)
        {
            
        }
        finally
        {
            ChangeAccessToken(accessToken: this._accessToken);
            ChangeUserName(userName: this._userName);
        
            await SaveAsync(cancellationToken);   
        }
    }
}

file static class HttpResponseMessageExtensions
{
    public static async Task ThrowIfErrorAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (((int)response.StatusCode) >= 400)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Something went wrong: {(int)response.StatusCode} - {response.StatusCode}");
            sb.Append(await response.Content.ReadAsStringAsync());
            throw new Exception(sb.ToString());
        }
    }
}