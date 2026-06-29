#!/usr/bin/env bash
#
# Release keystore yaratadi va Digital Asset Links uchun SHA-256 barmoq izini chiqaradi.
# Faqat BIR MARTA bajariladi — keystore yo'qolsa, ilovani Play'da yangilab bo'lmaydi,
# shuning uchun .jks faylini va parollarni XAVFSIZ saqlang (zaxira nusxa oling).
#
# Foydalanish:
#   ./make-keystore.sh
#
set -euo pipefail
cd "$(dirname "$0")"

KEYSTORE="kitobdagimen-release.jks"
ALIAS="kitobdagimen"

if [ -f "$KEYSTORE" ]; then
    echo "⚠️  '$KEYSTORE' allaqachon mavjud — qayta yaratilmaydi (ustiga yozish xavfli)."
else
    echo "🔑 Yangi release keystore yaratilmoqda: $KEYSTORE"
    echo "    Parolni eslab qoling va xavfsiz saqlang!"
    keytool -genkeypair \
        -v \
        -keystore "$KEYSTORE" \
        -alias "$ALIAS" \
        -keyalg RSA \
        -keysize 2048 \
        -validity 10000 \
        -dname "CN=kitobdagimen.uz, OU=Mobile, O=kitobdagimen, L=Tashkent, C=UZ"
    echo
    echo "✅ Keystore yaratildi."
    echo "   Endi 'keystore.properties' faylini to'ldiring (keystore.properties.example dan nusxa oling)."
fi

echo
echo "================ assetlinks.json uchun SHA-256 ================"
./print-fingerprint.sh "$KEYSTORE" "$ALIAS"
