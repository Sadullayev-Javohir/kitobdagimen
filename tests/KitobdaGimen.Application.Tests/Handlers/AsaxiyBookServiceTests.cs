using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Infrastructure.External;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KitobdaGimen.Application.Tests.Handlers;

public class AsaxiyBookServiceTests
{
    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }

    private sealed class FakeCacheService : KitobdaGimen.Application.Common.Interfaces.ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default) => Task.CompletedTask;
        public Task FlushAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    [Fact]
    public void ParseSearch_Parses_JsonLd_With_Quotes_In_Title()
    {
        var html = @"<!DOCTYPE html><html><head>
            <script type=""application/ld+json"">{
                ""@context"":""https://schema.org"",
                ""@type"":""ItemList"",
                ""name"":""Kitoblar"",
                ""numberOfItems"":2,
                ""itemListElement"":[
                    {
                        ""@type"":""ListItem"",
                        ""position"":1,
                        ""url"":""https://asaxiy.uz/product/garri-potter-1"",
                        ""name"":""Joanna Ketlin Rouling: \""Garri Potter\"" to'plami"",
                        ""image"":""https://assets.asaxiy.uz/product/p1.webp""
                    },
                    {
                        ""@type"":""ListItem"",
                        ""position"":2,
                        ""url"":""https://asaxiy.uz/product/alximik-a6"",
                        ""name"":""Paulo Koelo: Alximik"",
                        ""image"":""https://assets.asaxiy.uz/product/p2.webp""
                    }
                ]
            }</script>
        </head><body></body></html>";

        // AsaxiyBookService instance
        var service = new AsaxiyBookService(
            new FakeHttpClientFactory(),
            new AsaxiyTransportState(),
            new AsaxiyOptions(),
            new FakeCacheService(),
            NullLogger<AsaxiyBookService>.Instance);

        // We can test via reflection or public method if needed
        var method = typeof(AsaxiyBookService).GetMethod("ParseSearch",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var results = (IReadOnlyList<AsaxiyBookResult>)method.Invoke(null, new object[] { html })!;
        Assert.Equal(2, results.Count);
        Assert.Equal("\"Garri Potter\" to'plami", results[0].Title);
        Assert.Equal("Joanna Ketlin Rouling", results[0].Author);
        Assert.Equal("Alximik", results[1].Title);
        Assert.Equal("Paulo Koelo", results[1].Author);
    }

    [Fact]
    public void ParseSearch_Handles_Books_Without_Colon()
    {
        var html = @"<!DOCTYPE html><html><head>
            <script type=""application/ld+json"">{
                ""@context"":""https://schema.org"",
                ""@type"":""ItemList"",
                ""numberOfItems"":1,
                ""itemListElement"":[
                    {
                        ""@type"":""ListItem"",
                        ""position"":1,
                        ""url"":""https://asaxiy.uz/product/b1"",
                        ""name"":""Гарри Поттер и Проклятое дитя"",
                        ""image"":""https://assets.asaxiy.uz/product/p1.webp""
                    }
                ]
            }</script>
        </head><body></body></html>";

        var method = typeof(AsaxiyBookService).GetMethod("ParseSearch",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var results = (IReadOnlyList<AsaxiyBookResult>)method.Invoke(null, new object[] { html })!;
        Assert.Single(results);
        Assert.Equal("Гарри Поттер и Проклятое дитя", results[0].Title);
        Assert.Equal("Noma'lum muallif", results[0].Author);
    }

    [Fact]
    public void IsValidAsaxiyHtml_Detects_Cloudflare_Challenge()
    {
        var challengeHtml = "<!doctype html><html><head><title>Just a moment...</title></head><body>cf-mitigated</body></html>";
        var normalHtml = "<!doctype html><html><head><title>asaxiy kitoblar</title></head><body></body></html>";

        var method = typeof(AsaxiyBookService).GetMethod("IsValidAsaxiyHtml",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var isChallengeValid = (bool)method.Invoke(null, new object[] { challengeHtml })!;
        var isNormalValid = (bool)method.Invoke(null, new object[] { normalHtml })!;

        Assert.False(isChallengeValid);
        Assert.True(isNormalValid);
    }
}
