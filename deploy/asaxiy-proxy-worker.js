// asaxiy.uz uchun bepul Cloudflare Worker reverse-proxy.
//
// MAQSAD: Hetzner (Helsinki) server IP'si asaxiy.uz Cloudflare WAF tomonidan
// 403 bilan bloklangan. asaxiy.uz O'ZI Cloudflare ortida turgani uchun, bu
// Worker'dan yuborilgan fetch() so'rovi Cloudflare tarmog'idan chiqadi —
// Hetzner ASN'idan emas — shu sababli IP/ASN blok qoidasiga tushmaydi.
// Worker har doim yoniq (serverless), shuning uchun desktop SSH tunnel'dan
// farqli ravishda uy kompyuteri o'chiq bo'lsa ham ishlaydi.
//
// Free plan: kuniga 100 000 so'rov — importga ko'pdan-ko'p yetadi.
//
// FOYDALANISH: GET https://<worker>.workers.dev/?url=<asaxiy-url-encoded>
//   X-Proxy-Secret: <maxfiy kalit>   (PROXY_SECRET o'rnatilgan bo'lsa majburiy)
//
// XAVFSIZLIK: faqat *.asaxiy.uz hostlariga ruxsat (ochiq SSRF proksi BO'LMASIN),
// va PROXY_SECRET orqali faqat bizning server foydalanadi.

const ALLOWED_HOST = /^([a-z0-9-]+\.)*asaxiy\.uz$/i;

const BROWSER_HEADERS = {
  "User-Agent":
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
    "(KHTML, like Gecko) Chrome/124.0 Safari/537.36",
  "Accept-Language": "uz,en;q=0.8",
  Accept: "text/html,application/xhtml+xml,application/xml;q=0.9,image/*,*/*;q=0.8",
};

export default {
  async fetch(request, env) {
    // 1) Auth — faqat maxfiy kalitni biluvchi (bizning server) o'tadi.
    if (env.PROXY_SECRET) {
      if (request.headers.get("X-Proxy-Secret") !== env.PROXY_SECRET) {
        return new Response("forbidden", { status: 403 });
      }
    }

    // 2) Maqsad URL'ni olish va tekshirish.
    const target = new URL(request.url).searchParams.get("url");
    if (!target) {
      return new Response("missing ?url", { status: 400 });
    }

    let dest;
    try {
      dest = new URL(target);
    } catch {
      return new Response("bad url", { status: 400 });
    }

    if (
      (dest.protocol !== "https:" && dest.protocol !== "http:") ||
      !ALLOWED_HOST.test(dest.hostname)
    ) {
      return new Response("host not allowed", { status: 403 });
    }

    // 3) asaxiy'ga brauzer ko'rinishida so'rov.
    let upstream;
    try {
      upstream = await fetch(dest.toString(), {
        headers: BROWSER_HEADERS,
        redirect: "follow",
        cf: { cacheTtl: 300, cacheEverything: false },
      });
    } catch (err) {
      return new Response("upstream error: " + err, { status: 502 });
    }

    // 4) Javobni o'zgartirmasdan qaytarish (HTML yoki muqova rasmi).
    const headers = new Headers();
    const ct = upstream.headers.get("content-type");
    if (ct) headers.set("content-type", ct);
    headers.set("cache-control", "no-store");
    return new Response(upstream.body, {
      status: upstream.status,
      headers,
    });
  },
};
