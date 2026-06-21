using System.Net.Http.Headers;

namespace KitobdaGimen.Infrastructure.External;

/// <summary>
/// asaxiy.uz so'rovlarini bepul Cloudflare Worker reverse-proxy orqali yo'naltiradi.
/// Hetzner server IP'si asaxiy Cloudflare WAF'da bloklangan; Worker'dan chiqqan
/// trafik Cloudflare tarmog'idan ketadi va blokka tushmaydi. SOCKS tunneldan
/// (Asaxiy:ProxyUrl) farqli — Worker har doim yoniq, uy kompyuteriga bog'liq emas.
///
/// Faqat host = asaxiy.uz (yoki *.asaxiy.uz) bo'lgan so'rovlar qayta yoziladi:
/// asl URL <c>{WorkerUrl}?url=...</c> ko'rinishiga o'tadi va <c>X-Proxy-Secret</c>
/// sarlavhasi qo'shiladi.
/// </summary>
public sealed class AsaxiyWorkerProxyHandler : DelegatingHandler
{
    private readonly string _workerUrl;
    private readonly string? _secret;

    public AsaxiyWorkerProxyHandler(string workerUrl, string? secret)
    {
        _workerUrl = workerUrl.TrimEnd('/');
        _secret = secret;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri;
        if (uri is not null && IsAsaxiyHost(uri.Host))
        {
            var rewritten = $"{_workerUrl}?url={Uri.EscapeDataString(uri.ToString())}";
            request.RequestUri = new Uri(rewritten);

            if (!string.IsNullOrEmpty(_secret))
            {
                request.Headers.TryAddWithoutValidation("X-Proxy-Secret", _secret);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static bool IsAsaxiyHost(string host)
    {
        host = host.ToLowerInvariant();
        return host == "asaxiy.uz" || host.EndsWith(".asaxiy.uz");
    }
}
