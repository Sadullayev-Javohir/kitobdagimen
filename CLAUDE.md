# kitobdagimen.uz — loyiha xotirasi

Bu fayl Claude Code tomonidan HAR BIR sessiya boshida avtomatik o'qiladi.
`/clear` qilingandan keyin ham, shu fayl orqali loyiha holatini eslab qol.

## MUHIM ISH QOIDASI

HECH QACHON tasdiq so'rama ("davom etaymi?", "shuni qilsam bo'ladimi?"). Vazifani to'liq bajar.
Har bir qadam tugagach, PROGRESS.md faylini albatta yangila — bu keyingi sessiya uchun xotira.

## LOYIHA HAQIDA

kitobdagimen.uz — o'zbek kitobxonlari uchun ijtimoiy veb-platforma.
To'liq texnik tafsilotlar: `docs/PROJECT-SPEC.md` faylida.
Dizayn ma'lumotnomasi: `design-reference/` papkasida (Stitch'dan eksport qilingan 9 ta sahifa).

## TEXNOLOGIK STACK (qisqacha)

.NET 8, Clean Architecture, CQRS+MediatR, PostgreSQL, Redis, SignalR, Hangfire, Mapster, FluentValidation, Google OAuth+JWT.

## HOZIRGI HOLAT

Joriy progress: `docs/PROGRESS.md` faylini O'QI — u yerda qaysi bosqich tugaganini, keyingi qadam nima ekanini ko'rasan.

## DIZAYN HAQIDA MUHIM ESLATMA

`design-reference/` papkasidagi fayllar Stitch AI orqali generatsiya
qilingan — ular XATO va NOMUVOFIQ bo'lishi mumkin (har xil navbar,
buzilgan kod, placeholder matnlar). Ulardan FAQAT vizual yo'nalishni ol,
kodni so'zma-so'z ko'chirma. To'liq qoida: `docs/PROJECT-SPEC.md` dagi
"MUHIM — Stitch kodi xom material" bo'limida.

## ISH TARTIBI

1. Avval `docs/PROGRESS.md` ni o'qi — qayerda to'xtaganingni bil
2. `docs/PROJECT-SPEC.md` dan shu bosqichga oid bo'limni o'qi
3. Faqat shu bosqichni bajar (boshqa bosqichlarga o'tma)
4. Bajarib bo'lgach, `docs/PROGRESS.md` ni yangila: bajarilgan bosqichni belgilang, keyingi bosqichni aniq yoz
5. Build qilib xatosiz ekanini tekshir