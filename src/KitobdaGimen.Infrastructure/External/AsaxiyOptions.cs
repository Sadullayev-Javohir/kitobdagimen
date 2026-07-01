namespace KitobdaGimen.Infrastructure.External;

/// <summary>
/// asaxiy.uz transport sozlamalari (appsettings / env'dan). Hech biri majburiy emas —
/// hech narsa berilmasa ham servis to'g'ridan-to'g'ri va Jina Reader orqali ishlaydi.
/// </summary>
public sealed class AsaxiyOptions
{
    /// <summary>Bepul Cloudflare Worker reverse-proxy URL'i (env: Asaxiy__WorkerUrl).</summary>
    public string? WorkerUrl { get; init; }

    /// <summary>Worker uchun maxfiy kalit (env: Asaxiy__WorkerSecret).</summary>
    public string? WorkerSecret { get; init; }

    /// <summary>O'zbekiston IP'sidagi SOCKS/HTTP proksi (env: Asaxiy__ProxyUrl).</summary>
    public string? ProxyUrl { get; init; }

    public bool HasWorker => !string.IsNullOrWhiteSpace(WorkerUrl);
    public bool HasProxy => !string.IsNullOrWhiteSpace(ProxyUrl);
}
