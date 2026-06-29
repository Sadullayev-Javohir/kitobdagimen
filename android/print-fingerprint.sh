#!/usr/bin/env bash
#
# Keystore'dan SHA-256 sertifikat barmoq izini chiqaradi (assetlinks.json formatida).
# Bu qiymatni serverdagi appsettings -> Android:Sha256CertFingerprints[] ga qo'ying.
#
# Foydalanish:
#   ./print-fingerprint.sh [keystore.jks] [alias]
#
set -euo pipefail
cd "$(dirname "$0")"

KEYSTORE="${1:-kitobdagimen-release.jks}"
ALIAS="${2:-kitobdagimen}"

if [ ! -f "$KEYSTORE" ]; then
    echo "❌ Keystore topilmadi: $KEYSTORE" >&2
    echo "   Avval ./make-keystore.sh ni ishga tushiring." >&2
    exit 1
fi

echo "Keystore: $KEYSTORE   Alias: $ALIAS"
echo "(parol so'ralsa, keystore parolini kiriting)"
echo

FP=$(keytool -list -v -keystore "$KEYSTORE" -alias "$ALIAS" 2>/dev/null \
    | grep -i 'SHA256:' | head -1 | sed 's/.*SHA256: *//' | tr -d '[:space:]')

if [ -z "$FP" ]; then
    echo "❌ SHA-256 barmoq izi o'qilmadi (parol noto'g'ri yoki alias xato?)." >&2
    exit 1
fi

echo "SHA-256: $FP"
echo
echo "appsettings.json ichiga qo'ying:"
echo "  \"Android\": {"
echo "    \"PackageName\": \"uz.kitobdagimen.twa\","
echo "    \"Sha256CertFingerprints\": [ \"$FP\" ]"
echo "  }"
echo
echo "Eslatma: Google Play App Signing ishlatsangiz, Play Console -> App Integrity"
echo "dagi SHA-256 ni ham shu ro'yxatga QO'SHING (aks holda Play orqali o'rnatilgan"
echo "ilovada asset-link tasdiqlanmaydi)."
