# Deploy kундaligi — kitobdagimen.uz (Hetzner)

**Sana:** 2026-06-20
**Natija:** ✅ Sayt jonli — **https://kitobdagimen.uz** (HTTPS, Google login, real-time chat ishlayapti)

Bu hujjat loyihani noldan Hetzner serverga joylashtirish jarayonini, yo'lda
uchragan **xatoliklarni** va ularning **yechimlarini** bosqichma-bosqich yozadi.
Maqsad — keyingi deploy yoki muammo bo'lganda shu kundalikka qarab tez hal qilish.

---

## 1. Umumiy ko'rinish

Joylashtirish tartibi (yakuniy, ishlagan ketma-ketlik):

1. **Xavfsizlik auditi + hardening** (kod tomonida) — deployдан oldin
2. **Public repo tayyorlash** — secret'lar GitHub'ga ketmasligi
3. **Hetzner server** sotib olish + bazaviy himoya (firewall, swap, fail2ban)
4. **Stack o'rnatish** — .NET 8, PostgreSQL, Redis, nginx
5. **DB** — baza + kam huquqli foydalanuvchi
6. **Kod** — `git clone` + `dotnet publish`
7. **Env var'lar** + **systemd service**
8. **nginx** reverse proxy + **Let's Encrypt** HTTPS
9. **Google OAuth** production sozlamasi

Yakuniy infratuzilma:

| Komponent | Qiymat |
|---|---|
| Server | Hetzner CX23, Ubuntu, 4 GB RAM, IP `204.168.192.197` (Helsinki) |
| Runtime | .NET 8 (`/usr/share/dotnet`), Kestrel `127.0.0.1:5000` |
| Service | systemd `kitobdagimen`, user `kitobapp` (kam huquqli) |
| DB | PostgreSQL 18, baza+user `kitobdagimen` (superuser emas) |
| Kesh | Redis 8 (parol bilan) |
| Proxy | nginx 1.28 + Let's Encrypt (auto-renew) |
| Env | `/etc/kitobdagimen/kitobdagimen.env` (chmod 600) |

---

## 2. Uchragan xatoliklar va yechimlar

### Xato #1 — `.NET SDK` paketi topilmadi

**Belgisi:**
```
apt install -y dotnet-sdk-8.0
Error: Unable to locate package dotnet-sdk-8.0
```

**Sabab:** Server juda yangi Ubuntu relizida edi (`resolute` / 26.04), uning
repozitoriysida `dotnet-sdk-8.0` paketi yo'q.

**Yechim:** Microsoft'ning rasmiy install skripti orqali o'rnatildi (distroga
bog'liq emas):
```bash
apt install -y libicu-dev
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet
ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
```
PostgreSQL/Redis/nginx esa Ubuntu repo'da bor edi — ular oddiy `apt` bilan o'rnatildi.

---

### Xato #2 — `global.json` SDK versiyasini bloklab qo'ydi

**Belgisi (potensial):** publish "A compatible .NET SDK was not found.
Requested 8.0.128" bilan to'xtashi mumkin edi.

**Sabab:** `global.json` da `"rollForward": "latestPatch"` — bu faqat `8.0.1xx`
feature bandini qabul qiladi. Serverga esa `8.0.422` (4xx band) o'rnatilgan edi.

**Yechim:** Repoda `rollForward` `latestFeature` ga o'zgartirildi (.NET 8
doirasida qoladi, lekin istalgan 8.0.x SDK bilan quriladi — lokal `8.0.128` ham,
server `8.0.422` ham ishlaydi). Laptopdан commit + push qilinib, server toza
versiyani clone qildi.

---

### Xato #3 — Terminal paste UZUN qatorlar va heredoc'ni BUZDI ⚠️ (eng ko'p vaqt olgan)

**Belgisi:** env fayl va systemd service'ni yaratishda:
- `cat > fayl <<EOF` ishlatilganda terminal `>` da osilib qolardi;
- `printf '...\n...'` da `\n` belgilar `n` ga aylanib, buyruq parchalanardi;
- `{ echo ...; echo ...; } > fayl` bitta uzun qatori o'rtasidan uzilib,
  `command not found` xatolari berardi;
- uzun bitta qator (~80+ belgi) nusxalashda o'rab (wrap) qilinib, ichiga
  yashirin yangi qator tushardi (oxirgi qator tushib qolardi).

**Sabab:** Foydalanuvchining SSH/clipboard yo'li uzun bitta qatorlarni segmentlaydi
va ko'p qatorli shell konstruksiyalarini (`<<EOF`, `{ }`) saqlamaydi.

**Yechim (ishlagan usul):**
- **Heredoc, `printf '\n'`, `{ }` guruhlardan voz kechildi.**
- Fayllar faqat **qisqa, mustaqil** `echo "kalit=$VAR" >> fayl` qatorlari bilan,
  bittalab yozildi (har qator ~75 belgidan kam, otступsiz).
- Uzun qiymatlar avval **bo'laklab** o'zgaruvchiga yig'ildi:
  ```bash
  CS="Host=localhost;Port=5432;Database=kitobdagimen"
  CS="$CS;Username=kitobdagimen;Password=$DB_PASS"
  echo "ConnectionStrings__DefaultConnection=$CS" >> kitobdagimen.env
  ```
- Maxfiy/uzun qiymatlar (Google Client ID/Secret) uchun `read -r VAR` ishlatildi
  — qiymat alohida bo'sh qatorga paste qilinadi, uzunlik/belgi muammosi bo'lmaydi.
- Har bir fayl yozilgach `cat` bilan tekshirildi.

> **Saboq:** bu terminalga HECH QACHON heredoc yoki uzun bitta qatorli buyruq
> bermaslik kerak — faqat qisqa `echo` qatorlari va `read -r`.

---

### Xato #4 — Ilova lokal so'rovga HTTP 400 qaytardi

**Belgisi:**
```
curl http://127.0.0.1:5000/   →   HTTP 400
```

**Sabab:** Bu **xato emas, balki himoya ishlayotgani edi.** Env'da
`AllowedHosts=kitobdagimen.uz` qo'yilgan; curl esa `Host: 127.0.0.1` bilan
so'rardi → ilova noto'g'ri host'ni ataylab rad etdi (Host-header himoyasi).

**Yechim:** To'g'ri host bilan tekshirildi:
```bash
curl -H "Host: kitobdagimen.uz" http://127.0.0.1:5000/   →   200/302
```
Domen orqali (nginx) kelganda so'rov to'g'ri host bilan keladi — muammo yo'q.

---

### Xato #5 — Domen boshqa serverga ishora qilardi

**Belgisi:** DNS yozuvlari `kitobdagimen.uz` ni `45.138.159.4` (eski hosting) ga
yo'naltirgan edi, Hetzner `204.168.192.197` ga emas. Bu holda Let's Encrypt
sertifikat ololmas edi.

**Sabab:** Domen avval boshqa hostingda turgan; apex `A` yozuvi eski IP'da edi.

**Yechim:** Domen panelida faqat **apex `A` yozuvi** `204.168.192.197` ga
o'zgartirildi. Pochta yozuvlari (`MX`, `mail`, `ftp`, `webmail`, `SPF`, `DMARC`)
tegilmadi — pochta ishlashda davom etdi. `www` (CNAME → apex) avtomatik ergashdi.
Tarqalish tekshirildi:
```bash
dig +short kitobdagimen.uz @8.8.8.8   →   204.168.192.197
```

---

### Xato #6 — Google OAuth `redirect_uri_mismatch`

**Belgisi:**
```
Xato 400: redirect_uri_mismatch
"Bu ilovaning so'rovi yaroqsiz"
```

**Sabab:** Google Console'ga noto'g'ri redirect URI qo'shilgan edi
(`/auth/google-callback`). Aslida .NET'ning Google moduli **standart
`/signin-google`** manzilidan foydalanadi — Google aynan shuni kutadi.
`/auth/google-callback` esa ichki (ilova Google'dan keyin shu yerga yo'naltiradi),
Google uni ko'rmaydi.

**Yechim:** Google Cloud Console → Credentials → Web client → Authorized redirect
URIs ga aynan shu qo'shildi:
```
https://kitobdagimen.uz/signin-google
```
Saqlangach (~5 daqiqa), Google login ishladi.

---

### Xato #7 — `dotnet publish` fayl band xatosi (`MSB3021` / `MSB3027`) ⚠️

**Belgisi (redeploy paytida):**
```
error MSB3027: Could not copy "obj/Release/net8.0/KitobdaGimen.Web.pdb" to
"/var/www/kitobdagimen/publish/KitobdaGimen.Web.pdb". Exceeded retry count of 10.
error MSB3021: Unable to copy file ... The process cannot access the file
'.../publish/KitobdaGimen.Web.pdb' because it is being used by another process.
```
(Xuddi shu `KitobdaGimen.Application.pdb`/`.dll` uchun ham takrorlanadi.)

**Sabab:** Redeploy buyrug'ida `systemctl restart` **publishdan keyin** turgan edi.
Ya'ni `dotnet publish` ishga tushganda `kitobdagimen` servisi hamon ishlab turgan
va `publish/` ichidagi `.dll`/`.pdb` fayllarni **band qilib** turgan. Publish o'sha
fayllar ustiga yoza olmay, 10 marta urinib xato bilan to'xtaydi.

**Yechim:** Publishdan **oldin** servisni to'xtatish, publishdan **keyin** ishga
tushirish. To'g'ri tartib: `stop → publish → chown → start`:
```bash
cd /var/www/kitobdagimen && git pull && systemctl stop kitobdagimen && dotnet publish src/KitobdaGimen.Web -c Release -o publish && chown -R kitobapp:kitobapp publish && systemctl start kitobdagimen
```

> **Saboq:** running servis publish papkasidagi fayllarni lock qiladi — HECH QACHON
> ishlab turgan servis ustiga publish qilmang; avval `systemctl stop`, keyin publish,
> oxirida `systemctl start`.

---

## 3. Yakuniy xavfsizlik holati

Deploy tugagach faol bo'lgan himoyalar:

- **HTTPS** (Let's Encrypt) + HTTP→HTTPS redirect + HSTS
- **Xavfsizlik sarlavhalari** — CSP, `X-Frame-Options: DENY`, `nosniff`,
  Referrer-Policy, Permissions-Policy
- **Rate limiting** — har IP 600 so'rov/daqiqa
- **JWT fail-fast** — zaif kalit bilan ishga tushmaydi
- **Hangfire dashboard** — faqat admin email
- **Fayl yuklash** — ImageSharp bilan qayta-kodlash (polyglot/skript yuklab bo'lmaydi)
- **Kam huquqli** DB foydalanuvchi (superuser emas) va app foydalanuvchi (`kitobapp`)
- **Redis paroli**, faqat `localhost` da tinglovchi DB/Redis
- **Firewall (ufw)** — faqat 22/80/443 ochiq
- **Public repo'da secret yo'q** — `.gitignore` + user-secrets/env fayl

---

## 4. Asosiy saboqlar

1. **Yangi Ubuntu'da .NET'ni apt'dan kutmang** — Microsoft `dotnet-install.sh`
   ishonchli.
2. **`global.json` rollForward'ni `latestFeature` qiling** — server SDK versiyasi
   lokaldan farq qilishi mumkin.
3. **SSH paste juda noziq** — heredoc/uzun qator ishlatmang; qisqa `echo` qatorlari
   va `read -r`.
4. **HTTP 400 = AllowedHosts himoyasi**, panika emas — to'g'ri `Host` bilan tekshiring.
5. **DNS'ni deploydan oldin tekshiring** — Let's Encrypt domen serverга
   ishlashini talab qiladi; pochta yozuvlariga tegmang.
6. **.NET Google OAuth redirect URI = `/signin-google`**, `/auth/google-callback` emas.
7. **Redeploy tartibi: `stop → publish → start`** — ishlab turgan servis `publish/`
   fayllarini lock qiladi; publishdan oldin `systemctl stop` qilmasa `MSB3021`/`MSB3027`
   (file in use) xatosi chiqadi.

---

## 5. Tegishli hujjatlar

- `docs/DEPLOY-SECURITY.md` — to'liq xavfsizlik checklisti + nginx/env namunalar
- `docs/PROGRESS.md` — sessiya tarixi (eng yuqori yozuv = deploy)
- Redeploy buyrug'i:
  ```bash
  cd /var/www/kitobdagimen && git pull && systemctl stop kitobdagimen && dotnet publish src/KitobdaGimen.Web -c Release -o publish && chown -R kitobapp:kitobapp publish && systemctl start kitobdagimen
  ```
