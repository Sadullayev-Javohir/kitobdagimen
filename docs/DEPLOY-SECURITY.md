# Deploy va xavfsizlik qo'llanmasi (Hetzner + nginx)

Bu fayl serverga qo'yishdan oldin BAJARILISHI SHART bo'lgan xavfsizlik
qadamlarini sanaydi. Kod tomonidagi himoyalar allaqachon qo'shilgan
(`Program.cs`, `IdentityServiceExtensions.cs`, `HangfireDashboardAuthFilter.cs`);
bu yerda — serverda qo'lda sozlanadigan qism.

---

## 1. MAXFIY QIYMATLAR — repoga HECH QACHON kommit qilinmaydi

Public repo. Maxfiy qiymatlar faqat serverda **environment variable** sifatida
beriladi (yoki user-secrets — dev'da). `.gitignore` quyidagilarni bloklaydi:
`.claude/settings.local.json`, `appsettings.Production.json`, `*.pfx/*.pem/*.key`,
`secrets.json`, `appsettings.*.local.json`, `wwwroot/uploads/`.

`git push` qilishdan OLDIN tekshiring:

```bash
git status --porcelain          # kutilmagan fayl yo'qligini ko'ring
git ls-files | grep -iE "secret|\.pfx|\.pem|\.key|settings\.local|Production\.json"
# ^ hech narsa chiqmasligi kerak
git ls-files | grep -i "appsettings"
# ^ faqat appsettings.json va appsettings.Development.json (ikkalasida ham haqiqiy maxfiy qiymat YO'Q)
```

`appsettings.json` — placeholder shablon (Jwt:Key = "REPLACE_WITH...", Google bo'sh).
`appsettings.Development.json` — faqat dev (kalit "dev_only...change_in_production",
production'da ishlatilmaydi). Ikkalasi ham xavfsiz — kommit qilinadi.

## 2. Production environment variable'lari (SHART)

Systemd unit yoki Docker env orqali bering (`__` = bo'lim ajratuvchi):

```
ASPNETCORE_ENVIRONMENT=Production
Jwt__Key=<kamida 32 belgili, tasodifiy, MAXFIY>          # openssl rand -base64 48
Jwt__Issuer=kitobdagimen.uz
Jwt__Audience=kitobdagimen.uz
ConnectionStrings__DefaultConnection=Host=...;Database=...;Username=...;Password=...
ConnectionStrings__Redis=localhost:6379
Authentication__Google__ClientId=<google-client-id>
Authentication__Google__ClientSecret=<google-client-secret>
AllowedHosts=kitobdagimen.uz;www.kitobdagimen.uz
Hangfire__DashboardEmails__0=<admin-email@gmail.com>     # /hangfire kira oladigan admin
```

> **MUHIM:** `Jwt__Key` bo'lmasa yoki 32 belgidan qisqa bo'lsa, ilova ataylab
> ishga TUSHMAYDI (fail-fast). Bu zaif kalit bilan tokenlarni soxtalashtirishning
> oldini oladi. Yangi kalit: `openssl rand -base64 48`.

`AllowedHosts` ni o'z domeningizga cheklang (`*` emas) — Host-header hujumlariga qarshi.

## 3. nginx reverse proxy (TLS terminatsiya)

Ilova `app.UseForwardedHeaders()` bilan `X-Forwarded-Proto`/`X-Forwarded-For` ni
o'qiydi — shu sababli nginx ularni TO'G'RI qo'yishi SHART, aks holda:
- cookie `Secure` flagi qo'yilmaydi (HTTPS aniqlanmaydi);
- rate-limit barcha foydalanuvchini bitta IP'ga jamlaydi.

```nginx
server {
    listen 443 ssl http2;
    server_name kitobdagimen.uz www.kitobdagimen.uz;

    ssl_certificate     /etc/letsencrypt/live/kitobdagimen.uz/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/kitobdagimen.uz/privkey.pem;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;

        # SignalR (WebSocket) uchun:
        proxy_set_header   Upgrade    $http_upgrade;
        proxy_set_header   Connection "upgrade";
        proxy_read_timeout 100s;
    }

    # /hangfire ni xohlasangiz tashqaridan butunlay bekiting (admin filtri ustiga):
    # location /hangfire { allow <sizning-ip>; deny all; proxy_pass http://127.0.0.1:5000; }
}

server {                      # HTTP → HTTPS
    listen 80;
    server_name kitobdagimen.uz www.kitobdagimen.uz;
    return 301 https://$host$request_uri;
}
```

> nginx **yagona kirish nuqtasi** bo'lsin: Kestrel'ni faqat `127.0.0.1:5000` da
> tinglatib, tashqi portni (5000) firewall bilan yoping. Aks holda `UseForwardedHeaders`
> ning `KnownProxies.Clear()` sozlamasi mijoz IP'sini soxtalashtirishga yo'l ochadi.

## 4. Firewall (ufw) va Postgres/Redis

```bash
ufw default deny incoming
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable
```

- Postgres/Redis faqat `localhost` da tinglasin (tashqi portda EMAS). Redis'ga parol
  (`requirepass`) qo'ying yoki faqat loopback'ga bog'lang.
- Postgres uchun ilovaga alohida, kam huquqli foydalanuvchi yarating (superuser emas).

## 5. Google OAuth

Google Cloud Console'da production redirect URI qo'shing:
`https://kitobdagimen.uz/auth/google-callback`. Authorized JavaScript origins:
`https://kitobdagimen.uz`.

## 6. Kod tomonidagi himoyalar (allaqachon BAJARILGAN)

- **JWT fail-fast** — zaif/bo'sh imzo kaliti bilan ishga tushmaydi.
- **ForwardedHeaders** — proxy ortida to'g'ri HTTPS/IP.
- **Xavfsizlik sarlavhalari** — `X-Content-Type-Options: nosniff` (yuklangan rasm
  ichidagi yashirin skriptni bloklaydi), `X-Frame-Options: DENY` + CSP
  `frame-ancestors 'none'` (clickjacking), `Referrer-Policy`, `Permissions-Policy`,
  `Content-Security-Policy` (faqat ishonchli manbalar: self, Google Fonts, cdnjs, unpkg).
- **Rate limiting** — har IP uchun 600 so'rov/daqiqa (DoS/flood).
- **Hangfire dashboard** — faqat `Hangfire:DashboardEmails` dagi adminlar.
- **Fayl yuklash** — cover va post rasmlari ImageSharp bilan WebP'ga qayta-kodlanadi
  (content-type'ga ishonilmaydi; polyglot/EXE/HTML yuklab bo'lmaydi); 5–8 MB limit.
- **Antiforgery** — barcha holat o'zgartiruvchi POST endpointlarda token tekshiriladi.
- **Auth cookie** — HttpOnly, SameSite=Lax, HTTPS'da Secure.
- **Exception middleware** — production'da stack-trace/ichki xato sizib chiqmaydi.
- **CSRF/IDOR** — egalik tekshiruvi Application qatlamida (faqat muallif o'chiradi/tahrirlaydi).

## 7. Deploy keyin tezkor tekshiruv

```bash
curl -sI https://kitobdagimen.uz | grep -iE "content-security-policy|x-frame|x-content-type|strict-transport"
curl -s -o /dev/null -w "%{http_code}\n" https://kitobdagimen.uz/hangfire   # 401/302 kutiladi (admin emas)
```
