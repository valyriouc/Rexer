using System.Runtime.CompilerServices;

namespace Backend;

internal enum WebMethod
{
    Post,
    Get,
    Put,
    Patch,
    Head,
    Delete,
    Options,
    Trace
}

internal static class WebMethodExtensions
{
    public static void ApplyToBuilder(this WebMethod method, HttpRequestMessageBuilder builder)
    {
        switch (method)
        {
            case WebMethod.Post:
                builder.WithPost();
                break;
            case WebMethod.Get:
                builder.WithGet();
                break;
            case WebMethod.Put:
                builder.WithPut();
                break;
            case WebMethod.Patch:
                builder.WithPatch();
                break;
            case WebMethod.Head:
                builder.WithHead();
                break;
            default:
                throw new NotSupportedException($"WebMethod {method} is not supported");
        }
    }
}

/// <summary>
/// A command-line http client like curl or wget 
/// </summary>
public class WebCommand : IArgCommand, IDescriptionProvider
{
    private readonly HttpRequestMessage _request;
    
    public WebCommand(HttpRequestMessage request)
    {
        this._request = request;
    }
    
    public static IArgCommand FromArgs(string[] args)
    {
        HttpRequestMessageBuilder builder = new HttpRequestMessageBuilder();
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-m":
                    var method = Enum.Parse<WebMethod>(args[i + 1].Trim(), true);
                    method.ApplyToBuilder(builder);
                    i += 1;
                    break;
                case "-u":
                    string url = args[i + 1].Trim();
                    builder.WithUrl(url);
                    i += 1;
                    break;
                case "-b":
                    string body = args[i + 1].Trim();
                    builder.WithContent(body);
                    i += 1;
                    break;
                case "-h":
                    string[] headerParts1 = args[i + 1].Split(":");
                    if (headerParts1.Length != 2)
                    {
                        throw new InvalidOperationException("Http header is malformed!");
                    }
                    builder.WithHeader(headerParts1[0].Trim(), headerParts1[1].Trim());
                    i += 1;
                    break;
                case "-ch":
                    string[] headerParts2 = args[i + 1].Split(":");
                    if (headerParts2.Length != 2)
                    {
                        throw new InvalidOperationException("Http content header is malformed!");
                    }
                    builder.WithContentHeader(headerParts2[0].Trim(), headerParts2[1].Trim());
                    i += 1;
                    break;
                default:
                    throw new ArgParsingException($"Unkown argument: {args[i]}");
            }
        }

        return new WebCommand(builder.Build());
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient client = new HttpClient();
            using HttpResponseMessage response = await client.SendAsync(_request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {(int)response.StatusCode} - {response.ReasonPhrase}"); 
                string content = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine(content);
                return;
            }
            
            Console.WriteLine(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        finally
        {
            this._request.Dispose();
        }
    }

    public static string CreateDescription()
    {
        return """
               Web - A command-line http client like curl or wget
               Args:
                -u  - The url to send the request to
                -m  - The http method to use
                -b  - The http body
                -h  - The http header
                -ch - The http content header
               
               Example: 
               Rexer.exe web -m GET -b "Hello World" -h "Content-Type: text/plain" -ch "Accept: text/plain" -u https://www.google.com/
               """;
    }
}

internal class HttpRequestMessageBuilder
{ 
    private HttpMethod? method;
    private List<KeyValuePair<string, string>> headers = [];
    private List<KeyValuePair<string, string>> contentHeaders = [];
    private StringContent? content;
    private Uri? uri;

    public HttpRequestMessageBuilder WithGet()
    {
        this.method = HttpMethod.Get;
        return this;
    }
    
    public HttpRequestMessageBuilder WithPost()
    {
        this.method = HttpMethod.Post;
        return this;
    }

    public HttpRequestMessageBuilder WithPatch()
    {
        this.method = HttpMethod.Patch;
        return this;
    }
    
    public HttpRequestMessageBuilder WithPut()
    {
        this.method = HttpMethod.Put;
        return this;
    }
    
    public HttpRequestMessageBuilder WithHead()
    {
        this.method = HttpMethod.Head;
        return this;
    }

    public HttpRequestMessageBuilder WithUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentNullException(nameof(url));
        }
        
        this.uri = new Uri(url);
        return this;
    }

    public HttpRequestMessageBuilder WithHeader(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(nameof(value));    
        }
        
        this.headers.Add(new KeyValuePair<string, string>(name, value));
        return this;
    }

    public HttpRequestMessageBuilder WithContentHeader(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(nameof(value));
        }
        
        this.contentHeaders.Add(new KeyValuePair<string, string>(name, value));
        return this;
    }

    public HttpRequestMessageBuilder WithContent(string payload)
    {
        this.content = new StringContent(payload);
        return this;
    }

    public HttpRequestMessage Build()
    {
        if (this.uri is null)
        {
            throw new InvalidOperationException("The request uri has not been set.");
        }
        
        HttpRequestMessage message = this.method is null ? 
            new HttpRequestMessage(HttpMethod.Get, this.uri) :
            new HttpRequestMessage(this.method, this.uri);
        
        foreach (KeyValuePair<string, string> header in this.headers)
        {
            message.Headers.Add(header.Key, header.Value);
        }

        message.Content = this.content;

        foreach (KeyValuePair<string, string> contentHeader in this.contentHeaders)
        {
            message.Content!.Headers.Add(contentHeader.Key, contentHeader.Value);
        }

        return message;
    }
}
