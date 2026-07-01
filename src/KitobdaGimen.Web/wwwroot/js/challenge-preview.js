// Super admin "oldindan sinab ko'rish" (ephemeral preview).
// Joriy davr yetakchilari (top 3) g'olib sifatida ko'rsatiladi; super admin ularga sovg'a
// kitob tayinlab, bayram ko'rinishi va g'olib modalini sinab ko'radi. HECH NARSA SAQLANMAYDI —
// "Orqaga qaytish" tugmasi hamma o'zgarishlarni bekor qiladi (faqat brauzerda, DBga tegmaydi).
(function () {
    "use strict";

    var overlay = document.querySelector("[data-sa-preview-overlay]");
    if (!overlay) { return; }

    var liveEl = document.querySelector("[data-sa-live]");
    var live = [];
    try { live = JSON.parse(liveEl ? liveEl.textContent : "[]") || []; } catch (e) { live = []; }

    // Top 3 (o'rin bo'yicha).
    var top3 = live
        .filter(function (w) { return w.rank >= 1 && w.rank <= 3; })
        .sort(function (a, b) { return a.rank - b.rank; });

    // rank -> { title, author, coverUrl }
    var gifts = {};

    function esc(s) {
        return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
        });
    }
    function initial(name) { return name && name.trim() ? name.trim()[0].toUpperCase() : "?"; }
    function medal(rank) {
        var cls = rank === 1 ? "ch-medal-gold" : rank === 2 ? "ch-medal-silver" : "ch-medal-bronze";
        return '<span class="material-symbols-outlined ch-medal-ic ' + cls + '">workspace_premium</span>';
    }
    function avatarHtml(w, extraCls) {
        return w.avatarUrl
            ? '<img class="avatar ' + extraCls + '" src="' + esc(w.avatarUrl) + '" alt="" referrerpolicy="no-referrer" />'
            : '<span class="avatar ' + extraCls + '">' + esc(initial(w.fullName)) + "</span>";
    }
    function giftPreviewHtml(rank) {
        var g = gifts[rank];
        if (!g || !g.title) { return ""; }
        var cover = g.coverUrl
            ? '<img src="' + esc(g.coverUrl) + '" alt="" referrerpolicy="no-referrer" />'
            : '<span class="ch-gift-noimg"><span class="material-symbols-outlined">menu_book</span></span>';
        return '<div class="ch-gift">' + cover +
            '<div><span class="ch-gift-label"><span class="material-symbols-outlined">redeem</span> Sovg\'a kitob</span>' +
            '<strong>' + esc(g.title) + "</strong>" +
            (g.author ? '<span class="muted">' + esc(g.author) + "</span>" : "") +
            "</div></div>";
    }

    var winnersHost = overlay.querySelector("[data-sa-winners]");
    var formsHost = overlay.querySelector("[data-sa-gift-forms]");
    var summaryHost = overlay.querySelector("[data-sa-summary]");
    var emptyBox = overlay.querySelector("[data-sa-empty]");
    var body = overlay.querySelector("[data-sa-body]");

    function renderWinners() {
        if (!winnersHost) { return; }
        var order = [2, 1, 3];
        var html = "";
        order.forEach(function (slot) {
            var w = top3.find(function (x) { return x.rank === slot; });
            if (!w) { return; }
            html +=
                '<div class="ch-sa-winner ch-place-' + w.rank + '" data-sa-winner-card="' + w.rank + '">' +
                    '<div class="ch-medal">' + medal(w.rank) + "</div>" +
                    avatarHtml(w, "ch-avatar") +
                    '<div class="ch-name">' + esc(w.fullName) + "</div>" +
                    '<div class="ch-detail muted">' + w.rank + "-o'rin</div>" +
                    '<div class="ch-sa-winner-stats muted">' +
                        w.pagesRead + " bet · " + w.booksRead + " kitob · kuniga " + w.avgPagesPerDay + " bet" +
                    "</div>" +
                    '<div class="ch-sa-winner-gift" data-sa-winner-gift="' + w.rank + '">' + giftPreviewHtml(w.rank) + "</div>" +
                "</div>";
        });
        winnersHost.innerHTML = html;
    }

    function renderGiftForms() {
        if (!formsHost) { return; }
        var html = "";
        top3.forEach(function (w) {
            var g = gifts[w.rank] || {};
            html +=
                '<div class="ch-sa-gift-form card" data-sa-form="' + w.rank + '">' +
                    '<div class="ch-sa-gift-who">' + medal(w.rank) + " <strong>" + esc(w.fullName) + "</strong> <span class=\"muted\">(" + w.rank + "-o'rin)</span></div>" +
                    '<div class="ch-form-row">' +
                        '<label>Kitob nomi<input type="text" data-sa-input="title" data-sa-rank="' + w.rank + '" value="' + esc(g.title || "") + '" placeholder="Masalan: Atom odatlari" /></label>' +
                        '<label>Muallif<input type="text" data-sa-input="author" data-sa-rank="' + w.rank + '" value="' + esc(g.author || "") + '" placeholder="Masalan: Jeyms Klir" /></label>' +
                    "</div>" +
                    '<div class="ch-form-row">' +
                        '<label>Muqova URL (ixtiyoriy)<input type="text" data-sa-input="coverUrl" data-sa-rank="' + w.rank + '" value="' + esc(g.coverUrl || "") + '" placeholder="https://..." /></label>' +
                    "</div>" +
                "</div>";
        });
        formsHost.innerHTML = html;

        formsHost.querySelectorAll("[data-sa-input]").forEach(function (inp) {
            inp.addEventListener("input", function () {
                var rank = parseInt(inp.getAttribute("data-sa-rank"), 10);
                var field = inp.getAttribute("data-sa-input");
                gifts[rank] = gifts[rank] || {};
                gifts[rank][field] = inp.value.trim();
                var giftCell = overlay.querySelector('[data-sa-winner-gift="' + rank + '"]');
                if (giftCell) { giftCell.innerHTML = giftPreviewHtml(rank); }
                renderSummary();
            });
        });
    }

    function renderSummary() {
        if (!summaryHost) { return; }
        var rows = top3.map(function (w) {
            var g = gifts[w.rank];
            if (g && g.title) {
                return '<li><span class="material-symbols-outlined">redeem</span> <strong>' + esc(w.fullName) +
                    '</strong> (' + w.rank + "-o'rin) ← «" + esc(g.title) + "»" +
                    (g.author ? " <span class=\"muted\">— " + esc(g.author) + "</span>" : "") + "</li>";
            }
            return '<li class="muted"><span class="material-symbols-outlined">radio_button_unchecked</span> <strong>' +
                esc(w.fullName) + "</strong> (" + w.rank + "-o'rin) — kitob tayinlanmagan</li>";
        }).join("");
        summaryHost.innerHTML =
            '<h4 class="ch-sa-summary-title">Kimga qaysi kitob beriladi</h4><ul class="ch-sa-summary-list">' + rows + "</ul>";
    }

    // ── Bayram ko'rinishi (sparkles) ─────────────────────────────────────────────
    var festiveOn = false;
    function toggleFestive() {
        festiveOn = !festiveOn;
        var page = document.querySelector(".ch-page");
        if (page) { page.classList.toggle("is-festive", festiveOn); }
        var host = document.querySelector("[data-ch-festive]");
        if (festiveOn && !host) {
            host = document.createElement("div");
            host.className = "ch-festive";
            host.setAttribute("data-ch-festive", "");
            document.body.appendChild(host);
            var icons = ["celebration", "auto_awesome", "star", "emoji_events", "workspace_premium", "menu_book", "redeem"];
            for (var i = 0; i < 28; i++) {
                var s = document.createElement("span");
                s.className = "ch-spark material-symbols-outlined c-gold";
                s.textContent = icons[i % icons.length];
                s.style.left = Math.random() * 100 + "vw";
                s.style.animationDuration = (5 + Math.random() * 7) + "s";
                s.style.animationDelay = (Math.random() * 6) + "s";
                s.style.fontSize = (18 + Math.random() * 20) + "px";
                host.appendChild(s);
            }
        } else if (!festiveOn && host) {
            host.remove();
        }
    }
    function clearFestive() {
        festiveOn = false;
        var page = document.querySelector(".ch-page");
        if (page) { page.classList.remove("is-festive"); }
        var host = document.querySelector("[data-ch-festive]");
        if (host) { host.remove(); }
    }

    // ── G'olib modali (preview) ──────────────────────────────────────────────────
    var modal = null;
    function buildModal() {
        var order = [2, 1, 3];
        var winners = "";
        order.forEach(function (slot) {
            var w = top3.find(function (x) { return x.rank === slot; });
            if (!w) { return; }
            var g = gifts[w.rank];
            var giftHtml = "";
            if (g && g.title) {
                var cover = g.coverUrl
                    ? '<img src="' + esc(g.coverUrl) + '" alt="" referrerpolicy="no-referrer" />'
                    : '<span class="ch-modal-gift-noimg"><span class="material-symbols-outlined">menu_book</span></span>';
                giftHtml = '<div class="ch-modal-gift">' + cover +
                    '<div class="ch-modal-gift-info"><span class="ch-modal-gift-label"><span class="material-symbols-outlined">redeem</span> Sovg\'a kitob</span>' +
                    '<span class="ch-modal-gift-title">' + esc(g.title) + "</span>" +
                    (g.author ? '<span class="ch-modal-gift-author">' + esc(g.author) + "</span>" : "") +
                    "</div></div>";
            }
            winners +=
                '<div class="ch-modal-winner ch-place-' + w.rank + '">' +
                    '<div class="ch-medal">' + medal(w.rank) + "</div>" +
                    avatarHtml(w, "ch-avatar") +
                    '<div class="ch-modal-name">' + esc(w.fullName) + "</div>" +
                    '<div class="ch-modal-stats muted">' + w.pagesRead + " bet · " + w.booksRead + " kitob · kuniga " + w.avgPagesPerDay + " bet</div>" +
                    giftHtml +
                "</div>";
        });

        if (!modal) {
            modal = document.createElement("div");
            modal.className = "ch-modal";
            document.body.appendChild(modal);
        }
        modal.innerHTML =
            '<div class="ch-modal-backdrop" data-sa-modal-close></div>' +
            '<div class="ch-modal-box" role="dialog" aria-modal="true">' +
                '<div class="ch-modal-header"><div class="ch-modal-confetti" aria-hidden="true"></div>' +
                    '<button type="button" class="ch-modal-x" data-sa-modal-close aria-label="Yopish"><span class="material-symbols-outlined">close</span></button>' +
                    '<span class="material-symbols-outlined ch-modal-crown">workspace_premium</span>' +
                    '<h2 class="ch-modal-title">G\'oliblar (sinov)</h2>' +
                    '<p class="ch-modal-sub">Foydalanuvchilar shu ko\'rinishni ko\'radi.</p></div>' +
                '<div class="ch-modal-winners">' + winners + "</div>" +
                '<button type="button" class="btn btn-accent" data-sa-modal-close>Yopish</button>' +
            "</div>";
        modal.querySelectorAll("[data-sa-modal-close]").forEach(function (b) {
            b.addEventListener("click", closeModal);
        });
        modal.hidden = false;
    }
    function closeModal() { if (modal) { modal.hidden = true; } }

    // ── Ochish / yopish (revert) ─────────────────────────────────────────────────
    function open() {
        if (!top3.length) {
            if (body) { body.hidden = true; }
            if (emptyBox) { emptyBox.hidden = false; }
        } else {
            if (emptyBox) { emptyBox.hidden = true; }
            if (body) { body.hidden = false; }
            renderWinners();
            renderGiftForms();
            renderSummary();
        }
        overlay.hidden = false;
        document.body.classList.add("ch-sa-open");
        document.body.style.overflow = "hidden";
    }

    // "Orqaga qaytish" — hamma sinov o'zgarishlarini bekor qiladi.
    function back() {
        overlay.hidden = true;
        document.body.classList.remove("ch-sa-open");
        document.body.style.overflow = "";
        gifts = {};
        clearFestive();
        closeModal();
        if (formsHost) {
            formsHost.querySelectorAll("input").forEach(function (i) { i.value = ""; });
        }
    }

    document.querySelectorAll("[data-sa-preview]").forEach(function (b) { b.addEventListener("click", open); });
    overlay.querySelectorAll("[data-sa-preview-back]").forEach(function (b) { b.addEventListener("click", back); });
    overlay.querySelectorAll("[data-sa-festive]").forEach(function (b) { b.addEventListener("click", toggleFestive); });
    overlay.querySelectorAll("[data-sa-open-modal]").forEach(function (b) { b.addEventListener("click", buildModal); });

    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") {
            if (modal && !modal.hidden) { closeModal(); }
            else if (!overlay.hidden) { back(); }
        }
    });
})();
