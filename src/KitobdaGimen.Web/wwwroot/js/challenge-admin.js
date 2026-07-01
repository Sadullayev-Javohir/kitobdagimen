// Challenge admin preview: standings'dan g'olib modalini quradi (preview) va bayram
// ko'rinishini sinash tugmasi.
(function () {
    "use strict";

    function medal(rank) {
        var cls = rank === 1 ? "ch-medal-gold" : rank === 2 ? "ch-medal-silver" : "ch-medal-bronze";
        return '<span class="material-symbols-outlined ch-medal-ic ' + cls + '">workspace_premium</span>';
    }
    function initial(name) { return name && name.trim() ? name.trim()[0].toUpperCase() : "?"; }
    function esc(s) {
        return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
        });
    }

    let standings = [];
    const dataEl = document.querySelector("[data-preview-standings]");
    if (dataEl) {
        try { standings = JSON.parse(dataEl.textContent) || []; } catch (e) { standings = []; }
    }

    // Preview modalini standings'dan to'ldiramiz
    const winnersHost = document.querySelector("[data-preview-winners]");
    if (winnersHost) {
        if (!standings.length) {
            winnersHost.innerHTML = '<p class="muted">Bu davrda reyting bo\'sh — preview uchun ma\'lumot yo\'q.</p>';
        } else {
            const order = [2, 1, 3];
            let html = "";
            order.forEach(function (slot) {
                const w = standings.find(function (x) { return x.rank === slot; });
                if (!w) { return; }
                const avatar = w.avatarUrl
                    ? '<img class="avatar ch-avatar" src="' + esc(w.avatarUrl) + '" alt="" referrerpolicy="no-referrer" />'
                    : '<span class="avatar ch-avatar">' + esc(initial(w.fullName)) + "</span>";
                html +=
                    '<div class="ch-modal-winner ch-place-' + w.rank + '">' +
                        '<div class="ch-medal">' + medal(w.rank) + "</div>" +
                        avatar +
                        '<div class="ch-modal-name">' + esc(w.fullName) + "</div>" +
                        '<div class="ch-modal-stats muted">' +
                            w.pagesRead + " bet · " + w.booksRead + " kitob · kuniga " + w.avgPagesPerDay + " bet" +
                        "</div>" +
                    "</div>";
            });
            winnersHost.innerHTML = html;
        }
    }

    // Modalni ochish/yopish
    const modal = document.querySelector("[data-preview-modal-box]");
    function openModal() { if (modal) { modal.hidden = false; document.body.style.overflow = "hidden"; } }
    function closeModal() { if (modal) { modal.hidden = true; document.body.style.overflow = ""; } }

    document.querySelectorAll("[data-preview-modal]").forEach(function (b) {
        b.addEventListener("click", openModal);
    });
    document.querySelectorAll("[data-preview-close]").forEach(function (b) {
        b.addEventListener("click", closeModal);
    });
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") { closeModal(); }
    });

    // Bayram ko'rinishini sinash
    let festiveOn = false;
    document.querySelectorAll("[data-festive-toggle]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            festiveOn = !festiveOn;
            document.querySelector(".ch-page").classList.toggle("is-festive", festiveOn);
            let host = document.querySelector("[data-ch-festive]");
            if (festiveOn && !host) {
                host = document.createElement("div");
                host.className = "ch-festive";
                host.setAttribute("data-ch-festive", "");
                document.body.appendChild(host);
                const icons = [
                    { icon: "celebration", cls: "c-accent" },
                    { icon: "auto_awesome", cls: "c-gold" },
                    { icon: "star", cls: "c-gold" },
                    { icon: "emoji_events", cls: "c-gold" },
                    { icon: "workspace_premium", cls: "c-silver" },
                    { icon: "menu_book", cls: "c-primary" },
                    { icon: "redeem", cls: "c-accent" },
                    { icon: "local_fire_department", cls: "c-accent" }
                ];
                for (let i = 0; i < 30; i++) {
                    const pick = icons[i % icons.length];
                    const s = document.createElement("span");
                    s.className = "ch-spark material-symbols-outlined " + pick.cls;
                    s.textContent = pick.icon;
                    s.style.left = Math.random() * 100 + "vw";
                    s.style.animationDuration = (5 + Math.random() * 7) + "s";
                    s.style.animationDelay = (Math.random() * 6) + "s";
                    s.style.fontSize = (18 + Math.random() * 20) + "px";
                    host.appendChild(s);
                }
            } else if (!festiveOn && host) {
                host.remove();
            }
        });
    });
})();
