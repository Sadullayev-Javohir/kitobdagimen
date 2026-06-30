/*
 * Web Push obunasi (mijoz tomoni).
 * Kirgan foydalanuvchida: service worker tayyor bo'lgach VAPID public key olinadi,
 * brauzer push obunasi yaratiladi va serverga (/push/subscribe) saqlanadi. Shundan so'ng
 * server yuborgan bildirishnomalar telefon bildirishnoma tovoqchasida (TWA orqali) chiqadi.
 */
(function () {
    if (!('serviceWorker' in navigator) || !('PushManager' in window) || !('Notification' in window)) {
        return;
    }
    var body = document.body;
    if (!body || body.getAttribute('data-authenticated') !== 'true') {
        return; // faqat kirgan foydalanuvchi uchun
    }

    function token() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function urlB64ToUint8Array(base64String) {
        var padding = '='.repeat((4 - (base64String.length % 4)) % 4);
        var base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        var raw = atob(base64);
        var out = new Uint8Array(raw.length);
        for (var i = 0; i < raw.length; i++) { out[i] = raw.charCodeAt(i); }
        return out;
    }

    async function getPublicKey() {
        try {
            var r = await fetch('/push/public-key', { 
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                credentials: 'include'
            });
            if (!r.ok) return '';
            var d = await r.json();
            return d.publicKey || '';
        } catch (e) { return ''; }
    }

    async function subscribe() {
        var pk = await getPublicKey();
        if (!pk) return; // server'da push sozlanmagan
        var reg = await navigator.serviceWorker.ready;
        var sub = await reg.pushManager.getSubscription();
        if (!sub) {
            sub = await reg.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlB64ToUint8Array(pk)
            });
        }
        var raw = sub.toJSON();
        await fetch('/push/subscribe', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
            credentials: 'include',
            body: JSON.stringify({ endpoint: raw.endpoint, keys: raw.keys })
        });
    }

    async function enable() {
        if (Notification.permission === 'granted') { await subscribe(); return 'granted'; }
        if (Notification.permission === 'denied') { return 'denied'; }
        var perm = await Notification.requestPermission();
        if (perm === 'granted') { await subscribe(); }
        return perm;
    }

    async function disable() {
        try {
            var reg = await navigator.serviceWorker.ready;
            var sub = await reg.pushManager.getSubscription();
            if (sub) {
                var ep = sub.endpoint;
                await sub.unsubscribe();
                await fetch('/push/unsubscribe', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                    credentials: 'include',
                    body: JSON.stringify({ endpoint: ep })
                });
            }
        } catch (e) { /* jim */ }
    }

    window.kitobPush = { enable: enable, disable: disable, subscribe: subscribe };

    // Avtomatik: ruxsat bor bo'lsa jim obuna bo'lamiz; aks holda bir marta so'raymiz.
    window.addEventListener('load', function () {
        if (Notification.permission === 'granted') {
            subscribe().catch(function () { });
        } else if (Notification.permission === 'default') {
            setTimeout(function () { enable().catch(function () { }); }, 2000);
        }
    });
})();
