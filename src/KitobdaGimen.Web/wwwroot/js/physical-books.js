// "Almashish" sahifasi: kitob qo'shish (qidiruv/qo'lda/muqova), band qilish, egasi amallari
// va band qilingan kitoblar uchun 24 soatlik teskari sanoq.
(function () {
    "use strict";

    const kitob = window.kitob;
    if (!kitob) return;

    function esc(s) {
        return String(s == null ? "" : s).replace(/[&<>"']/g, c => ({
            "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
        }[c]));
    }

    // ---------- Kitob qo'shish paneli ----------
    const addToggle = document.getElementById("pbAddToggle");
    const addPanel = document.getElementById("pbAddPanel");
    const searchInput = document.getElementById("pbSearchInput");
    const suggestions = document.getElementById("pbSuggestions");
    const selected = document.getElementById("pbSelected");
    const selectedText = document.getElementById("pbSelectedText");
    const selectedClear = document.getElementById("pbSelectedClear");
    const manualToggle = document.getElementById("pbManualToggle");
    const manualForm = document.getElementById("pbManualForm");
    const manualTitle = document.getElementById("pbManualTitle");
    const manualAuthor = document.getElementById("pbManualAuthor");
    const addSave = document.getElementById("pbAddSave");
    const addCancel = document.getElementById("pbAddCancel");

    // Muqova yuklash (qo'lda kiritish uchun)
    const coverInput = document.getElementById("pbManualCover");
    const coverPreview = document.getElementById("pbManualCoverPreview");
    const coverClear = document.getElementById("pbManualCoverClear");
    const coverHint = document.getElementById("pbManualCoverHint");

    let selectedBookId = null;
    let searchTimer = null;
    let coverFile = null;
    let coverUrl = null;

    function openPanel() { addPanel.hidden = false; searchInput.focus(); }
    function closePanel() {
        addPanel.hidden = true;
        resetForm();
    }
    function resetCover() {
        coverFile = null;
        coverUrl = null;
        if (coverInput) coverInput.value = "";
        if (coverPreview) coverPreview.innerHTML = '<span class="material-symbols-outlined">image</span>';
        if (coverClear) coverClear.hidden = true;
        if (coverHint) coverHint.textContent = "Ixtiyoriy — JPG/PNG, 5 MB gacha.";
    }
    function resetForm() {
        selectedBookId = null;
        searchInput.value = "";
        suggestions.innerHTML = "";
        suggestions.hidden = true;
        selected.hidden = true;
        manualForm.hidden = true;
        manualTitle.value = "";
        manualAuthor.value = "";
        resetCover();
    }

    // Katalog yoki asaxiy kitobini tanlash — bookId'ni saqlaydi.
    function pickBook(book) {
        selectedBookId = book.id;
        selectedText.textContent = `${book.title} — ${book.author}`;
        selected.hidden = false;
        suggestions.hidden = true;
        suggestions.innerHTML = "";
        searchInput.value = "";
        manualForm.hidden = true;
    }

    if (addToggle) addToggle.addEventListener("click", () => addPanel.hidden ? openPanel() : closePanel());
    if (addCancel) addCancel.addEventListener("click", closePanel);
    if (selectedClear) selectedClear.addEventListener("click", () => {
        selectedBookId = null;
        selected.hidden = true;
    });
    if (manualToggle) manualToggle.addEventListener("click", () => {
        manualForm.hidden = !manualForm.hidden;
        if (!manualForm.hidden) manualTitle.focus();
    });

    // Muqova rasmini tanlash — oldindan ko'rsatamiz, yuklashni saqlashda bajaramiz.
    if (coverInput) coverInput.addEventListener("change", () => {
        const f = coverInput.files && coverInput.files[0];
        if (!f) { resetCover(); return; }
        coverFile = f;
        coverUrl = null;
        if (coverClear) coverClear.hidden = false;
        if (coverHint) coverHint.textContent = f.name;
        const reader = new FileReader();
        reader.onload = (ev) => {
            coverPreview.innerHTML = "";
            const img = document.createElement("img");
            img.src = ev.target.result;
            coverPreview.appendChild(img);
        };
        reader.readAsDataURL(f);
    });
    if (coverClear) coverClear.addEventListener("click", resetCover);

    async function uploadCover(file) {
        const fd = new FormData();
        fd.append("file", file);
        const res = await fetch("/books/upload-cover", {
            method: "POST",
            headers: {
                "X-Requested-With": "XMLHttpRequest",
                "RequestVerificationToken": kitob.antiforgeryToken()
            },
            body: fd
        });
        if (res.status === 401) { window.location.href = "/"; return null; }
        if (!res.ok) {
            let message = "Rasmni yuklab bo'lmadi.";
            try { const d = await res.json(); message = d.message || message; } catch { /* ignore */ }
            throw new Error(message);
        }
        const data = await res.json();
        return data.url;
    }

    // Kitob qidirish (katalog + asaxiy) — Feed pikeri bilan bir xil.
    if (searchInput) searchInput.addEventListener("input", () => {
        clearTimeout(searchTimer);
        const q = searchInput.value.trim();
        if (q.length < 2) { suggestions.hidden = true; return; }
        suggestions.dataset.q = q;
        searchTimer = setTimeout(async () => {
            try {
                const res = await fetch(`/books/search?q=${encodeURIComponent(q)}`, {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                if (!res.ok) return;
                const books = await res.json();
                suggestions.innerHTML = "";
                books.forEach(b => {
                    const div = document.createElement("div");
                    div.className = "pb-suggestion";
                    div.textContent = `${b.title} — ${b.author}`;
                    div.addEventListener("click", () => pickBook(b));
                    suggestions.appendChild(div);
                });
                suggestions.hidden = false;
                // Tashqi (asaxiy) natijalar — tanlanganda import qilinib, lokal kitob qaytadi.
                kitob.renderAsaxiyBooks(q, suggestions, pickBook);
            } catch { /* tarmoq xatosi — jim */ }
        }, 250);
    });

    // Kitob qo'shish.
    if (addSave) addSave.addEventListener("click", async () => {
        let command;
        if (selectedBookId) {
            command = { bookId: selectedBookId };
        } else {
            const title = manualTitle.value.trim();
            if (!title) {
                kitob.showToast("Kitobni tanlang yoki nomini qo'lda kiriting.");
                if (manualForm.hidden) { manualForm.hidden = false; manualTitle.focus(); }
                return;
            }
            command = { manualTitle: title, manualAuthor: manualAuthor.value.trim() || null };
        }

        addSave.disabled = true;
        try {
            // Qo'lda kitob uchun muqova tanlangan bo'lsa — avval yuklaymiz.
            if (!selectedBookId && coverFile && !coverUrl) {
                coverUrl = await uploadCover(coverFile);
            }
            if (!selectedBookId && coverUrl) command.manualCoverUrl = coverUrl;

            await kitob.apiPost("/almashish/add", command);
            kitob.showToast("Kitob qo'shildi ✓");
            location.reload();
        } catch (err) {
            kitob.showToast(err.message || "Xatolik yuz berdi.");
            addSave.disabled = false;
        }
    });

    // ---------- Kitob amallari (band qilish, topshirish, qaytarish, ...) ----------
    const CONFIRM_ACTIONS = {
        delete: "Bu kitobni ro'yxatdan o'chirasizmi?",
        cancel: "Band qilishni bekor qilasizmi?",
        return: "Kitob qaytarib olindimi?"
    };

    document.addEventListener("click", async (e) => {
        const btn = e.target.closest("[data-pb-action]");
        if (!btn) return;

        const action = btn.dataset.pbAction;
        const id = btn.dataset.pbId;
        if (!id) return;

        const confirmMsg = CONFIRM_ACTIONS[action];
        if (confirmMsg && !window.confirm(confirmMsg)) return;

        btn.disabled = true;
        try {
            await kitob.apiPost(`/almashish/${id}/${action}`);
            const messages = {
                reserve: "Kitob band qilindi. 24 soat ichida egasidan olib keting!",
                confirm: "Topshirish tasdiqlandi.",
                return: "Kitob qaytarildi.",
                cancel: "Band qilish bekor qilindi.",
                delete: "Kitob o'chirildi."
            };
            kitob.showToast(messages[action] || "Bajarildi ✓");
            location.reload();
        } catch (err) {
            kitob.showToast(err.message || "Xatolik yuz berdi.");
            btn.disabled = false;
        }
    });

    // ---------- Kartochka HTML'i (kutubxona qidiruvi uchun — partial bilan bir xil) ----------
    const BADGE_CLASS = { 0: "pb-badge--free", 1: "pb-badge--reserved", 2: "pb-badge--reading" };

    function userLink(url, name) {
        return `<a class="pb-user-link" href="${esc(url)}">${esc(name)}</a>`;
    }

    function libCard(b) {
        const cover = b.coverUrl
            ? `<img src="${esc(b.coverUrl)}" alt="${esc(b.title)} muqovasi" loading="lazy" referrerpolicy="no-referrer" />`
            : `<span class="material-symbols-outlined pb-cover-ph">menu_book</span>`;
        const badgeClass = BADGE_CLASS[b.status] || "pb-badge--free";

        // Egasi — hammaga ko'rinadi.
        const ownerMeta = b.isMine
            ? `<p class="pb-meta pb-meta--muted"><span class="material-symbols-outlined">person</span><span>Sizning kitobingiz</span></p>`
            : `<p class="pb-meta pb-meta--muted"><span class="material-symbols-outlined">person</span><span>Egasi: ${userLink(b.ownerProfileUrl, b.ownerName)}</span></p>`;

        // Band qilindi / O'qiyapti holati.
        let statusMeta = "";
        let countdown = "";
        if (b.status === 1 && b.reserverName) {
            statusMeta = `<p class="pb-meta"><span class="material-symbols-outlined">schedule</span><span>${userLink(b.reserverProfileUrl, b.reserverName)} band qildi</span></p>`;
            if (b.reservationExpiresAt) {
                countdown = `<p class="pb-countdown" data-pb-expires="${esc(b.reservationExpiresAt)}"><span class="material-symbols-outlined">hourglass_bottom</span><span class="pb-countdown-text">—</span></p>`;
            }
        } else if (b.status === 2 && b.reserverName) {
            statusMeta = `<p class="pb-meta"><span class="material-symbols-outlined">auto_stories</span><span>${userLink(b.reserverProfileUrl, b.reserverName)} o'qiyapti</span></p>`;
        }

        // Amallar.
        let actions = "";
        if (!b.isMine && b.status === 0) {
            actions = `<div class="pb-actions"><button class="btn btn-accent btn-sm" data-pb-action="reserve" data-pb-id="${b.id}">O'qimoqchiman</button></div>`;
        } else if (!b.isMine && b.reservedByMe && b.status === 1) {
            actions = `<div class="pb-actions"><button class="btn btn-ghost btn-sm" data-pb-action="cancel" data-pb-id="${b.id}">Bandni bekor qilish</button></div>`;
        }

        return `<article class="pb-card" data-pb-card="${b.id}" data-status="${b.status}">
            <div class="pb-cover">${cover}<span class="pb-badge ${badgeClass}">${esc(b.statusText)}</span></div>
            <div class="pb-body">
                <h3 class="pb-title" title="${esc(b.title)}">${esc(b.title)}</h3>
                <p class="pb-author">${esc(b.author)}</p>
                ${ownerMeta}
                ${statusMeta}
                ${countdown}
                ${actions}
            </div>
        </article>`;
    }

    // ---------- Kutubxona qidiruvi ----------
    const libSearch = document.getElementById("pbLibSearch");
    const libGrid = document.getElementById("pbLibGrid");
    const libEmpty = document.getElementById("pbLibEmpty");
    let libTimer = null;

    if (libSearch) libSearch.addEventListener("input", () => {
        clearTimeout(libTimer);
        const q = libSearch.value.trim();
        libTimer = setTimeout(async () => {
            try {
                const res = await fetch(`/almashish/search?q=${encodeURIComponent(q)}`, {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                if (!res.ok) return;
                const books = await res.json();
                libGrid.innerHTML = books.map(libCard).join("");
                libEmpty.hidden = books.length > 0;
                libEmpty.textContent = q
                    ? "Bu qidiruv bo'yicha kitob topilmadi."
                    : "Hozircha almashish uchun kitob yo'q.";
                startCountdowns();
            } catch { /* tarmoq xatosi — jim */ }
        }, 250);
    });

    // ---------- 24 soatlik teskari sanoq ----------
    // Har bir [data-pb-expires] elementini har soniyada yangilaydi.
    let countdownTimer = null;

    function formatRemaining(ms) {
        if (ms <= 0) return "Muddati tugadi";
        const totalMin = Math.floor(ms / 60000);
        const hours = Math.floor(totalMin / 60);
        const mins = totalMin % 60;
        if (hours > 0) return `${hours} soat ${mins} daqiqa qoldi`;
        if (mins > 0) return `${mins} daqiqa qoldi`;
        const secs = Math.floor(ms / 1000);
        return `${secs} soniya qoldi`;
    }

    function tickCountdowns() {
        const nodes = document.querySelectorAll(".pb-countdown[data-pb-expires]");
        if (nodes.length === 0) {
            if (countdownTimer) { clearInterval(countdownTimer); countdownTimer = null; }
            return;
        }
        const now = Date.now();
        nodes.forEach(node => {
            const expires = Date.parse(node.dataset.pbExpires);
            if (isNaN(expires)) return;
            const remaining = expires - now;
            const text = node.querySelector(".pb-countdown-text");
            if (text) text.textContent = formatRemaining(remaining);
            node.classList.toggle("pb-countdown--soon", remaining > 0 && remaining < 3600000);
            node.classList.toggle("pb-countdown--over", remaining <= 0);
        });
    }

    function startCountdowns() {
        tickCountdowns();
        if (!countdownTimer && document.querySelector(".pb-countdown[data-pb-expires]")) {
            countdownTimer = setInterval(tickCountdowns, 1000);
        }
    }

    startCountdowns();
})();
