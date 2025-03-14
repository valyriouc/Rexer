using Xunit;
using System.Net.Http;

namespace Backend.Tests
{
    public class WebCommandTests
    {
        [Fact]
        public void FromArgs_WithGetMethod_ShouldCreateCorrectRequest()
        {
            // Arrange
            string[] args = { "-m", "GET", "-u", "https://example.com" };

            // Act
            var webCommand = WebCommand.FromArgs(args);

            // Assert
            Assert.IsType<WebCommand>(webCommand);
            Assert.Equal(HttpMethod.Get, ((WebCommand)webCommand)._request.Method);
            Assert.Equal("https://example.com", ((WebCommand)webCommand)._request.RequestUri.ToString());
        }

        [Fact]
        public void FromArgs_WithPostMethodAndBody_ShouldCreateCorrectRequest()
        {
            // Arrange
            string[] args = { "-m", "POST", "-u", "https://example.com", "-b", "Hello, World!" };

            // Act
            var webCommand = WebCommand.FromArgs(args);

            // Assert
            Assert.IsType<WebCommand>(webCommand);
            Assert.Equal(HttpMethod.Post, ((WebCommand)webCommand)._request.Method);
            Assert.Equal("https://example.com", ((WebCommand)webCommand)._request.RequestUri.ToString());
            Assert.NotNull(((WebCommand)webCommand)._request.Content);
            Assert.Equal("Hello, World!", ((WebCommand)webCommand)._request.Content.ReadAsStringAsync().Result);
        }

        [Fact]
        public void FromArgs_WithHeader_ShouldCreateCorrectRequest()
        {
            // Arrange
            string[] args = { "-m", "GET", "-u", "https://example.com", "-h", "Content-Type: application/json" };

            // Act
            var webCommand = WebCommand.FromArgs(args);

            // Assert
            Assert.IsType<WebCommand>(webCommand);
            Assert.Equal(HttpMethod.Get, ((WebCommand)webCommand)._request.Method);
            Assert.Equal("https://example.com", ((WebCommand)webCommand)._request.RequestUri.ToString());
            Assert.NotNull(((WebCommand)webCommand)._request.Headers.TryGetValues("Content-Type", out var values));
            Assert.Contains("application/json", values);
        }
    }
}