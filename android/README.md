# kitobdagimen.uz — Android ilova (TWA)

Bu papka `kitobdagimen.uz` saytini **Trusted Web Activity (TWA)** sifatida o'rab,
Play Store'ga yoki to'g'ridan-to'g'ri qurilmaga o'rnatish mumkin bo'lgan native
Android ilovaga aylantiradi.

## Nima uchun TWA (oddiy WebView emas)?

| Mezon | Oddiy WebView | TWA (bu loyiha) |
|------|----------------|------------------|
| Google OAuth login | ❌ Google bloklaydi (`disallowed_useragent`) | ✅ ishlaydi (Chrome Custom Tabs) |
| Play Protect "xavfli" ogohlantirishi | ⚠️ ehtimoli yuqori | ✅ minimal xavf (faqat saytni Chrome'da ochadi) |
| Cookie / sessiya / SignalR / fayl yuklash | qisman | ✅ to'liq (haqiqiy Chrome dvigateli) |
| URL paneli | doim ko'rinadi | ✅ asset-links tasdiqlansa yashiriladi |
| Yangilanish | APK qayta chiqarish | ✅ sayt yangilansa avtomatik |

TWA — bu Google rasman tavsiya qiladigan, "web sayt → Play Store ilova" yo'li.
Ilovada o'z Java/Kotlin kodimiz **yo'q**: Google'ning `androidbrowserhelper`
kutubxonasidagi tayyor `LauncherActivity` ishlatiladi.

## Play Protect "xavfli" demasligi uchun shartlar

1. **Haqiqiy release keystore bilan imzolang** (debug imzo emas). → `./make-keystore.sh`
2. **Minimal ruxsatlar** — manifestda faqat `INTERNET`. Shubhali ruxsat yo'q.
3. **Digital Asset Links** — domeningizda `/.well-known/assetlinks.json` ilovaning
   SHA-256 imzo barmoq iziga mos bo'lishi shart (quyida 5-qadam). Bu app↔sayt
   ishonchini tasdiqlaydi.
4. **Eng ishonchli yo'l — Google Play orqali tarqatish.** Play imzolagan ilovalarni
   Play Protect ishonchli deb biladi. (Yon yuklangan toza TWA odatda o'tadi, lekin
   Play — eng kafolatli.)
5. Kod obfuskatsiya/packer ishlatilmaydi, `targetSdk` zamonaviy (34).

## Talablar

- **JDK 17+** (loyihada Java 21 tekshirilgan)
- **Android SDK** (`cmdline-tools`, `platform-tools`, `platforms;android-34`,
  `build-tools;34.0.0`) — Android Studio bilan yoki `sdkmanager` orqali.
- Gradle alohida shart emas — `./gradlew` o'zi 8.7 versiyani yuklab oladi.

### Android SDK o'rnatish (Android Studio'siz, CLI)

```bash
# 1) cmdline-tools ni yuklab oling: https://developer.android.com/studio#command-line-tools-only
# 2) Masalan:
export ANDROID_HOME="$HOME/Android/Sdk"
mkdir -p "$ANDROID_HOME/cmdline-tools"
# (yuklab olingan zipni "latest" papkasiga oching)
yes | "$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" --licenses
"$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" \
    "platform-tools" "platforms;android-34" "build-tools;34.0.0"
```

Gradle SDK joyini topishi uchun `android/local.properties` yarating:

```properties
sdk.dir=/home/<user>/Android/Sdk
```

> `local.properties` va `keystore.properties` **kommit qilinmaydi** (.gitignore).

## Qurish

### Debug (test uchun, tez)

```bash
cd android
./gradlew assembleDebug
# Natija: app/build/outputs/apk/debug/app-debug.apk
```

### Release (imzolangan — tarqatish uchun)

```bash
cd android
./make-keystore.sh            # bir marta: keystore yaratadi + SHA-256 ni chiqaradi
cp keystore.properties.example keystore.properties
#  -> keystore.properties ni to'ldiring (parollar, alias)

./gradlew assembleRelease     # imzolangan APK
#   Natija: app/build/outputs/apk/release/app-release.apk

./gradlew bundleRelease       # Play Store uchun AAB
#   Natija: app/build/outputs/bundle/release/app-release.aab
```

## Imzo barmoq izini saytga ulash (MUHIM)

TWA "xavfli emas" va URL panelisiz ishlashi uchun ikki tomonlama ishonch kerak:

1. **Ilova → sayt**: `app/src/main/res/values/strings.xml` dagi `asset_statements`
   (domen ko'rsatilgan — allaqachon `https://kitobdagimen.uz`).
2. **Sayt → ilova**: serverda `/.well-known/assetlinks.json` ilova imzosining
   SHA-256 barmoq izini qaytarishi kerak.

Barmoq izini oling:

```bash
cd android
./print-fingerprint.sh        # release keystore'dan SHA-256
```

So'ng uni serverga qo'shing — `src/KitobdaGimen.Web/appsettings.json`:

```json
"Android": {
  "PackageName": "uz.kitobdagimen.twa",
  "Sha256CertFingerprints": [ "AA:BB:CC:...:99" ]
}
```

(Yoki environment: `Android__Sha256CertFingerprints__0=AA:BB:...`)

> **Google Play App Signing**: agar Play ilovangizni qayta imzolasa, Play Console →
> *Setup → App integrity* dagi SHA-256 ni ham shu massivga QO'SHING. Aks holda
> Play orqali o'rnatilgan ilovada asset-link tasdiqlanmaydi.

Tekshirish:

```bash
curl https://kitobdagimen.uz/.well-known/assetlinks.json
```

## Qurilmaga o'rnatish (adb)

```bash
adb install -r android/app/build/outputs/apk/release/app-release.apk
```

Telefonda ilovani oching. Asset-links to'g'ri bo'lsa, sayt **URL panelisiz**,
to'liq ekran rejimida ochiladi.

## Versiyani oshirish

Har bir yangi reliz uchun `app/build.gradle` da:

```gradle
versionCode 2          // har safar +1 (butun son)
versionName "1.0.1"    // ko'rinadigan versiya
```

## Sozlamalar qisqa ma'lumotnomasi

| Narsa | Qayerda |
|------|---------|
| Paket nomi (`applicationId`) | `app/build.gradle` → `uz.kitobdagimen.twa` |
| Ochiladigan URL | `res/values/strings.xml` → `launch_url` |
| Domen (deep link) | `res/values/strings.xml` → `host` |
| Ilova nomi | `res/values/strings.xml` → `app_name` |
| Ranglar / splash | `res/values/colors.xml`, `res/drawable/splash.xml` |
| Ikon | `res/mipmap-*` (logodan generatsiya qilingan) |

## Eslatma

`assetlinks.json` to'ldirilmaguncha ilova baribir ishlaydi, lekin yuqorida URL
paneli ko'rinib turadi (ishonch tasdiqlanmagani uchun). Barmoq izi qo'shilgach,
keyingi ochilishda panel yo'qoladi.
