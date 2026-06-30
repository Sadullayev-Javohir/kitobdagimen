// ===== kitobdagimen.uz — umumiy frontend skriptlari =====
(function () {
    "use strict";

    // ===== Sahifa yuklanish loaderi =====
    // Barcha resurslar (rasm, shrift) yuklangach loaderni yashiramiz.
    (function () {
        var loader = document.getElementById("pageLoader");
        if (!loader) return;
        var hidden = false;
        function hide() {
            if (hidden) return;
            hidden = true;
            loader.classList.add("is-hidden");
            // Animatsiya tugagach DOM'dan butunlay olib tashlaymiz
            window.setTimeout(function () {
                if (loader && loader.parentNode) loader.parentNode.removeChild(loader);
            }, 450);
        }
        if (document.readyState === "complete") {
            hide();
        } else {
            window.addEventListener("load", hide);
            // Xavfsizlik chorasi: load hodisasi kechiksa ham loader 6 soniyada yo'qoladi
            window.setTimeout(hide, 6000);
        }
    })();

    // ===== Kun/tun rejimi (dark mode) =====
    // Boshlang'ich qiymat <head> dagi inline skript orqali allaqachon o'rnatilgan.
    function applyTheme(theme) {
        var root = document.documentElement;
        root.classList.add("theme-transition");
        root.setAttribute("data-theme", theme);
        try { localStorage.setItem("kitob-theme", theme); } catch (e) { }
        window.setTimeout(function () { root.classList.remove("theme-transition"); }, 300);
    }
    document.addEventListener("click", function (e) {
        var toggle = e.target.closest("[data-theme-toggle]");
        if (!toggle) return;
        e.preventDefault();
        var current = document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
        applyTheme(current === "dark" ? "light" : "dark");
    });

    // Mobil burger menyu
    const burger = document.querySelector("[data-burger]");
    const navLinks = document.querySelector("[data-nav-links]");
    if (burger && navLinks) {
        burger.addEventListener("click", () => navLinks.classList.toggle("open"));
    }

    // Like tugmalari (feed, profil, post detali) — delegatsiya
    document.addEventListener("click", async (e) => {
        const btn = e.target.closest(".like-btn");
        if (!btn) return;
        e.preventDefault();
        const id = btn.getAttribute("data-like");
        try {
            const result = await apiPost(`/posts/${id}/like`);
            if (!result) return;
            btn.classList.toggle("liked", result.isLiked);
            const countEl = btn.querySelector(".like-count");
            if (countEl) countEl.textContent = result.likeCount;
        } catch (err) {
            alert(err.message);
        }
    });

    // Follow tugmasi (profil, feed post kartasi) — delegatsiya
    document.addEventListener("click", async (e) => {
        const btn = e.target.closest("[data-follow]");
        if (!btn) return;
        e.preventDefault();
        const id = btn.getAttribute("data-follow");
        const fromFeed = btn.getAttribute("data-follow-source") === "feed";
        try {
            const result = await apiPost(`/profile/${id}/follow`);
            if (!result) return;
            // Bir xil muallifga tegishli barcha tugmalarni (masalan feedda bir nechta post) sinxronlash.
            document.querySelectorAll(`[data-follow="${id}"]`).forEach((b) => {
                b.classList.toggle("btn-primary", !result.isFollowing);
                b.classList.toggle("btn-outline", result.isFollowing);
                b.textContent = result.isFollowing ? "Kuzatilmoqda" : "Kuzatish";
            });
            const countEl = document.querySelector("[data-follower-count]");
            if (countEl) countEl.textContent = result.followerCount;
            // Feedda yangi kuzatilgan muallifning postlari ko'rinishi uchun ro'yxatni yangilaymiz.
            if (result.isFollowing && fromFeed) {
                setTimeout(() => window.location.reload(), 400);
            }
        } catch (err) { alert(err.message); }
    });

    // Izohni o'chirish (faqat egasiga ko'rinadi) — delegatsiya
    document.addEventListener("click", async (e) => {
        const btn = e.target.closest("[data-delete-comment]");
        if (!btn) return;
        e.preventDefault();
        if (!confirm("Izohni o'chirmoqchimisiz?")) return;
        const id = btn.getAttribute("data-delete-comment");
        try {
            await apiPost(`/posts/comment/${id}/delete`);
            const node = btn.closest(".comment");
            if (node) node.remove();
        } catch (err) { alert(err.message); }
    });

    // Iqtibos saqlash tugmasi — delegatsiya
    document.addEventListener("click", async (e) => {
        const btn = e.target.closest("[data-save-quote]");
        if (!btn) return;
        e.preventDefault();
        const id = btn.getAttribute("data-save-quote");
        try {
            const result = await apiPost(`/quotes/${id}/save`);
            if (!result) return;
            btn.classList.toggle("liked", result.isSaved);
            const countEl = btn.querySelector(".save-count");
            if (countEl) countEl.textContent = result.saveCount;
        } catch (err) { alert(err.message); }
    });

    // Admin moderatsiyasi — istalgan post/iqtibosni o'chirish (admin/super admin uchun).
    document.addEventListener("click", async (e) => {
        const pBtn = e.target.closest("[data-admin-delete-post]");
        const qBtn = e.target.closest("[data-admin-delete-quote]");
        if (!pBtn && !qBtn) return;
        e.preventDefault();
        const isPost = !!pBtn;
        const id = (pBtn || qBtn).getAttribute(isPost ? "data-admin-delete-post" : "data-admin-delete-quote");
        if (!confirm(isPost ? "Bu postni o'chirishni tasdiqlaysizmi? (admin)" : "Bu iqtibosni o'chirishni tasdiqlaysizmi? (admin)")) return;
        try {
            await apiPost(`/admin/${isPost ? "posts" : "quotes"}/${id}/delete`);
            const card = (pBtn || qBtn).closest(".post-card, .quote-card, .pd-article");
            if (card) { card.style.transition = "opacity .2s"; card.style.opacity = "0"; setTimeout(() => card.remove(), 200); }
            showToast(isPost ? "Post o'chirildi." : "Iqtibos o'chirildi.");
        } catch (err) { alert(err.message); }
    });

    // Post tahrirlash/o'chirish — "[data-post-scope]" ichida ham feed kartasi (.post-card),
    // ham post detali sahifasi (.pd-article) bir xil belgilar bilan ishlaydi.
    function setEditing(scope, editing) {
        scope.querySelectorAll("[data-view-panel]").forEach((el) => { el.hidden = editing; });
        const panel = scope.querySelector("[data-edit-panel]");
        if (panel) panel.hidden = !editing;
    }

    document.addEventListener("click", (e) => {
        const btn = e.target.closest("[data-edit-post]");
        if (!btn) return;
        e.preventDefault();
        const scope = btn.closest("[data-post-scope]");
        if (!scope) return;
        setEditing(scope, true);
        const editor = scope.querySelector("[data-rich-editor]");
        if (editor && editor.richEditor) editor.richEditor.focus();
    });

    // Tahrirlashni bekor qilish — asl matn/rasmni qayta tiklaydi
    document.addEventListener("click", (e) => {
        const btn = e.target.closest("[data-cancel-edit-post]");
        if (!btn) return;
        e.preventDefault();
        const scope = btn.closest("[data-post-scope]");
        if (!scope) return;
        const editor = scope.querySelector("[data-rich-editor]");
        const output = scope.querySelector("[data-edit-text]");
        const urlInput = scope.querySelector("[data-edit-image-url]");
        const preview = scope.querySelector("[data-edit-image-preview]");
        const previewImg = scope.querySelector("[data-edit-image-preview-img]");
        if (editor && editor.richEditor && output) {
            editor.richEditor.setHtml(output.getAttribute("data-original-text") || "");
        }
        if (urlInput) {
            const originalUrl = urlInput.getAttribute("data-original-url") || "";
            urlInput.value = originalUrl;
            if (previewImg) previewImg.src = originalUrl;
            if (preview) preview.hidden = !originalUrl;
        }
        setEditing(scope, false);
    });

    // Tahrir panelidagi rasm tanlash — yuklab, serverdan qaytgan URL'ni saqlaydi
    document.addEventListener("change", async (e) => {
        const input = e.target.closest("[data-edit-image-input]");
        if (!input) return;
        const file = input.files && input.files[0];
        if (!file) return;
        const scope = input.closest("[data-post-scope]");
        const urlInput = scope.querySelector("[data-edit-image-url]");
        const preview = scope.querySelector("[data-edit-image-preview]");
        const previewImg = scope.querySelector("[data-edit-image-preview-img]");

        const reader = new FileReader();
        reader.onload = (ev) => {
            previewImg.src = ev.target.result;
            preview.hidden = false;
        };
        reader.readAsDataURL(file);

        try {
            const fd = new FormData();
            fd.append("file", file);
            const res = await fetch("/posts/upload-image", {
                method: "POST",
                headers: { "X-Requested-With": "XMLHttpRequest", "RequestVerificationToken": antiforgeryToken() },
                credentials: "include",
                body: fd
            });
            if (res.status === 401) { window.location.href = "/"; return; }
            if (!res.ok) {
                let message = "Rasmni yuklab bo'lmadi.";
                try { const d = await res.json(); message = d.message || message; } catch { /* ignore */ }
                throw new Error(message);
            }
            const data = await res.json();
            urlInput.value = data.url;
        } catch (err) {
            alert(err.message);
            urlInput.value = "";
            preview.hidden = true;
        }
    });

    document.addEventListener("click", (e) => {
        const btn = e.target.closest("[data-edit-image-remove]");
        if (!btn) return;
        e.preventDefault();
        const scope = btn.closest("[data-post-scope]");
        const urlInput = scope.querySelector("[data-edit-image-url]");
        const preview = scope.querySelector("[data-edit-image-preview]");
        urlInput.value = "";
        preview.hidden = true;
    });

    // Tahrirni saqlash — yangilangan matn/rasmni ko'rinishga qayta chiqaradi
    document.addEventListener("click", async (e) => {
        const btn = e.target.closest("[data-save-edit-post]");
        if (!btn) return;
        e.preventDefault();
        const id = btn.getAttribute("data-save-edit-post");
        const scope = btn.closest("[data-post-scope]");
        const editor = scope.querySelector("[data-rich-editor]");
        const output = scope.querySelector("[data-edit-text]");
        const urlInput = scope.querySelector("[data-edit-image-url]");
        if (editor && editor.richEditor) editor.richEditor.sync();
        const text = (output.value || "").trim();
        if (!text) { alert("Fikr matnini kiriting."); return; }
        if (editor && editor.richEditor) {
            const c = editor.richEditor.check();
            if (c.tooShort) { alert(`Fikr kamida ${editor.richEditor.minLength} belgidan iborat bo'lishi kerak.`); return; }
            if (c.tooLong) { alert(`Fikr ${editor.richEditor.maxLength} belgidan oshmasligi kerak.`); return; }
        }

        btn.disabled = true;
        try {
            const post = await apiPost(`/posts/${id}/update`, {
                postId: parseInt(id, 10),
                reviewText: text,
                imageUrl: urlInput.value || null
            });
            if (!post) return;

            // post.reviewText server tomonda sanitize qilingan (faqat b/i/u/mark) —
            // shuning uchun innerHTML xavfsiz va format ko'rinishda saqlanadi.
            const textEl = scope.querySelector("[data-post-text]");
            if (textEl) textEl.innerHTML = post.reviewText;
            if (output) output.setAttribute("data-original-text", post.reviewText);
            if (editor && editor.richEditor) editor.richEditor.setHtml(post.reviewText);

            // Rasm ko'rsatuvchi joy — post detalida muqovaga qaytish imkoni bilan (data-book-cover-url).
            const imageBox = scope.querySelector("[data-post-image]");
            if (imageBox) {
                const fallbackCover = imageBox.getAttribute("data-book-cover-url") || "";
                const finalUrl = post.imageUrl || fallbackCover;
                const img = imageBox.querySelector("[data-post-image-img]");
                if (img) img.src = finalUrl;
                imageBox.hidden = !finalUrl;
            }
            urlInput.value = post.imageUrl || "";
            urlInput.setAttribute("data-original-url", post.imageUrl || "");

            setEditing(scope, false);
        } catch (err) { alert(err.message); }
        finally { btn.disabled = false; }
    });

    // Postni o'chirish (faqat egasiga ko'rinadi) — feed kartasidan o'chiradi,
    // post detali sahifasida esa o'chirilgandan keyin feedga qaytaradi.
    document.addEventListener("click", async (e) => {
        const btn = e.target.closest("[data-delete-post]");
        if (!btn) return;
        e.preventDefault();
        if (!confirm("Postni o'chirmoqchimisiz?")) return;
        const id = btn.getAttribute("data-delete-post");
        try {
            await apiPost(`/posts/${id}/delete`);
            const card = btn.closest(".post-card");
            if (card) { card.remove(); return; }
            window.location.href = "/Feed";
        } catch (err) { alert(err.message); }
    });

    // Ulashish tugmasi — qo'llab-quvvatlasa qurilmaning ulashish menyusi, aks holda havolani nusxalash
    document.addEventListener("click", async (e) => {
        const btn = e.target.closest("[data-share-url]");
        if (!btn) return;
        e.preventDefault();
        const url = btn.getAttribute("data-share-url");
        if (navigator.share) {
            try { await navigator.share({ url }); } catch { /* foydalanuvchi ulashishni bekor qildi */ }
            return;
        }
        try {
            await navigator.clipboard.writeText(url);
            showToast("Havola nusxalandi!");
        } catch {
            alert(url);
        }
    });

    // Genre tanlash kartochkalari (onboarding)
    // <label> ichidagi checkbox brauzer tomonidan avtomatik almashtiriladi —
    // qo'lda toggle qilmaymiz (aks holda ikki marta almashinib, hech narsa o'zgarmaydi),
    // faqat "change" hodisasida vizual holatni sinxronlaymiz.
    document.querySelectorAll(".genre-card").forEach((card) => {
        const input = card.querySelector("input[type=checkbox]");
        if (!input) return;
        const sync = () => card.classList.toggle("selected", input.checked);
        input.addEventListener("change", sync);
        sync();
    });
})();

// Antiforgery token (forms _Layout'da yashirin maydonda)
function antiforgeryToken() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : "";
}

// Soddalashtirilgan AJAX POST yordamchisi (JSON qaytaradi)
async function apiPost(url, body) {
    const res = await fetch(url, {
        method: "POST",
        headers: {
            "X-Requested-With": "XMLHttpRequest",
            "RequestVerificationToken": antiforgeryToken(),
            ...(body ? { "Content-Type": "application/json" } : {})
        },
        credentials: "include",
        body: body ? JSON.stringify(body) : undefined
    });
    if (res.status === 401) {
        window.location.href = "/";
        return null;
    }
    if (!res.ok) {
        let message = "Xatolik yuz berdi.";
        try { const data = await res.json(); message = data.message || message; } catch { /* ignore */ }
        throw new Error(message);
    }
    // 204 yoki bo'sh body (masalan Ok()/NoContent qaytaruvchi endpointlar) — JSON parslamaymiz.
    if (res.status === 204) return null;
    const text = await res.text();
    return text ? JSON.parse(text) : null;
}

// ===== Toast (vaqtinchalik bildirishnoma oynachasi) =====
function showToast(message, options) {
    options = options || {};
    const host = document.getElementById("toastHost");
    if (!host) return;
    const el = document.createElement("div");
    el.className = "toast";
    if (options.avatarUrl) {
        const img = document.createElement("img");
        img.className = "toast-avatar";
        img.src = options.avatarUrl;
        el.appendChild(img);
    }
    const span = document.createElement("span");
    span.textContent = message;
    el.appendChild(span);
    if (options.url) {
        el.classList.add("clickable");
        el.addEventListener("click", () => { window.location.href = options.url; });
    }
    host.appendChild(el);
    requestAnimationFrame(() => el.classList.add("show"));
    setTimeout(() => {
        el.classList.remove("show");
        setTimeout(() => el.remove(), 300);
    }, 5000);
}

// Boyo'g'li (🦉) kunlik o'qish eslatmasi — diqqatni jalb qiluvchi maxsus toast.
// Har sahifada ishlaydi (boyo'g'li paneliga bog'liq emas), bosilganda /reading-books ga olib boradi.
function showOwlReminder(message, url) {
    const host = document.getElementById("toastHost");
    if (!host) { return; }
    const el = document.createElement("div");
    el.className = "toast owl-reminder clickable";
    const owl = document.createElement("span");
    owl.className = "owl-reminder-emoji";
    owl.textContent = "🦉";
    const span = document.createElement("span");
    span.textContent = message;
    el.appendChild(owl);
    el.appendChild(span);
    el.addEventListener("click", () => { window.location.href = url || "/reading-books"; });
    host.appendChild(el);
    requestAnimationFrame(() => el.classList.add("show"));
    // Eslatma muhim — biroz uzunroq turadi (10s).
    setTimeout(() => {
        el.classList.remove("show");
        setTimeout(() => el.remove(), 300);
    }, 10000);
}

// ===== Real-time bildirishnomalar (SignalR) =====
function initNotifications() {
    if (document.body.dataset.authenticated !== "true" || !window.signalR) return;

    const badge = document.querySelector("[data-notif-badge]");
    let unread = 0;
    function renderBadge() {
        if (!badge) return;
        if (unread > 0) {
            badge.textContent = unread > 9 ? "9+" : String(unread);
            badge.hidden = false;
        } else {
            badge.textContent = "";
            badge.hidden = true;
        }
    }
    function bumpBadge() { unread += 1; renderBadge(); }
    function setBadge(count) { unread = Math.max(0, count | 0); renderBadge(); }

    // ── Bildirishnomalar paneli (qo'ng'iroq dropdown) ──
    // Kim qaysi postga izoh qoldirgani / like bosgani / kuzatgani ro'yxati. Panel ochilganda
    // serverga "o'qildi" deb belgilanadi — keyingi refreshda ro'yxat tozalanadi (joriy ochilgan
    // panelda esa ko'rinib turadi, foydalanuvchi o'qib ulgursin).
    const notifWrap = document.querySelector("[data-notif-wrap]");
    const notifToggle = document.querySelector("[data-notif-toggle]");
    const notifPanel = document.querySelector("[data-notif-panel]");
    const notifList = document.querySelector("[data-notif-list]");
    let items = [];

    function esc(s) {
        const d = document.createElement("div");
        d.textContent = s == null ? "" : String(s);
        return d.innerHTML;
    }
    function timeAgo(iso) {
        const t = new Date(iso).getTime();
        if (!t) return "";
        const sec = Math.max(0, (Date.now() - t) / 1000);
        if (sec < 60) return "hozir";
        const min = Math.floor(sec / 60);
        if (min < 60) return min + " daqiqa oldin";
        const hr = Math.floor(min / 60);
        if (hr < 24) return hr + " soat oldin";
        const day = Math.floor(hr / 24);
        if (day < 7) return day + " kun oldin";
        return new Date(iso).toLocaleDateString("uz");
    }
    function renderList() {
        if (!notifList) return;
        if (!items.length) {
            notifList.innerHTML = `<p class="notif-empty">Yangi bildirishnoma yo'q.</p>`;
            return;
        }
        notifList.innerHTML = items.map((n) => {
            const av = n.actorAvatarUrl
                ? `<img class="notif-avatar" src="${esc(n.actorAvatarUrl)}" alt="">`
                : `<span class="notif-avatar notif-avatar-letter">${esc((n.actorName || "?").trim().charAt(0)).toUpperCase()}</span>`;
            const tag = n.url ? "a" : "div";
            const href = n.url ? ` href="${esc(n.url)}"` : "";
            return `<${tag} class="notif-item"${href}>${av}` +
                `<span class="notif-meta">` +
                `<span class="notif-msg">${esc(n.message)}</span>` +
                `<span class="notif-time">${esc(timeAgo(n.createdAt))}</span>` +
                `</span></${tag}>`;
        }).join("");
    }
    function openNotifPanel() {
        if (!notifPanel) return;
        notifPanel.hidden = false;
        if (notifToggle) notifToggle.setAttribute("aria-expanded", "true");
        if (unread > 0) {
            apiPost("/notifications/read").catch(() => {});
            setBadge(0); // panel ochildi → o'qildi; ro'yxat hozir ko'rinib turadi, refreshdan keyin tozalanadi
        }
    }
    function closeNotifPanel() {
        if (!notifPanel) return;
        notifPanel.hidden = true;
        if (notifToggle) notifToggle.setAttribute("aria-expanded", "false");
    }
    if (notifToggle && notifPanel) {
        notifToggle.addEventListener("click", (e) => {
            e.preventDefault();
            if (notifPanel.hidden) openNotifPanel(); else closeNotifPanel();
        });
        document.addEventListener("click", (e) => {
            if (!notifPanel.hidden && notifWrap && !notifWrap.contains(e.target)) closeNotifPanel();
        });
        document.addEventListener("keydown", (e) => {
            if (e.key === "Escape" && !notifPanel.hidden) closeNotifPanel();
        });
    }

    // ── Navbar "Xabarlar" unread-message badge (separate from the activity bell) ──
    // Bir nechta joyda bo'lishi mumkin: yuqori navbar + mobil pastki navigatsiya.
    const msgBadges = document.querySelectorAll("[data-msg-badge]");
    let unreadMsgs = 0;
    function renderMsgBadge() {
        msgBadges.forEach((b) => {
            if (unreadMsgs > 0) {
                b.textContent = unreadMsgs > 99 ? "99+" : String(unreadMsgs);
                b.hidden = false;
            } else {
                b.textContent = "";
                b.hidden = true;
            }
        });
    }
    function bumpMsgBadge() { unreadMsgs += 1; renderMsgBadge(); }
    function setMsgBadge(count) { unreadMsgs = Math.max(0, count | 0); renderMsgBadge(); }

    // The conversation currently open on /chat (if any) — its incoming messages are read on
    // arrival, so they must NOT bump the badge.
    function activeConversationId() {
        const el = document.getElementById("conversationId");
        return el ? (parseInt(el.value, 10) || 0) : 0;
    }

    // Source of truth: re-fetch the real unread-message count on every page load, so the badge is
    // accurate after a refresh and clears once a conversation is opened (server marks it read).
    fetch("/chat/unread-count", { headers: { "X-Requested-With": "XMLHttpRequest" } })
        .then(r => (r.ok ? r.json() : null))
        .then(d => { if (d) setMsgBadge(d.count); })
        .catch(() => {});

    // Replay: on every page load, pull notifications missed while offline so an invite sent
    // while we were logged out still surfaces (badge count). /chat marks them read server-side,
    // so there the count is already 0.
    fetch("/notifications/unread", { headers: { "X-Requested-With": "XMLHttpRequest" } })
        .then(r => (r.ok ? r.json() : null))
        .then(d => { if (d) { setBadge(d.count); items = d.items || []; renderList(); } })
        .catch(() => {});

    // Cheksiz qayta-ulanish: default siyosat ~4 urinishdan keyin to'xtardi — bu hech qachon to'xtamaydi.
    const foreverRetry = {
        nextRetryDelayInMilliseconds: (ctx) =>
            Math.min(1000 * Math.pow(2, Math.min(ctx.previousRetryCount, 4)), 15000)
    };
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/notifications")
        .withAutomaticReconnect(foreverRetry)
        .build();

    connection.on("ReceiveNotification", (n) => {
        if (!n) return;

        // New chat message → drive the "Xabarlar" badge, NOT the activity bell.
        if (n.type === "message") {
            // If the user is already viewing this conversation, it's read on arrival — don't bump.
            if (n.relatedId && activeConversationId() === n.relatedId) return;
            bumpMsgBadge();
            // /chat shows its own per-message toast; elsewhere, surface one here.
            const onChat = location.pathname.toLowerCase().startsWith("/chat");
            if (!onChat) {
                showToast(n.message || "Yangi xabar", { avatarUrl: n.actorAvatarUrl, url: n.url || "/chat" });
            }
            return;
        }

        // Let pages (e.g. /chat owl) react to specific notification types first.
        document.dispatchEvent(new CustomEvent("kitob:notification", { detail: n }));

        // Kunlik o'qish eslatmasi — boyo'g'li (🦉) yetkazadi: ajralib turadigan toast.
        if (n.type === "reading_reminder") {
            showOwlReminder(n.message || "Bugun hali kitob o'qimadingiz. Bir oz vaqt toping! 🦉📖", n.url || "/reading-books");
            items.unshift(n);
            renderList();
            bumpBadge();
            return;
        }

        // Connection invites are surfaced by the chat owl, so skip the generic toast for them there.
        const handledByPage = (n.type === "connection_request" || n.type === "connection_accepted")
            && document.getElementById("owlPanel");
        if (!handledByPage) {
            showToast(n.message || "Yangi bildirishnoma", { avatarUrl: n.actorAvatarUrl, url: n.url });
        }
        // Qo'ng'iroq panelidagi ro'yxatga ham qo'shamiz (eng yangisi tepada).
        items.unshift(n);
        renderList();
        bumpBadge();
    });

    // To'liq uzilsa (server restart va auto-reconnect tugasa) — qo'lda qayta urinishni davom ettiramiz.
    let restartTimer = null;
    function startNotif() {
        connection.start()
            .then(() => { if (restartTimer) { clearInterval(restartTimer); restartTimer = null; } })
            .catch(() => { if (!restartTimer) restartTimer = setInterval(retryIfDown, 5000); });
    }
    function retryIfDown() {
        if (connection.state === signalR.HubConnectionState.Disconnected) startNotif();
        else { clearInterval(restartTimer); restartTimer = null; }
    }
    connection.onclose(() => { if (!restartTimer) restartTimer = setInterval(retryIfDown, 5000); });
    document.addEventListener("visibilitychange", () => {
        if (!document.hidden && connection.state === signalR.HubConnectionState.Disconnected) startNotif();
    });
    startNotif();
    window.kitob.notifications = connection;
}

/**
 * Cheksiz skroll (infinite scroll): `sentinel` ko'rinish maydoniga yaqinlashganda
 * `endpoint` dan keyingi sahifani (server render qilgan HTML fragment) yuklab,
 * `container` ichiga animatsiya bilan qo'shadi.
 *
 * Performans uchun: bitta IntersectionObserver (skroll hodisasi emas), rootMargin
 * bilan oldindan yuklash, bir vaqtda faqat bitta so'rov, oxirgi sahifada observer uziladi.
 *
 * opts: { sentinel, container, insertBefore?, loader?, endpoint, page, totalPages,
 *         search?, params?, onAppend? }
 */
function infiniteScroll(opts) {
    const sentinel = opts.sentinel;
    const container = opts.container;
    if (!sentinel || !container) return;

    const insertBefore = opts.insertBefore || null;
    const loader = opts.loader || null;
    const params = opts.params || {};
    const totalPages = opts.totalPages || 1;
    const search = opts.search || "";
    const onAppend = opts.onAppend;
    let page = opts.page || 1;
    if (page >= totalPages) return; // yuklash uchun boshqa sahifa yo'q

    let loading = false;
    let done = false;

    const observer = new IntersectionObserver(async (entries) => {
        if (done || loading || !entries[0].isIntersecting) return;
        loading = true;
        if (loader) loader.hidden = false;
        const next = page + 1;
        try {
            const url = new URL(opts.endpoint, window.location.origin);
            url.searchParams.set("page", next);
            if (search) url.searchParams.set("q", search);
            Object.keys(params).forEach((k) => {
                if (params[k] != null && params[k] !== "") url.searchParams.set(k, params[k]);
            });
            const res = await fetch(url.toString(), { 
                headers: { "X-Requested-With": "XMLHttpRequest" },
                credentials: "include"
            });
            if (res.status === 401) { window.location.href = "/"; return; }
            if (!res.ok) throw new Error("network");
            const html = (await res.text()).trim();
            const tmp = document.createElement("div");
            tmp.innerHTML = html;
            const nodes = Array.from(tmp.children);
            nodes.forEach((node, i) => {
                node.classList.add("card-enter");
                node.style.animationDelay = (i * 70) + "ms";
                container.insertBefore(node, insertBefore);
            });
            page = next;
            if (typeof onAppend === "function") onAppend(nodes);
            if (page >= totalPages) { done = true; observer.disconnect(); }
        } catch {
            // Tarmoq xatosi — observer saqlanadi, keyingi skrollda yana urinadi.
        } finally {
            loading = false;
            if (loader) loader.hidden = true;
        }
    }, { rootMargin: "600px 0px" });

    observer.observe(sentinel);
}

// ===== Asoschi (founder) nishoni — mijoz tomonda render qilinadigan ismlar uchun =====
// Server tomoni ViewHelpers.FounderUsername bilan AYNI qiymat bo'lishi shart.
const FOUNDER_USERNAME = "javohirsadullayev";
function isFounder(username) {
    return !!username && String(username).trim().toLowerCase() === FOUNDER_USERNAME;
}
function founderBadge(username) {
    return isFounder(username)
        ? '<span class="badge founder-badge" title="kitobdagimen.uz asoschisi"><span class="material-symbols-outlined">verified</span>Asoschi</span>'
        : "";
}

// ===== asaxiy.uz kitob qidiruvi (kitob tanlash oynalarida ishlatiladi) =====
// Lokal katalogda kitob kam bo'lgani uchun, qidiruvga mos asaxiy.uz kitoblarini
// ham ko'rsatamiz. Foydalanuvchi birini bossa — u lokal katalogga import qilinib
// (muqovasi bilan), tanlangan kitob sifatida qaytariladi (pickBook orqali).
async function renderAsaxiyBooks(query, suggestions, pickBook) {
    let items;
    try {
        const res = await fetch(`/books/asaxiy-search?q=${encodeURIComponent(query)}`,
            { 
                headers: { "X-Requested-With": "XMLHttpRequest" },
                credentials: "include"
            });
        if (!res.ok) return;
        items = await res.json();
    } catch { return; }

    // Foydalanuvchi shu orada boshqa narsa yozgan bo'lsa — eski natijani chiqarmaymiz.
    if (suggestions.dataset.q !== query || !Array.isArray(items) || !items.length) return;

    const head = document.createElement("div");
    head.className = "book-suggest__src";
    head.textContent = "asaxiy.uz dan:";
    suggestions.appendChild(head);

    items.forEach(it => {
        const div = document.createElement("div");
        div.className = "book-suggest__asaxiy";
        if (it.coverUrl) {
            const img = document.createElement("img");
            img.src = it.coverUrl;
            img.alt = "";
            img.loading = "lazy";
            div.appendChild(img);
        }
        const span = document.createElement("span");
        span.textContent = `${it.title} — ${it.author}`;
        div.appendChild(span);
        div.addEventListener("click", async () => {
            if (div.dataset.loading) return;
            div.dataset.loading = "1";
            const original = span.textContent;
            span.textContent = "Yuklanmoqda…";
            try {
                const book = await window.kitob.apiPost("/books/import-asaxiy", { url: it.url });
                if (book) pickBook(book);
            } catch (e) {
                span.textContent = original;
                delete div.dataset.loading;
                alert(e.message || "Kitobni import qilib bo'lmadi.");
            }
        });
        suggestions.appendChild(div);
    });
    suggestions.hidden = false;
}

window.kitob = { apiPost, antiforgeryToken, showToast, infiniteScroll, FOUNDER_USERNAME, isFounder, founderBadge, renderAsaxiyBooks };

initNotifications();

// ===== Kichik matn muharriri (rich text): qalin / kursiv / tagchiziq / marker =====
// Server tomonda RichTextSanitizer FAQAT <b><i><u><mark> teglarini qoldiradi, shu
// sabab muharrir ham chiqishni shu to'plamga normallashtiradi (XSS bo'lmasligi uchun
// matn tugunlari encode qilinadi). Newline'lar `\n` sifatida saqlanadi.
(function () {
    "use strict";

    const ALLOWED = { b: "b", strong: "b", i: "i", em: "i", u: "u", mark: "mark" };

    function escapeText(s) {
        return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    }

    function isHighlightSpan(el) {
        if (el.tagName !== "SPAN") return false;
        const bg = el.style && el.style.backgroundColor;
        return !!bg && bg !== "transparent" && bg !== "rgba(0, 0, 0, 0)";
    }

    // contenteditable DOM'ni xavfsiz, kichik HTML satriga aylantiradi.
    function serialize(node) {
        let out = "";
        node.childNodes.forEach((child) => {
            if (child.nodeType === 3) {
                out += escapeText(child.nodeValue);
                return;
            }
            if (child.nodeType !== 1) return;
            const tag = child.tagName.toLowerCase();
            if (tag === "br") { out += "\n"; return; }
            const inner = serialize(child);
            let mapped = ALLOWED[tag];
            if (!mapped && isHighlightSpan(child)) mapped = "mark";
            if (mapped) {
                out += inner.trim() ? `<${mapped}>${inner}</${mapped}>` : inner;
            } else if (tag === "div" || tag === "p") {
                if (out && !out.endsWith("\n")) out += "\n";
                out += inner;
            } else {
                out += inner; // noma'lum teglarni tekislaymiz
            }
        });
        return out;
    }

    function initRichEditor(root) {
        if (root.dataset.richReady === "1") return;
        const content = root.querySelector("[data-rich-content]");
        const output = root.querySelector("[data-rich-output]");
        const toolbar = root.querySelector(".rich-toolbar");
        if (!content || !output) return;
        root.dataset.richReady = "1";

        const minLen = parseInt(root.dataset.richMin || "0", 10) || 0;
        const maxLen = parseInt(root.dataset.richMax || "0", 10) || 0;
        const counterBox = root.querySelector("[data-rich-counter]");
        const counterNum = root.querySelector("[data-rich-count]");
        let lastGoodHtml = content.innerHTML; // maxLen oshganda qaytariladigan oxirgi to'g'ri holat

        function placeCaretEnd(el) {
            const sel = window.getSelection();
            if (!sel) return;
            const range = document.createRange();
            range.selectNodeContents(el);
            range.collapse(false);
            sel.removeAllRanges();
            sel.addRange(range);
        }

        function updateCounter(len) {
            if (counterNum) counterNum.textContent = len;
            if (counterBox) {
                const invalid = (maxLen && len > maxLen) || (minLen && len > 0 && len < minLen);
                counterBox.classList.toggle("rich-counter-invalid", !!invalid);
            }
        }

        function updatePlaceholder() {
            const empty = content.textContent.replace(/​/g, "").trim().length === 0;
            content.classList.toggle("is-empty", empty);
        }

        function sync() {
            let value = serialize(content).replace(/\n{3,}/g, "\n\n").trim();
            if (maxLen && value.length > maxLen) {
                // Limitdan oshdi — oxirgi qabul qilingan holatga qaytaramiz (haqiqiy maxlength xulqi).
                content.innerHTML = lastGoodHtml;
                placeCaretEnd(content);
                value = serialize(content).replace(/\n{3,}/g, "\n\n").trim();
            } else {
                lastGoodHtml = content.innerHTML;
            }
            output.value = value;
            updateCounter(value.length);
            updatePlaceholder();
            root.dispatchEvent(new CustomEvent("rich:input", { bubbles: true }));
        }

        function setHtml(html) {
            content.innerHTML = html || "";
            sync();
        }

        function exec(cmd) {
            content.focus();
            try { document.execCommand("styleWithCSS", false, false); } catch (_) { /* ok */ }
            if (cmd === "bold" || cmd === "italic" || cmd === "underline") {
                document.execCommand(cmd, false, null);
            } else if (cmd === "highlight") {
                toggleHighlight();
            } else if (cmd === "clear") {
                document.execCommand("removeFormat", false, null);
                unwrapMarks();
            }
            sync();
        }

        function toggleHighlight() {
            const sel = window.getSelection();
            if (!sel || sel.rangeCount === 0 || sel.isCollapsed) return;
            const range = sel.getRangeAt(0);
            let anc = range.commonAncestorContainer;
            if (anc.nodeType === 3) anc = anc.parentNode;
            const existing = anc.closest ? anc.closest("mark") : null;
            if (existing && content.contains(existing)) {
                const parent = existing.parentNode;
                while (existing.firstChild) parent.insertBefore(existing.firstChild, existing);
                parent.removeChild(existing);
                return;
            }
            const mark = document.createElement("mark");
            try {
                range.surroundContents(mark);
            } catch (_) {
                mark.appendChild(range.extractContents());
                range.insertNode(mark);
            }
            sel.removeAllRanges();
            const r = document.createRange();
            r.selectNodeContents(mark);
            sel.addRange(r);
        }

        // Tanlov ichidagi yoki tegib turgan barcha <mark>larni ochib tashlaydi.
        function unwrapMarks() {
            const sel = window.getSelection();
            content.querySelectorAll("mark").forEach((m) => {
                if (sel && sel.rangeCount && !sel.getRangeAt(0).intersectsNode(m)) return;
                const parent = m.parentNode;
                while (m.firstChild) parent.insertBefore(m.firstChild, m);
                parent.removeChild(m);
            });
        }

        if (toolbar) {
            toolbar.addEventListener("mousedown", (e) => e.preventDefault()); // tanlovni yo'qotmaslik
            toolbar.addEventListener("click", (e) => {
                const btn = e.target.closest("[data-cmd]");
                if (!btn) return;
                e.preventDefault();
                exec(btn.getAttribute("data-cmd"));
                syncToolbarState();
            });
        }

        function syncToolbarState() {
            if (!toolbar) return;
            ["bold", "italic", "underline"].forEach((cmd) => {
                const btn = toolbar.querySelector(`[data-cmd="${cmd}"]`);
                if (!btn) return;
                let active = false;
                try { active = document.queryCommandState(cmd); } catch (_) { /* ok */ }
                btn.classList.toggle("active", active);
            });
        }

        content.addEventListener("input", sync);
        content.addEventListener("blur", sync);
        content.addEventListener("keyup", syncToolbarState);
        content.addEventListener("mouseup", syncToolbarState);

        root.richEditor = {
            setHtml,
            sync,
            focus() { content.focus(); },
            getValue() { return output.value; },
            minLength: minLen,
            maxLength: maxLen,
            // Limit holatini tekshiradi: { ok, len, tooShort, tooLong }
            check() {
                const len = (output.value || "").length;
                return {
                    ok: (!minLen || len >= minLen) && (!maxLen || len <= maxLen),
                    len,
                    tooShort: !!minLen && len < minLen,
                    tooLong: !!maxLen && len > maxLen
                };
            }
        };

        sync(); // dastlabki holatni o'rnatadi (oldindan to'ldirilgan tahrir paneli uchun)
    }

    function initAll(scope) {
        (scope || document).querySelectorAll("[data-rich-editor]").forEach(initRichEditor);
    }

    window.kitob.initRichEditors = initAll;
    initAll();
})();

// ===== Story'lar: ko'rish oynasi + qo'shish kompozeri =====
(function () {
    "use strict";

    function timeAgo(iso) {
        const d = new Date(iso);
        const diff = Math.floor((Date.now() - d.getTime()) / 1000);
        if (diff < 60) return "hozir";
        if (diff < 3600) return Math.floor(diff / 60) + " daqiqa oldin";
        if (diff < 86400) return Math.floor(diff / 3600) + " soat oldin";
        return Math.floor(diff / 86400) + " kun oldin";
    }

    // ---------- Ko'rish oynasi ----------
    const viewer = document.getElementById("storyViewer");
    if (viewer) {
        const imageWrap = viewer.querySelector(".story-image-wrap");
        const imgEl = document.getElementById("storyImage");
        const captionEl = document.getElementById("storyCaption");
        const authorEl = document.getElementById("storyAuthor");
        const progressEl = document.getElementById("storyProgress");
        const viewCountEl = document.getElementById("storyViewCount");
        const likeBtn = document.getElementById("storyLikeBtn");
        const likeCountEl = document.getElementById("storyLikeCount");
        const deleteBtn = document.getElementById("storyDeleteBtn");
        const prevBtn = viewer.querySelector("[data-story-prev]");
        const nextBtn = viewer.querySelector("[data-story-next]");

        let stories = [];
        let idx = 0;
        let changed = false; // story qo'shildi/o'chdi — yopilganda sahifani yangilash uchun

        function render() {
            const s = stories[idx];
            if (!s) return;

            const src = s.imageUrl || "";
            imgEl.src = src;
            imgEl.style.display = src ? "" : "none";
            imageWrap.classList.toggle("noimg", !src);

            captionEl.innerHTML = "";
            const title = document.createElement("div");
            title.className = "story-book-title";
            title.textContent = s.title;
            const text = document.createElement("div");
            text.className = "story-book-author";
            text.textContent = s.text;
            captionEl.appendChild(title);
            captionEl.appendChild(text);

            authorEl.innerHTML = "";
            const a = document.createElement("a");
            a.href = "/profile/" + s.author.id;
            a.className = "story-author-inner";
            const av = document.createElement("span");
            av.className = "avatar sm";
            if (s.author.avatarUrl) {
                const im = document.createElement("img");
                im.src = s.author.avatarUrl;
                im.className = "avatar sm";
                im.referrerPolicy = "no-referrer"; // Google avatar'lari uchun kerak
                a.appendChild(im);
            } else {
                av.textContent = (s.author.fullName || "?").trim().charAt(0).toUpperCase();
                a.appendChild(av);
            }
            const nm = document.createElement("div");
            nm.className = "story-author-meta";
            nm.innerHTML = "<span class=\"name\"></span><span class=\"time\"></span>";
            nm.querySelector(".name").textContent = s.author.fullName;
            nm.querySelector(".time").textContent = timeAgo(s.createdAt);
            a.appendChild(nm);
            authorEl.appendChild(a);

            viewCountEl.textContent = s.viewCount;
            likeCountEl.textContent = s.likeCount;
            likeBtn.classList.toggle("liked", !!s.isLikedByCurrentUser);
            deleteBtn.hidden = !s.isMine;

            progressEl.innerHTML = "";
            stories.forEach((_, i) => {
                const seg = document.createElement("span");
                seg.className = "story-seg" + (i === idx ? " active" : "") + (i < idx ? " seen" : "");
                progressEl.appendChild(seg);
            });

            prevBtn.style.visibility = idx > 0 ? "visible" : "hidden";
            nextBtn.style.visibility = idx < stories.length - 1 ? "visible" : "hidden";

            recordView(s);
        }

        async function recordView(s) {
            try {
                const r = await apiPost(`/stories/${s.id}/view`);
                if (r && typeof r.viewCount === "number") {
                    s.viewCount = r.viewCount;
                    if (stories[idx] === s) viewCountEl.textContent = r.viewCount;
                }
            } catch { /* ko'rishni yozib bo'lmadi — jim o'tkazamiz */ }
        }

        function open(list, startIndex) {
            stories = list;
            idx = startIndex > 0 && startIndex < list.length ? startIndex : 0;
            changed = false;
            viewer.hidden = false;
            document.body.classList.add("no-scroll");
            render();
        }

        // Boshqa sahifalar (masalan /profile "Storylar" tabi) viewer'ni to'g'ridan-to'g'ri
        // o'z ro'yxati bilan, kerakli story'dan boshlab ocha olishi uchun.
        window.kitobStory = { open };
        function close() {
            viewer.hidden = true;
            document.body.classList.remove("no-scroll");
            if (changed) window.location.reload();
        }

        function go(delta) {
            const n = idx + delta;
            if (n < 0) return;
            if (n >= stories.length) { close(); return; }
            idx = n;
            render();
        }

        // Avatar (feed/profil) bosilganda story'larni yuklab ochamiz
        document.addEventListener("click", async (e) => {
            const trigger = e.target.closest("[data-open-story]");
            if (!trigger) return;
            e.preventDefault();
            const userId = trigger.getAttribute("data-open-story");
            try {
                const res = await fetch(`/stories/user/${userId}`, { 
                    headers: { "X-Requested-With": "XMLHttpRequest" },
                    credentials: "include"
                });
                if (!res.ok) return;
                const list = await res.json();
                if (!list || list.length === 0) { window.location.href = "/profile/" + userId; return; }
                open(list);
            } catch { /* tarmoq xatosi */ }
        });

        viewer.querySelector("[data-story-close]").addEventListener("click", close);
        prevBtn.addEventListener("click", () => go(-1));
        nextBtn.addEventListener("click", () => go(1));
        viewer.addEventListener("click", (e) => { if (e.target === viewer) close(); });
        document.addEventListener("keydown", (e) => {
            if (viewer.hidden) return;
            if (e.key === "Escape") close();
            else if (e.key === "ArrowLeft") go(-1);
            else if (e.key === "ArrowRight") go(1);
        });

        likeBtn.addEventListener("click", async () => {
            const s = stories[idx];
            if (!s) return;
            try {
                const r = await apiPost(`/stories/${s.id}/like`);
                if (!r) return;
                s.isLikedByCurrentUser = r.isLiked;
                s.likeCount = r.likeCount;
                likeBtn.classList.toggle("liked", r.isLiked);
                likeCountEl.textContent = r.likeCount;
            } catch (err) { alert(err.message); }
        });

        deleteBtn.addEventListener("click", async () => {
            const s = stories[idx];
            if (!s) return;
            if (!confirm("Story'ni o'chirmoqchimisiz?")) return;
            try {
                await apiPost(`/stories/${s.id}/delete`);
                changed = true;
                stories.splice(idx, 1);
                if (stories.length === 0) { close(); return; }
                if (idx >= stories.length) idx = stories.length - 1;
                render();
            } catch (err) { alert(err.message); }
        });
    }

    // ---------- Qo'shish kompozeri ----------
    const composer = document.getElementById("storyComposer");
    if (composer) {
        const titleInput = document.getElementById("storyTitleInput");
        const textInput = document.getElementById("storyTextInput");
        const titleCount = document.getElementById("storyTitleCount");
        const textCount = document.getElementById("storyTextCount");
        const imageInput = document.getElementById("storyImageInput");
        const imageUrl = document.getElementById("storyImageUrl");
        const imgPreview = document.getElementById("storyImgPreview");
        const imgPreviewImg = document.getElementById("storyImgPreviewImg");
        const imgClear = document.getElementById("storyImgClear");
        const submitBtn = document.getElementById("storyComposerSubmit");
        const errorEl = document.getElementById("storyComposerError");
        const durationGroup = document.getElementById("storyDuration");
        const durationOpts = durationGroup ? Array.from(durationGroup.querySelectorAll(".story-duration-opt")) : [];
        let uploading = false;
        let durationHours = 24;

        function setDuration(hours) {
            durationHours = hours;
            durationOpts.forEach((b) => b.classList.toggle("is-active", Number(b.dataset.duration) === hours));
        }
        durationOpts.forEach((b) => b.addEventListener("click", () => setDuration(Number(b.dataset.duration))));

        function refresh() {
            const title = titleInput.value.trim();
            const text = textInput.value.trim();
            titleCount.textContent = titleInput.value.length;
            textCount.textContent = textInput.value.length;
            const valid = title.length >= 3 && title.length <= 50 && text.length >= 3 && text.length <= 140;
            submitBtn.disabled = !valid || uploading;
        }
        function openComposer() {
            composer.hidden = false;
            document.body.classList.add("no-scroll");
            titleInput.value = "";
            textInput.value = "";
            resetImage();
            setDuration(24);
            errorEl.hidden = true;
            refresh();
        }
        function closeComposer() {
            composer.hidden = true;
            document.body.classList.remove("no-scroll");
        }
        function resetImage() {
            imageInput.value = "";
            imageUrl.value = "";
            imgPreviewImg.src = "";
            imgPreviewImg.style.cssText = ""; // Brauzer extension'lar qo'shgan inline style'larni tozalash
            imgPreview.hidden = true;
            uploading = false;
        }

        document.querySelector("[data-open-story-composer]")?.addEventListener("click", openComposer);
        composer.querySelector("[data-story-composer-cancel]").addEventListener("click", closeComposer);
        composer.addEventListener("click", (e) => { if (e.target === composer) closeComposer(); });
        titleInput.addEventListener("input", refresh);
        textInput.addEventListener("input", refresh);

        imageInput.addEventListener("change", async () => {
            const f = imageInput.files && imageInput.files[0];
            if (!f) return;
            
            console.log("Story image upload started:", f.name, f.type, f.size);
            
            // Preview'ni darhol ko'rsatamiz (rasm yuklanguncha loading holatida)
            imgPreview.hidden = false;
            imgPreviewImg.src = "";
            imgPreviewImg.style.backgroundColor = "#f0f0f0";
            imgPreviewImg.style.visibility = "visible"; // Brauzer extension bloklarini bekor qilish
            
            // Base64 preview
            const reader = new FileReader();
            reader.onload = (ev) => { 
                imgPreviewImg.src = ev.target.result;
                imgPreviewImg.style.backgroundColor = "";
            };
            reader.readAsDataURL(f);
            
            imageUrl.value = "";
            uploading = true;
            refresh();
            try {
                const fd = new FormData();
                fd.append("file", f);
                
                const token = antiforgeryToken();
                console.log("Antiforgery token:", token ? "present" : "MISSING");
                
                const res = await fetch("/stories/upload-image", {
                    method: "POST",
                    headers: { 
                        "X-Requested-With": "XMLHttpRequest", 
                        "RequestVerificationToken": token 
                    },
                    credentials: "include",
                    body: fd
                });
                
                console.log("Upload response:", res.status, res.statusText);
                
                if (res.status === 401) { window.location.href = "/"; return; }
                if (!res.ok) {
                    let message = "Rasmni yuklab bo'lmadi.";
                    try { 
                        const d = await res.json(); 
                        console.error("Upload error response:", d);
                        message = d.message || message; 
                    } catch (e) { 
                        console.error("Failed to parse error:", e);
                    }
                    throw new Error(message);
                }
                const data = await res.json();
                console.log("Upload success:", data);
                imageUrl.value = data.url;
                // Server rasmini preview'da ko'rsatamiz
                imgPreviewImg.src = data.url;
            } catch (err) {
                console.error("Story image upload failed:", err);
                alert(err.message);
                resetImage();
            } finally {
                uploading = false;
                refresh();
            }
        });
        imgClear.addEventListener("click", resetImage);

        submitBtn.addEventListener("click", async () => {
            const title = titleInput.value.trim();
            const text = textInput.value.trim();
            if (title.length < 3 || text.length < 3) return;
            submitBtn.disabled = true;
            errorEl.hidden = true;
            try {
                const story = await apiPost("/stories/create", {
                    title: title,
                    text: text,
                    imageUrl: imageUrl.value || null,
                    durationHours: durationHours
                });
                if (story) {
                    closeComposer();
                    showToast("Story joylandi!");
                    setTimeout(() => window.location.reload(), 600);
                }
            } catch (err) {
                errorEl.textContent = err.message;
                errorEl.hidden = false;
                submitBtn.disabled = false;
            }
        });
    }
})();

// Brauzer extension'lar rasmlarni visibility:hidden qilishini oldini olish
(function() {
    const fixImageVisibility = () => {
        document.querySelectorAll('img[data-post-image-img], img[data-quote-book-cover], .post-image, .quote-book-cover, .pd-hero img').forEach(img => {
            if (img.style.visibility === 'hidden') {
                img.style.visibility = 'visible';
            }
        });
    };
    
    // Dastlab tekshirish
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', fixImageVisibility);
    } else {
        fixImageVisibility();
    }
    
    // MutationObserver - yangi rasmlar yoki style o'zgarishlari uchun
    const observer = new MutationObserver(fixImageVisibility);
    observer.observe(document.body, { 
        childList: true, 
        subtree: true, 
        attributes: true, 
        attributeFilter: ['style'] 
    });
})();
