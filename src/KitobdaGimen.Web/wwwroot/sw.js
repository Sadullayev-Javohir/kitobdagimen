/*
 * kitobdagimen.uz — minimal service worker
 *
 * Maqsad: PWA "o'rnatish" mezonlarini qondirish va Android TWA sifat talablariga
 * mos kelish. ATAYLAB juda ehtiyotkor:
 *   - Navigatsiya (HTML sahifalar) HECH QACHON keshlanmaydi -> auth/dinamik kontent
 *     hech qachon eskirmaydi (stale bo'lmaydi).
 *   - Faqat o'zgarmas statik aktivlar (css/js/img/icons/fonts) "stale-while-revalidate"
 *     bilan keshlanadi.
 *   - GET bo'lmagan so'rovlar, hublar (/hubs/...) va yuklamalar (/uploads/...) chetlab
 *     o'tiladi.
 */
const VERSION = 'v1';
const STATIC_CACHE = 'kg-static-' + VERSION;

// Versiya almashganda eski keshlarni tozalash.
self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(keys.filter((k) => k !== STATIC_CACHE).map((k) => caches.delete(k)))
    ).then(() => self.clients.claim())
  );
});

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(STATIC_CACHE).then((cache) => cache.addAll(['/offline.html'])).catch(() => {})
  );
  self.skipWaiting();
});

function isStaticAsset(url) {
  return (
    url.pathname.startsWith('/css/') ||
    url.pathname.startsWith('/js/') ||
    url.pathname.startsWith('/img/') ||
    url.pathname.startsWith('/lib/') ||
    url.pathname === '/manifest.webmanifest' ||
    url.pathname === '/favicon.ico'
  );
}

self.addEventListener('fetch', (event) => {
  const req = event.request;

  // Faqat GET; boshqalarini (POST/PUT...) tarmoqqa qoldiramiz.
  if (req.method !== 'GET') return;

  const url = new URL(req.url);

  // Faqat o'z domenimiz; tashqi (fonts/cdn) so'rovlarga tegmaymiz.
  if (url.origin !== self.location.origin) return;

  // Real-time hublar va foydalanuvchi yuklamalari keshlanmaydi.
  if (url.pathname.startsWith('/hubs/') || url.pathname.startsWith('/uploads/')) return;

  // Statik aktivlar: stale-while-revalidate.
  if (isStaticAsset(url)) {
    event.respondWith(
      caches.open(STATIC_CACHE).then(async (cache) => {
        const cached = await cache.match(req);
        const network = fetch(req)
          .then((res) => {
            if (res && res.status === 200 && res.type === 'basic') {
              cache.put(req, res.clone());
            }
            return res;
          })
          .catch(() => cached);
        return cached || network;
      })
    );
    return;
  }

  // Navigatsiya (HTML): doim tarmoq-birinchi, keshlamaymiz.
  if (req.mode === 'navigate') {
    event.respondWith(fetch(req).catch(() => caches.match('/offline.html')));
    return;
  }
});

// ===== Web Push: bildirishnomalarni qabul qilish va ko'rsatish =====
// Server VAPID orqali yuborgan push'ni Android tizim bildirishnomasiga aylantiramiz
// (TWA uni telefon bildirishnoma tovoqchasida ko'rsatadi — Telegram kabi).
self.addEventListener('push', (event) => {
  let data = {};
  try { data = event.data ? event.data.json() : {}; }
  catch (e) { data = { body: event.data ? event.data.text() : '' }; }

  const title = data.title || 'kitobdagimen.uz';
  const options = {
    body: data.body || '',
    icon: data.icon || '/img/icons/icon-192.png',
    badge: '/img/icons/icon-192.png',
    tag: data.tag || undefined,
    renotify: !!data.tag,
    data: { url: data.url || '/' }
  };
  event.waitUntil(self.registration.showNotification(title, options));
});

// Bildirishnoma bosilganda — ilovani ochamiz/fokuslaymiz va kerakli sahifaga o'tamiz.
self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  const url = (event.notification.data && event.notification.data.url) || '/';
  event.waitUntil((async () => {
    const all = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
    for (const client of all) {
      if ('focus' in client) {
        await client.focus();
        if ('navigate' in client && url) { try { await client.navigate(url); } catch (e) { /* ignore */ } }
        return;
      }
    }
    if (self.clients.openWindow) { await self.clients.openWindow(url); }
  })());
});
