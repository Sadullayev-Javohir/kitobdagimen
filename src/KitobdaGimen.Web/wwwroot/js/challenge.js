// Challenge sahifasi: orqa fonda ko'plab uchuvchi kitob muqovalari, zamonaviy icon'li
// bayram effektlari, GitHub uslubidagi o'qish statistikasi (30 kunlik + yillik heatmap,
// avvalgi yillarni ko'rish, kun ustiga bosilganda o'sha kuni o'qilgan kitoblar), g'oliblar
// modali va like tizimi.

(function () {
    "use strict";

    const dataEl = document.querySelector("script[data-ch-data]");
    if (!dataEl) { return; }

    let data;
    try {
        data = JSON.parse(dataEl.textContent);
    } catch (e) {
        return;
    }

    const MONTHS_FULL = ["Yanvar", "Fevral", "Mart", "Aprel", "May", "Iyun",
        "Iyul", "Avgust", "Sentabr", "Oktabr", "Noyabr", "Dekabr"];
    const MONTHS_SHORT = ["Yan", "Fev", "Mar", "Apr", "May", "Iyn",
        "Iyl", "Avg", "Sen", "Okt", "Noy", "Dek"];

    // ─────────────────────── Uchuvchi kitob dekoratsiyalari (ko'p) ───────────────────────
    function initDecorations() {
        const host = document.querySelector("[data-ch-decor]");
        if (!host) { return; }
        const covers = Array.isArray(data.covers) ? data.covers.filter(Boolean) : [];

        // Juda ko'p kitob — muqova bo'lsa 50 tagacha, bo'lmasa 30 ta rangli.
        const count = covers.length ? Math.min(Math.max(covers.length, 24), 54) : 30;
        const books = [];

        for (let i = 0; i < count; i++) {
            const el = document.createElement(covers.length ? "img" : "div");
            el.className = "ch-book";
            const w = 30 + Math.random() * 34;
            el.style.width = w + "px";
            el.style.height = (w * 1.42) + "px";
            if (covers.length) {
                el.src = covers[i % covers.length];
                el.referrerPolicy = "no-referrer";
                el.alt = "";
                el.addEventListener("error", () => { el.style.display = "none"; });
            } else {
                el.style.background = ["#1b4d3e", "#e8703a", "#c98b3a", "#7a5c3e", "#4a6d5e"][i % 5];
            }
            const state = {
                el,
                x: Math.random() * window.innerWidth,
                y: Math.random() * window.innerHeight,
                vx: (Math.random() - 0.5) * 0.4,
                vy: (Math.random() - 0.5) * 0.4,
                rot: Math.random() * 360,
                vr: (Math.random() - 0.5) * 0.3
            };
            host.appendChild(el);
            books.push(state);
        }

        function tick() {
            const W = window.innerWidth, H = window.innerHeight;
            for (const b of books) {
                b.x += b.vx; b.y += b.vy; b.rot += b.vr;
                if (b.x < -80) { b.x = W + 60; }
                if (b.x > W + 80) { b.x = -60; }
                if (b.y < -120) { b.y = H + 60; }
                if (b.y > H + 120) { b.y = -60; }
                b.el.style.transform = `translate(${b.x}px, ${b.y}px) rotate(${b.rot}deg)`;
            }
            requestAnimationFrame(tick);
        }
        requestAnimationFrame(tick);
    }

    // ─────────────────────── Bayram belgilari (zamonaviy iconlar) ───────────────────────
    const FESTIVE_ICONS = [
        { icon: "celebration", cls: "c-accent" },
        { icon: "auto_awesome", cls: "c-gold" },
        { icon: "star", cls: "c-gold" },
        { icon: "emoji_events", cls: "c-gold" },
        { icon: "workspace_premium", cls: "c-silver" },
        { icon: "menu_book", cls: "c-primary" },
        { icon: "redeem", cls: "c-accent" },
        { icon: "local_fire_department", cls: "c-accent" }
    ];

    function spawnFestive(host, n) {
        for (let i = 0; i < n; i++) {
            const pick = FESTIVE_ICONS[i % FESTIVE_ICONS.length];
            const s = document.createElement("span");
            s.className = "ch-spark material-symbols-outlined " + pick.cls;
            s.textContent = pick.icon;
            s.style.left = Math.random() * 100 + "vw";
            s.style.animationDuration = (5 + Math.random() * 7) + "s";
            s.style.animationDelay = (Math.random() * 6) + "s";
            s.style.fontSize = (18 + Math.random() * 20) + "px";
            host.appendChild(s);
        }
    }

    function initFestive() {
        const host = document.querySelector("[data-ch-festive]");
        if (!host) { return; }
        spawnFestive(host, 30);
    }

    // ─────────────────────── Like tizimi ───────────────────────
    function initLikes() {
        const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenEl ? tokenEl.value : "";

        document.querySelectorAll("[data-like-btn]").forEach((btn) => {
            btn.addEventListener("click", async () => {
                const id = btn.getAttribute("data-winner-id");
                btn.disabled = true;
                try {
                    const res = await fetch(`/challenge/winners/${id}/like`, {
                        method: "POST",
                        headers: { "RequestVerificationToken": token }
                    });
                    if (!res.ok) { throw new Error("like failed"); }
                    const json = await res.json();
                    const countEl = btn.querySelector("[data-like-count]");
                    if (countEl) { countEl.textContent = json.likeCount; }
                    btn.classList.toggle("is-liked", json.liked);
                    btn.setAttribute("aria-pressed", json.liked ? "true" : "false");
                    btn.classList.remove("pop");
                    void btn.offsetWidth;
                    btn.classList.add("pop");
                } catch (e) {
                    // jimgina o'tkazamiz
                } finally {
                    btn.disabled = false;
                }
            });
        });
    }

    // ─────────────────────── G'oliblar modali ───────────────────────
    function initModal() {
        const modal = document.querySelector("[data-winners-modal]");
        if (!modal) { return; }

        function open() { modal.hidden = false; document.body.style.overflow = "hidden"; }
        function close() { modal.hidden = true; document.body.style.overflow = ""; }

        modal.querySelectorAll("[data-modal-close]").forEach((el) =>
            el.addEventListener("click", close));
        document.addEventListener("keydown", (e) => {
            if (e.key === "Escape" && !modal.hidden) { close(); }
        });
        document.querySelectorAll("[data-open-winners]").forEach((el) =>
            el.addEventListener("click", open));

        const ann = data.announced;
        if (ann && ann.isAnnouncementActive) {
            setTimeout(open, 600);
        }
    }

    // ─────────────────────── Statistika: GitHub uslubidagi heatmap ───────────────────────
    function parseDate(s) {
        const p = String(s).split("-");
        return new Date(Number(p[0]), Number(p[1]) - 1, Number(p[2]));
    }

    function level(pages, max) {
        if (!pages || pages <= 0) { return 0; }
        const r = pages / (max || 1);
        if (r <= 0.25) { return 1; }
        if (r <= 0.5) { return 2; }
        if (r <= 0.75) { return 3; }
        return 4;
    }

    // Kunlarni Monday-first haftalarga bo'ladi (null — bo'sh katak).
    function buildWeeks(days) {
        if (!days.length) { return []; }
        const first = parseDate(days[0].date);
        const lead = (first.getDay() + 6) % 7; // Dushanba = 0
        const cells = [];
        for (let i = 0; i < lead; i++) { cells.push(null); }
        for (const d of days) { cells.push(d); }
        while (cells.length % 7 !== 0) { cells.push(null); }
        const weeks = [];
        for (let i = 0; i < cells.length; i += 7) {
            weeks.push(cells.slice(i, i + 7));
        }
        return weeks;
    }

    // Bir kun uchun katak DOM.
    function dayCell(d, max, onSelect) {
        const cell = document.createElement("div");
        if (!d) {
            cell.className = "gh-day gh-empty";
            return cell;
        }
        const lv = level(d.pages, max);
        cell.className = "gh-day lv-" + lv;
        cell.setAttribute("data-date", d.date);
        cell.setAttribute("data-pages", d.pages);
        cell.setAttribute("data-books", d.books || 0);
        const dt = parseDate(d.date);
        const human = dt.getDate() + "-" + MONTHS_FULL[dt.getMonth()] + " " + dt.getFullYear();
        cell.title = d.pages > 0
            ? `${human}: ${d.books || 0} kitob · ${d.pages} bet`
            : `${human}: o'qilmagan`;
        cell.setAttribute("role", "button");
        cell.setAttribute("tabindex", "0");
        cell.addEventListener("click", () => onSelect(cell, d, dt, human));
        cell.addEventListener("keydown", (e) => {
            if (e.key === "Enter" || e.key === " ") { e.preventDefault(); onSelect(cell, d, dt, human); }
        });
        return cell;
    }

    // Heatmap qurish: oy yorliqlari + hafta kunlari + kataklar.
    function buildHeatmap(days, max, detailEl, showMonths) {
        const weeks = buildWeeks(days);

        const cal = document.createElement("div");
        cal.className = "gh-cal";

        // Oy yorliqlari
        if (showMonths) {
            const monthsRow = document.createElement("div");
            monthsRow.className = "gh-months";
            let prevMonth = -1;
            weeks.forEach((week) => {
                const box = document.createElement("span");
                box.className = "gh-m";
                const rep = week.find((c) => c);
                if (rep) {
                    const m = parseDate(rep.date).getMonth();
                    if (m !== prevMonth) {
                        box.textContent = MONTHS_SHORT[m];
                        prevMonth = m;
                    }
                }
                monthsRow.appendChild(box);
            });
            cal.appendChild(monthsRow);
        }

        const cols = document.createElement("div");
        cols.className = "gh-cols";

        // Hafta kunlari yorlig'i (Du / Cho / Ju)
        const wd = document.createElement("div");
        wd.className = "gh-wd";
        const wdLabels = ["Du", "", "Cho", "", "Ju", "", ""];
        wdLabels.forEach((lab) => {
            const s = document.createElement("span");
            s.textContent = lab;
            wd.appendChild(s);
        });
        cols.appendChild(wd);

        function selectDay(cellEl, d, dt, human) {
            cal.querySelectorAll(".gh-day.is-sel").forEach((c) => c.classList.remove("is-sel"));
            cellEl.classList.add("is-sel");
            if (!detailEl) { return; }
            if (d.pages > 0) {
                detailEl.innerHTML =
                    '<span class="material-symbols-outlined">event_available</span>' +
                    '<span><strong>' + human + '</strong> — ' +
                    '<b>' + (d.books || 0) + '</b> kitob o\'qildi · <b>' + d.pages + '</b> bet</span>';
            } else {
                detailEl.innerHTML =
                    '<span class="material-symbols-outlined">event_busy</span>' +
                    '<span><strong>' + human + '</strong> — bu kuni o\'qilmagan</span>';
            }
        }

        const weeksEl = document.createElement("div");
        weeksEl.className = "gh-weeks";
        weeks.forEach((week) => {
            const col = document.createElement("div");
            col.className = "gh-week";
            week.forEach((d) => col.appendChild(dayCell(d, max, selectDay)));
            weeksEl.appendChild(col);
        });
        cols.appendChild(weeksEl);

        cal.appendChild(cols);
        return cal;
    }

    function initStats() {
        const host = document.querySelector("[data-heatmap]");
        if (!host) { return; }

        const detailEl = document.querySelector("[data-day-detail]");
        const summaryEl = document.querySelector("[data-stat-summary]");
        const yearNav = document.querySelector("[data-year-nav]");
        const yearLabel = document.querySelector("[data-year-label]");
        const prevBtn = document.querySelector("[data-year-prev]");
        const nextBtn = document.querySelector("[data-year-next]");
        const tabs = document.querySelectorAll("[data-stat-tab]");

        const availableYears = Array.isArray(data.availableYears) && data.availableYears.length
            ? data.availableYears.slice().sort((a, b) => b - a)
            : [(data.year && data.year.year) || new Date().getFullYear()];
        const maxYear = availableYears[0];
        const minYear = availableYears[availableYears.length - 1];

        const yearCache = {};
        if (data.year && data.year.year) { yearCache[data.year.year] = data.year; }
        let currentYear = (data.year && data.year.year) || maxYear;
        let mode = "year";

        function setSummary(text) {
            if (summaryEl) { summaryEl.innerHTML = text; }
        }
        function resetDetail() {
            if (detailEl) {
                detailEl.innerHTML =
                    '<span class="material-symbols-outlined">touch_app</span>' +
                    '<span class="muted">Kunni ustiga bosing — o\'sha kuni qancha kitob o\'qiganingiz ko\'rsatiladi.</span>';
            }
        }

        // Yillik kalendar gorizontal aylantiriladigan. Joriy yil ko'rsatilganda
        // yanvar emas, balki hozirgi oy ko'rinadigan qilib aylantiramiz.
        function scrollYearToMonth(calEl, year) {
            const now = new Date();
            if (year !== now.getFullYear()) {
                host.scrollLeft = 0;
                return;
            }
            const mm = String(now.getMonth() + 1).padStart(2, "0");
            const ymPrefix = year + "-" + mm;
            let cell = calEl.querySelector('.gh-day[data-date="' + ymPrefix + '-01"]')
                || calEl.querySelector('.gh-day[data-date^="' + ymPrefix + '"]');
            if (!cell) { return; }
            const col = cell.closest(".gh-week");
            if (!col) { return; }
            const colRect = col.getBoundingClientRect();
            const hostRect = host.getBoundingClientRect();
            host.scrollLeft += (colRect.left - hostRect.left) - 40;
        }

        function renderYear(cal) {
            host.innerHTML = "";
            const calEl = buildHeatmap(cal.days || [], cal.maxPages || 0, detailEl, true);
            host.appendChild(calEl);
            setSummary(
                '<span class="material-symbols-outlined">menu_book</span> <strong>' + (cal.totalBooks || 0) + '</strong> kitob' +
                ' · <span class="material-symbols-outlined">description</span> <strong>' + (cal.totalPages || 0) + '</strong> bet' +
                ' · <span class="material-symbols-outlined">event_available</span> <strong>' + (cal.activeDays || 0) + '</strong> kun o\'qildi'
            );
            resetDetail();
            scrollYearToMonth(calEl, cal.year);
        }

        function renderDaily() {
            const days = data.daily || [];
            const max = days.reduce((m, d) => Math.max(m, d.pages || 0), 0);
            host.innerHTML = "";
            host.appendChild(buildHeatmap(days, max, detailEl, false));
            setSummary(
                '<span class="material-symbols-outlined">menu_book</span> <strong>' + (data.monthBooks || 0) + '</strong> kitob' +
                ' · <span class="material-symbols-outlined">description</span> <strong>' + (data.monthPages || 0) + '</strong> bet' +
                ' · <span class="material-symbols-outlined">event_available</span> <strong>' + (data.monthActiveDays || 0) + '</strong> kun (30 kun)'
            );
            resetDetail();
        }

        function updateYearNav() {
            if (yearLabel) { yearLabel.textContent = currentYear; }
            if (prevBtn) { prevBtn.disabled = currentYear <= minYear; }
            if (nextBtn) { nextBtn.disabled = currentYear >= maxYear; }
        }

        async function showYear(year) {
            currentYear = year;
            updateYearNav();
            if (yearCache[year]) {
                renderYear(yearCache[year]);
                return;
            }
            host.innerHTML = '<div class="gh-loading">Yuklanmoqda…</div>';
            try {
                const res = await fetch("/challenge/stats/" + year);
                if (!res.ok) { throw new Error("stats failed"); }
                const cal = await res.json();
                yearCache[year] = cal;
                if (mode === "year" && currentYear === year) { renderYear(cal); }
            } catch (e) {
                host.innerHTML = '<div class="gh-loading">Ma\'lumot yuklanmadi.</div>';
            }
        }

        function setMode(m) {
            mode = m;
            tabs.forEach((t) => t.classList.toggle("is-active", t.getAttribute("data-stat-tab") === m));
            if (yearNav) { yearNav.hidden = m !== "year"; }
            if (m === "year") {
                showYear(currentYear);
            } else {
                renderDaily();
            }
        }

        tabs.forEach((t) => t.addEventListener("click", () => setMode(t.getAttribute("data-stat-tab"))));
        if (prevBtn) { prevBtn.addEventListener("click", () => { if (currentYear > minYear) { showYear(currentYear - 1); } }); }
        if (nextBtn) { nextBtn.addEventListener("click", () => { if (currentYear < maxYear) { showYear(currentYear + 1); } }); }

        updateYearNav();
        setMode("daily");
    }

    // ─────────────────────── Ishga tushirish ───────────────────────
    initDecorations();
    initFestive();
    initLikes();
    initModal();
    initStats();
})();
