// "Almashish" sahifasi: kitob qo'shish (qidiruv/qo'lda), band qilish va egasi amallari.
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

    let selectedBookId = null;
    let searchTimer = null;

    function openPanel() { addPanel.hidden = false; searchInput.focus(); }
    function closePanel() {
        addPanel.hidden = true;
        resetForm();
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

    // Kitob qidirish (katalog + asaxiy) — Feed pikeri bilan bir xil.
    if (searchInput) searchInput.addEventListener("input", () => {
        clearTimeout(searchTimer);
        const q = searchInput.value.trim();
        if (q.length < 2) { suggestions.hidden = true; return; }
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
                reserve: "Kitob 24 soatga band qilindi. Egasi bilan bog'laning!",
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

    // ---------- Kutubxona qidiruvi ----------
    const libSearch = document.getElementById("pbLibSearch");
    const libGrid = document.getElementById("pbLibGrid");
    const libEmpty = document.getElementById("pbLibEmpty");
    let libTimer = null;

    function libCard(b) {
        const cover = b.coverUrl
            ? `<img src="${esc(b.coverUrl)}" alt="${esc(b.title)} muqovasi" loading="lazy" referrerpolicy="no-referrer" />`
            : `<span class="material-symbols-outlined pb-cover-ph">menu_book</span>`;
        return `<article class="pb-card" data-pb-card="${b.id}" data-status="0">
            <div class="pb-cover">${cover}<span class="pb-badge pb-badge--free">${esc(b.statusText)}</span></div>
            <div class="pb-body">
                <h3 class="pb-title" title="${esc(b.title)}">${esc(b.title)}</h3>
                <p class="pb-author">${esc(b.author)}</p>
                <p class="pb-meta pb-meta--muted"><span class="material-symbols-outlined">person</span><span>${esc(b.ownerName)}</span></p>
                <div class="pb-actions">
                    <button class="btn btn-accent btn-sm" data-pb-action="reserve" data-pb-id="${b.id}">O'qimoqchiman</button>
                </div>
            </div>
        </article>`;
    }

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
                    : "Hozircha almashish uchun mavjud kitob yo'q.";
            } catch { /* tarmoq xatosi — jim */ }
        }, 250);
    });
})();
