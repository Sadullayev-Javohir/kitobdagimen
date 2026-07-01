// Challenge sahifasi: uchuvchi kitob dekoratsiyalari, bayram belgilari, three.js 3D
// statistika (30 kunlik / 12 oylik), g'oliblar modali va like tizimi.
// three.js CDN'dan dinamik import qilinadi (CSP unpkg.com ga ruxsat beradi); yuklanmasa
// 2D zaxira grafik ko'rsatiladi.

const THREE_URL = "https://unpkg.com/three@0.160.0/build/three.module.js";

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

    // ─────────────────────── Uchuvchi kitob dekoratsiyalari ───────────────────────
    function initDecorations() {
        const host = document.querySelector("[data-ch-decor]");
        if (!host) { return; }
        const covers = Array.isArray(data.covers) ? data.covers.filter(Boolean) : [];

        const count = covers.length ? Math.min(covers.length, 14) : 10;
        const books = [];

        for (let i = 0; i < count; i++) {
            const el = document.createElement(covers.length ? "img" : "div");
            el.className = "ch-book";
            const w = 34 + Math.random() * 30;
            el.style.width = w + "px";
            el.style.height = (w * 1.4) + "px";
            if (covers.length) {
                el.src = covers[i % covers.length];
                el.referrerPolicy = "no-referrer";
                el.alt = "";
                el.addEventListener("error", () => { el.style.display = "none"; });
            } else {
                el.style.background = ["#1b4d3e", "#e8703a", "#c98b3a", "#7a5c3e"][i % 4];
            }
            const state = {
                el,
                x: Math.random() * window.innerWidth,
                y: Math.random() * window.innerHeight,
                vx: (Math.random() - 0.5) * 0.35,
                vy: (Math.random() - 0.5) * 0.35,
                rot: Math.random() * 360,
                vr: (Math.random() - 0.5) * 0.25
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

    // ─────────────────────── Bayram belgilari ───────────────────────
    function initFestive() {
        const host = document.querySelector("[data-ch-festive]");
        if (!host) { return; }
        const emojis = ["🎉", "🎊", "✨", "🎈", "📚", "⭐"];
        for (let i = 0; i < 26; i++) {
            const s = document.createElement("span");
            s.className = "ch-spark";
            s.textContent = emojis[i % emojis.length];
            s.style.left = Math.random() * 100 + "vw";
            s.style.animationDuration = (5 + Math.random() * 7) + "s";
            s.style.animationDelay = (Math.random() * 6) + "s";
            s.style.fontSize = (16 + Math.random() * 18) + "px";
            host.appendChild(s);
        }
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
                    // reflow -> restart animation
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

        // E'lon faol (2 kun) bo'lsa — dasturga har kirganda avtomatik ko'rsatamiz.
        const ann = data.announced;
        if (ann && ann.isAnnouncementActive) {
            setTimeout(open, 600);
        }
    }

    // ─────────────────────── Statistika (three.js + zaxira) ───────────────────────
    const PRIMARY = 0x1b4d3e;
    const MUTED = 0xc7ccd1;
    const ACCENT = 0xe8703a;

    function dailyValues() {
        return (data.daily || []).map((d) => ({ value: d.pages || 0, read: !!d.read }));
    }
    function monthlyValues() {
        return (data.monthly || []).map((m) => ({ value: m.pages || 0, read: !!m.read }));
    }

    function render2D(mode) {
        const host = document.querySelector("[data-bars2d]");
        if (!host) { return; }
        const vals = mode === "monthly" ? monthlyValues() : dailyValues();
        const max = Math.max(1, ...vals.map((v) => v.value));
        host.innerHTML = "";
        vals.forEach((v) => {
            const b = document.createElement("div");
            b.className = "b" + (v.read ? " read" : "");
            b.style.height = Math.max(2, (v.value / max) * 100) + "%";
            b.title = v.value + " bet";
            host.appendChild(b);
        });
    }

    async function initStats() {
        const canvas = document.querySelector("[data-stats-canvas]");
        const wrap = canvas ? canvas.parentElement : null;
        const fallback = document.querySelector("[data-stats-fallback]");
        let mode = "daily";

        // Tab boshqaruvi
        const tabs = document.querySelectorAll("[data-stat-tab]");
        function setActiveTab(m) {
            tabs.forEach((t) => t.classList.toggle("is-active", t.getAttribute("data-stat-tab") === m));
            document.querySelectorAll("[data-summary]").forEach((s) =>
                s.hidden = s.getAttribute("data-summary") !== m);
        }

        let three = null;
        try {
            three = await import(/* @vite-ignore */ THREE_URL);
        } catch (e) {
            three = null;
        }

        if (!three || !canvas || !wrap) {
            // 2D zaxira
            if (fallback) { fallback.hidden = false; }
            render2D(mode);
            tabs.forEach((t) => t.addEventListener("click", () => {
                mode = t.getAttribute("data-stat-tab");
                setActiveTab(mode);
                render2D(mode);
            }));
            return;
        }

        const THREE = three;
        const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
        renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));

        const scene = new THREE.Scene();
        const camera = new THREE.PerspectiveCamera(45, 1, 0.1, 1000);

        scene.add(new THREE.AmbientLight(0xffffff, 0.85));
        const dir = new THREE.DirectionalLight(0xffffff, 0.7);
        dir.position.set(6, 12, 8);
        scene.add(dir);

        const barGroup = new THREE.Group();
        scene.add(barGroup);

        function clearGroup() {
            while (barGroup.children.length) {
                const c = barGroup.children.pop();
                if (c.geometry) { c.geometry.dispose(); }
                if (c.material) { c.material.dispose(); }
                barGroup.remove(c);
            }
        }

        function buildBars(m) {
            clearGroup();
            const vals = m === "monthly" ? monthlyValues() : dailyValues();
            if (!vals.length) { return; }
            const max = Math.max(1, ...vals.map((v) => v.value));
            const n = vals.length;
            const spacing = m === "monthly" ? 1.5 : 0.85;
            const barW = m === "monthly" ? 0.9 : 0.55;
            const maxH = 8;
            const startX = -((n - 1) * spacing) / 2;

            vals.forEach((v, i) => {
                const h = Math.max(0.12, (v.value / max) * maxH);
                const geo = new THREE.BoxGeometry(barW, h, barW);
                const color = v.read ? (v.value === max ? ACCENT : PRIMARY) : MUTED;
                const mat = new THREE.MeshLambertMaterial({ color });
                const mesh = new THREE.Mesh(geo, mat);
                mesh.position.set(startX + i * spacing, h / 2, 0);
                barGroup.add(mesh);
            });

            // Kamerani ustunlar soniga moslaymiz
            const span = Math.max(6, n * spacing);
            camera.position.set(0, maxH * 0.9, span * 0.9);
            camera.lookAt(0, maxH * 0.35, 0);
        }

        function resize() {
            const w = wrap.clientWidth || 600;
            const h = wrap.clientHeight || 340;
            renderer.setSize(w, h, false);
            camera.aspect = w / h;
            camera.updateProjectionMatrix();
        }

        buildBars(mode);
        resize();
        window.addEventListener("resize", resize);

        let t = 0;
        function animate() {
            t += 0.0035;
            barGroup.rotation.y = Math.sin(t) * 0.5;
            renderer.render(scene, camera);
            requestAnimationFrame(animate);
        }
        requestAnimationFrame(animate);

        tabs.forEach((tab) => tab.addEventListener("click", () => {
            mode = tab.getAttribute("data-stat-tab");
            setActiveTab(mode);
            buildBars(mode);
        }));
    }

    // ─────────────────────── Ishga tushirish ───────────────────────
    initDecorations();
    initFestive();
    initLikes();
    initModal();
    initStats();
})();
