using System.Text;

namespace Backend.Extensions;

internal static class HttpResponseMessageExtensions
{
    public static async Task ThrowIfErrorAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (((int)response.StatusCode) >= 400)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Something went wrong: {(int)response.StatusCode} - {response.StatusCode}");
            sb.Append(await response.Content.ReadAsStringAsync(cancellationToken));
            throw new Exception(sb.ToString());
        }
    }
}