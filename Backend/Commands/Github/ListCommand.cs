using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Backend.Extensions;

namespace Backend.Github;

/// <summary>
/// List all repositories of the specified user 
/// </summary>
internal class ListCommand : GithubInteractor, IArgCommand
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
    
    private ListCommand(
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

        return new ListCommand(
            typeQuery,
            sortQuery,
            directionQuery,
            itemsPerPage,
            pageNumber);
    }

    private Uri BuildUrl()
    {
        StringBuilder sb = new StringBuilder(BaseUrl);

        sb.Append($"{this.Configuration.UserName}/repos");
        
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
        Console.WriteLine($"On this page are {items.Count} repositories of {this.Configuration.UserName}");        
        Console.ForegroundColor = ConsoleColor.White;
        
        await SaveAsync(cancellationToken);
    }
}