/* =============================================================================
   Yillik Kitob Yakuni (Year in Review) — mijoz mantiqi.
   - Auto-popup: 20-dekabrdan 1-yanvargacha har sessiyada bir marta modal ko'rsatadi.
   - Kartochkani PNG / JPG / PDF sifatida yuklab olish (html2canvas + jsPDF, cdnjs).
   - Ulashish (Web Share API yoki havolani nusxalash).
   - Bayram effekti (qor/uchqun).
   Kutubxonalar CSP ruxsat bergan cdnjs'dan faqat KERAK bo'lganda yuklanadi.
   ============================================================================= */
(function () {
    "use strict";

    if (window.YearReview) { return; } // ikki marta yuklansa qayta ishga tushmasin

    var H2C_SRC = "https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js";
    var JSPDF_SRC = "https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js";

    var scriptCache = {};

    function loadScript(src) {
        if (scriptCache[src]) { return scriptCache[src]; }
        scriptCache[src] = new Promise(function (resolve, reject) {
            var s = document.createElement("script");
            s.src = src;
            s.async = true;
            s.onload = resolve;
            s.onerror = function () { reject(new Error("yuklab bo'lmadi: " + src)); };
            document.head.appendChild(s);
        });
        return scriptCache[src];
    }

    function toast(msg) {
        if (typeof window.showToast === "function") { window.showToast(msg); }
    }

    // ── Bayram effekti ─────────────────────────────────────────────────────────
    function spawnFx(host, emojis) {
        if (!host) { return; }
        var pool = (emojis && emojis.length) ? emojis : ["❄️", "✨", "⭐"];
        var count = 26;
        for (var i = 0; i < count; i++) {
            var f = document.createElement("span");
            f.className = "yr-flake";
            f.textContent = pool[Math.floor(Math.random() * pool.length)];
            f.style.left = (Math.random() * 100) + "%";
            f.style.fontSize = (10 + Math.random() * 16) + "px";
            f.style.animationDuration = (5 + Math.random() * 6) + "s";
            f.style.animationDelay = (Math.random() * 6) + "s";
            host.appendChild(f);
        }
    }

    function emojisFromCard(card) {
        var out = [];
        card.querySelectorAll(".yr-corner").forEach(function (c) {
            var t = (c.textContent || "").trim();
            if (t) { out.push(t); }
        });
        return out;
    }

    // ── Yuklab olish ─────────────────────────────────────────────────────────────
    function fileName(card, ext) {
        var year = card.getAttribute("data-yr-year") || "";
        return "kitobdagimen-yillik-yakun-" + year + "." + ext;
    }

    function renderCanvas(card) {
        var target = card.querySelector("[data-yr-capture]") || card;
        return loadScript(H2C_SRC).then(function () {
            return window.html2canvas(target, {
                scale: 2,
                useCORS: true,
                backgroundColor: null,
                logging: false
            });
        });
    }

    function downloadDataUrl(dataUrl, name) {
        var a = document.createElement("a");
        a.href = dataUrl;
        a.download = name;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    }

    function withBusy(btn, fn) {
        if (btn) { btn.disabled = true; }
        return Promise.resolve()
            .then(fn)
            .catch(function (e) {
                toast("Yuklab olishda xatolik yuz berdi.");
                if (window.console) { console.error(e); }
            })
            .then(function () { if (btn) { btn.disabled = false; } });
    }

    function downloadImage(card, type, btn) {
        return withBusy(btn, function () {
            return renderCanvas(card).then(function (canvas) {
                if (type === "jpg") {
                    downloadDataUrl(canvas.toDataURL("image/jpeg", 0.95), fileName(card, "jpg"));
                } else {
                    downloadDataUrl(canvas.toDataURL("image/png"), fileName(card, "png"));
                }
            });
        });
    }

    function downloadPdf(card, btn) {
        return withBusy(btn, function () {
            return renderCanvas(card).then(function (canvas) {
                return loadScript(JSPDF_SRC).then(function () {
                    var jsPDFCtor = (window.jspdf && window.jspdf.jsPDF) || window.jsPDF;
                    if (!jsPDFCtor) { throw new Error("jsPDF topilmadi"); }
                    var w = canvas.width;
                    var h = canvas.height;
                    var pdf = new jsPDFCtor({
                        orientation: h >= w ? "portrait" : "landscape",
                        unit: "px",
                        format: [w, h]
                    });
                    pdf.addImage(canvas.toDataURL("image/jpeg", 0.95), "JPEG", 0, 0, w, h);
                    pdf.save(fileName(card, "pdf"));
                });
            });
        });
    }

    // ── Ulashish ─────────────────────────────────────────────────────────────────
    function shareCard(card) {
        var url = card.getAttribute("data-yr-share-url") || window.location.href;
        var name = card.getAttribute("data-yr-name") || "Kitobxon";
        var year = card.getAttribute("data-yr-year") || "";
        var title = name + " — " + year + "-yil kitob yakuni";
        var text = "Mening " + year + "-yilgi kitob yakunim — kitobdagimen.uz";

        if (navigator.share) {
            navigator.share({ title: title, text: text, url: url }).catch(function () { /* bekor qilindi */ });
            return;
        }
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(url).then(function () {
                toast("Havola nusxalandi!");
            }, function () {
                prompt("Ulashish havolasi:", url);
            });
            return;
        }
        prompt("Ulashish havolasi:", url);
    }

    // ── Kartochkani jonlantirish ──────────────────────────────────────────────────
    function enhanceCard(card) {
        if (!card || card.hasAttribute("data-yr-enhanced")) { return; }
        card.setAttribute("data-yr-enhanced", "1");

        spawnFx(card.querySelector("[data-yr-fx]"), emojisFromCard(card));

        card.querySelectorAll("[data-yr-download]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var type = btn.getAttribute("data-yr-download");
                if (type === "pdf") { downloadPdf(card, btn); }
                else { downloadImage(card, type, btn); }
            });
        });

        var shareBtn = card.querySelector("[data-yr-share]");
        if (shareBtn) {
            shareBtn.addEventListener("click", function () { shareCard(card); });
        }
    }

    function enhanceAll() {
        document.querySelectorAll(".yr-card").forEach(enhanceCard);
    }

    // ── Modal (auto-popup) ─────────────────────────────────────────────────────────
    function openModal(html) {
        var overlay = document.createElement("div");
        overlay.className = "yr-overlay";
        overlay.setAttribute("role", "dialog");
        overlay.setAttribute("aria-modal", "true");

        var closeBtn = document.createElement("button");
        closeBtn.type = "button";
        closeBtn.className = "yr-overlay-close";
        closeBtn.setAttribute("aria-label", "Yopish");
        closeBtn.innerHTML = "&times;";

        var wrap = document.createElement("div");
        wrap.className = "yr-modal-wrap";
        wrap.innerHTML = html;

        overlay.appendChild(closeBtn);
        overlay.appendChild(wrap);
        document.body.appendChild(overlay);
        document.body.style.overflow = "hidden";

        function close() {
            overlay.remove();
            document.body.style.overflow = "";
            document.removeEventListener("keydown", onKey);
        }
        function onKey(e) { if (e.key === "Escape") { close(); } }

        closeBtn.addEventListener("click", close);
        overlay.addEventListener("click", function (e) {
            if (e.target === overlay) { close(); }
        });
        document.addEventListener("keydown", onKey);

        var card = wrap.querySelector(".yr-card");
        if (card) { enhanceCard(card); }
    }

    function autoPopup() {
        // Ulashish/oldindan ko'rish sahifalarida modal chiqarmaymiz.
        if (window.location.pathname.indexOf("/yil-yakuni") === 0) { return; }

        var KEY = "kitob-yr-shown";
        try {
            if (sessionStorage.getItem(KEY)) { return; }
        } catch (e) { /* sessionStorage yo'q — davom etamiz */ }

        fetch("/yil-yakuni/card", { credentials: "include", headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (res) {
                if (res.status !== 200) { return null; }
                return res.text();
            })
            .then(function (html) {
                if (!html || !html.trim()) { return; }
                try { sessionStorage.setItem(KEY, "1"); } catch (e) { /* ignore */ }
                setTimeout(function () { openModal(html); }, 700);
            })
            .catch(function () { /* jimgina o'tkazamiz */ });
    }

    window.YearReview = { enhanceAll: enhanceAll, enhanceCard: enhanceCard, autoPopup: autoPopup, openModal: openModal };

    function init() {
        enhanceAll();
        var authed = document.body && document.body.getAttribute("data-authenticated") === "true";
        if (authed) { autoPopup(); }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
