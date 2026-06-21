# asaxiy.uz uchun bepul Cloudflare Worker proksi

## Muammo

Hetzner (Helsinki) server IP'si asaxiy.uz Cloudflare WAF tomonidan **403** bilan
bloklangan. Hozirgi yechim — uy kompyuteridan SSH SOCKS tunnel — faqat **desktop
yoniq** bo'lganda ishlaydi.

## Yechim — Cloudflare Worker (har doim yoniq, bepul)

asaxiy.uz **o'zi** Cloudflare ortida. Cloudflare Worker'dan yuborilgan `fetch()`
Cloudflare tarmog'idan chiqadi — Hetzner ASN'idan emas — shu sababli asaxiy'ning
IP/ASN blok qoidasiga **tushmaydi**. Worker serverless, doim ishlaydi, uy
kompyuteriga bog'liq emas. Free plan: **100 000 so'rov/kun** (importga ortig'i bilan yetadi).

Skript: [`asaxiy-proxy-worker.js`](./asaxiy-proxy-worker.js)

---

## A) Deploy — Dashboard orqali (eng oson, terminal shart emas)

1. https://dash.cloudflare.com → **Workers & Pages** → **Create** → **Create Worker**.
2. Nom ber (mas. `asaxiy-proxy`) → **Deploy** → **Edit code**.
3. `deploy/asaxiy-proxy-worker.js` ichidagi kodni to'liq nusxalab, tahrirlagichga
   joylashtir → **Deploy**.
4. **Settings → Variables and Secrets** → **Add** → `PROXY_SECRET` nomli **Secret**
   yarat (uzun tasodifiy qiymat, mas. `openssl rand -hex 24` natijasi).
5. URL'ni yozib ol: `https://asaxiy-proxy.<sub>.workers.dev`

## B) Deploy — Wrangler CLI orqali

```bash
npm i -g wrangler
wrangler login
cd deploy
# wrangler.toml yarat:
cat > wrangler.toml <<'EOF'
name = "asaxiy-proxy"
main = "asaxiy-proxy-worker.js"
compatibility_date = "2024-11-01"
EOF
wrangler deploy
wrangler secret put PROXY_SECRET   # qiymatni kirit
```

---

## Serverni sozlash (Hetzner)

`/etc/kitobdagimen/kitobdagimen.env` fayliga qo'sh (eski `Asaxiy__ProxyUrl` ni
o'chirish shart emas — WorkerUrl berilsa SOCKS proksi avtomatik e'tiborsiz qoldiriladi):

```
Asaxiy__WorkerUrl=https://asaxiy-proxy.<sub>.workers.dev
Asaxiy__WorkerSecret=<PROXY_SECRET bilan bir xil qiymat>
```

So'ng:

```bash
systemctl restart kitobdagimen
```

## Tekshirish

```bash
# Worker to'g'ridan-to'g'ri (serverdan yoki istalgan joydan):
curl -s -H "X-Proxy-Secret: <SECRET>" \
  "https://asaxiy-proxy.<sub>.workers.dev/?url=$(python3 -c 'import urllib.parse;print(urllib.parse.quote("https://asaxiy.uz/uz/product/knigi?key=qo%27rqma"))')" \
  | grep -o '"@type":"ItemList"' | head
# JSON-LD chiqsa — Worker asaxiy'ga yetayapti.
```

So'ng saytda kitob qidirib import qilib ko'r — desktop **o'chiq** bo'lsa ham ishlashi kerak.

---

## Mexanizm ishlamasa (zaxira reja)

Agar asaxiy Cloudflare Worker subrequest'larini ham bloklasa (kamdan-kam), Worker
javobida `502 upstream error` yoki asaxiy 403'i ko'rinadi. U holda mavjud SSH SOCKS
tunnel (`Asaxiy__ProxyUrl`) yagona ishonchli yo'l bo'lib qoladi — WorkerUrl ni
env'dan olib tashlab, tunnelga qaytiladi.

## Xavfsizlik

- Worker **ochiq SSRF proksi EMAS**: faqat `*.asaxiy.uz` hostlariga ruxsat beradi.
- `PROXY_SECRET` orqali faqat bizning server foydalanadi (boshqalar 403 oladi).
- C# tomonda ham `AsaxiyBookService` allaqachon faqat asaxiy URL'larini yuboradi (SSRF guard).
