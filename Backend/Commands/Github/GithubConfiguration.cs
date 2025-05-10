using System.Text.Json.Serialization;

namespace Backend.Github;

[method: JsonConstructor]
internal class GithubConfiguration(string authenticationToken, string userName)
{
    [JsonPropertyName("authentication_token")]
    public string AuthenticationToken { get; set; } = authenticationToken;

    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = userName;
}
