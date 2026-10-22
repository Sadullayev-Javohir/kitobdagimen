/* =============================================================================
   KitobdaGimen.uz — Yillik Kitob Yakuni (Annual Reading Report)
   Mijoz skripti (Client-side Logic)
   - WYSIWYG 1080px poster layout & responsiv masshtablash (scale)
   - Faqat yuqori sifatli PNG (HD) va JPG formatlarda eksport qilish
   - Auto-popup modal va sinov (preview) boshqaruvi
   ============================================================================= */
(function () {
    "use strict";

    if (window.YearReview) { return; }

    var H2C_SRC = "https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js";
    var h2cPromise = null;

    function loadHtml2Canvas() {
        if (window.html2canvas) { return Promise.resolve(window.html2canvas); }
        if (h2cPromise) { return h2cPromise; }
        h2cPromise = new Promise(function (resolve, reject) {
            var s = document.createElement("script");
            s.src = H2C_SRC;
            s.async = true;
            s.onload = function () { resolve(window.html2canvas); };
            s.onerror = function () { reject(new Error("html2canvas yuklanmadi: " + H2C_SRC)); };
            document.head.appendChild(s);
        });
        return h2cPromise;
    }

    function toast(msg) {
        if (typeof window.showToast === "function") {
            window.showToast(msg);
        } else {
            // Zaxira toast
            var t = document.createElement("div");
            t.className = "toast-notification";
            t.textContent = msg;
            t.style.position = "fixed";
            t.style.bottom = "24px";
            t.style.left = "50%";
            t.style.transform = "translateX(-50%)";
            t.style.background = "#0F5132";
            t.style.color = "#FFFFFF";
            t.style.padding = "12px 24px";
            t.style.borderRadius = "9999px";
            t.style.fontSize = "14px";
            t.style.fontWeight = "600";
            t.style.zIndex = "9999";
            t.style.boxShadow = "0 10px 25px rgba(0,0,0,0.3)";
            document.body.appendChild(t);
            setTimeout(function () { t.remove(); }, 3500);
        }
    }

    // ── Fayl nomi ─────────────────────────────────────────────────────────────
    function fileName(card, ext) {
        var year = card.getAttribute("data-yr-year") || new Date().getFullYear();
        return "kitobdagimen-yillik-hisobot-" + year + "." + ext;
    }

    // ── Dizayn o'lchami: 1080px (Instagram Story / HD Poster) ─────────────────
    var DESIGN_W = 1080;
    var SAFE_CANVAS_AREA = 16000000; // iOS Safari kanvas chegarasi

    function computeExportScale(w, h) {
        var maxByArea = Math.sqrt(SAFE_CANVAS_AREA / (w * h));
        var scale = Math.min(3, Math.max(2, maxByArea));
        if (w * h * scale * scale > SAFE_CANVAS_AREA) {
            scale = maxByArea;
        }
        return scale;
    }

    // ── Poster'ni konteyner kengligiga moslab masshtablaydi ─────────────────────
    function layoutStage(card) {
        if (!card) { return; }
        var stage = card.querySelector("[data-yr-stage]");
        var poster = card.querySelector("[data-yr-capture]");
        if (!stage || !poster) { return; }

        var avail = stage.clientWidth;
        if (!avail) { return; }

        poster.style.width = DESIGN_W + "px";
        poster.style.transform = "none";
        var naturalH = poster.offsetHeight;

        var scale = Math.min(1, avail / DESIGN_W);
        poster.style.transform = "scale(" + scale + ")";
        stage.style.aspectRatio = "auto";
        stage.style.height = Math.round(naturalH * scale) + "px";
    }

    var _relayoutRaf = 0;
    function scheduleRelayout() {
        if (_relayoutRaf) { cancelAnimationFrame(_relayoutRaf); }
        _relayoutRaf = requestAnimationFrame(function () {
            _relayoutRaf = 0;
            document.querySelectorAll(".yr-card").forEach(layoutStage);
        });
    }

    // ── Shriftlar to'liq tayyor bo'lishini kutish ──────────────────────────────
    function ensureFonts() {
        if (!document.fonts || !document.fonts.load) { return Promise.resolve(); }
        return Promise.all([
            document.fonts.load('700 120px "Lora"'),
            document.fonts.load('700 56px "Lora"'),
            document.fonts.load('700 32px "Lora"'),
            document.fonts.load('600 20px "Plus Jakarta Sans"'),
            document.fonts.load('700 15px "Plus Jakarta Sans"'),
            document.fonts.ready
        ]).catch(function () { /* zaxira shriftda davom etiladi */ });
    }

    // ── Kanvas renderlash ─────────────────────────────────────────────────────
    function renderCanvas(card) {
        var poster = card.querySelector("[data-yr-capture]") || card;

        var saved = {
            position: poster.style.position,
            top: poster.style.top,
            left: poster.style.left,
            zIndex: poster.style.zIndex,
            transform: poster.style.transform,
            width: poster.style.width,
            maxWidth: poster.style.maxWidth
        };

        function restore() {
            card.classList.remove("yr-capturing");
            poster.style.position = saved.position;
            poster.style.top = saved.top;
            poster.style.left = saved.left;
            poster.style.zIndex = saved.zIndex;
            poster.style.transform = saved.transform;
            poster.style.width = saved.width;
            poster.style.maxWidth = saved.maxWidth;
            layoutStage(card);
        }

        return ensureFonts()
            .then(function () { return loadHtml2Canvas(); })
            .then(function (html2canvas) {
                card.classList.add("yr-capturing");

                // Tabiiy 1080px holatida o'rnatish
                poster.style.position = "fixed";
                poster.style.top = "0";
                poster.style.left = "0";
                poster.style.zIndex = "-2147483648";
                poster.style.transform = "none";
                poster.style.width = DESIGN_W + "px";
                poster.style.maxWidth = "none";
                void poster.offsetWidth;

                var naturalH = poster.offsetHeight || Math.round(DESIGN_W * 1.6);
                var exportScale = computeExportScale(DESIGN_W, naturalH);

                return html2canvas(poster, {
                    scale: exportScale,
                    useCORS: true,
                    allowTaint: true,
                    backgroundColor: null,
                    logging: false,
                    width: DESIGN_W,
                    height: naturalH,
                    windowWidth: DESIGN_W,
                    windowHeight: naturalH,
                    scrollX: 0,
                    scrollY: 0,
                    onclone: function (doc) {
                        var clonedCard = doc.querySelector(".yr-card");
                        if (clonedCard) { clonedCard.classList.add("yr-capturing"); }
                        var st = doc.querySelector("[data-yr-stage]");
                        if (st) {
                            st.style.height = "auto";
                            st.style.overflow = "visible";
                            st.style.aspectRatio = "auto";
                        }
                        var node = doc.querySelector("[data-yr-capture]");
                        if (node) {
                            node.style.position = "static";
                            node.style.width = DESIGN_W + "px";
                            node.style.maxWidth = "none";
                            node.style.margin = "0";
                            node.style.transform = "none";
                        }
                    }
                });
            })
            .then(function (canvas) {
                restore();
                return canvas;
            })
            .catch(function (e) {
                restore();
                throw e;
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

    // JPG eksporti uchun to'q fon bilan kompozitlash (shaffoflik qora bo'lib qolmasligi uchun)
    function compositeJpg(canvas) {
        var out = document.createElement("canvas");
        out.width = canvas.width;
        out.height = canvas.height;
        var ctx = out.getContext("2d");
        ctx.fillStyle = "#080D15";
        ctx.fillRect(0, 0, out.width, out.height);
        ctx.drawImage(canvas, 0, 0);
        return out.toDataURL("image/jpeg", 0.95);
    }

    function downloadImage(card, type, btn) {
        if (!card) { return; }
        var origText = "";
        var labelEl = btn ? btn.querySelector(".yr-btn-text") : null;

        if (btn) {
            btn.disabled = true;
            if (labelEl) {
                origText = labelEl.textContent;
                labelEl.textContent = "Tayyorlanmoqda...";
            }
        }

        toast(type.toUpperCase() + " tasvir yaratilmoqda, kuting...");

        renderCanvas(card)
            .then(function (canvas) {
                if (type === "jpg") {
                    var jpgData = compositeJpg(canvas);
                    downloadDataUrl(jpgData, fileName(card, "jpg"));
                    toast("JPG formatda muvaffaqiyatli yuklab olindi!");
                } else {
                    var pngData = canvas.toDataURL("image/png");
                    downloadDataUrl(pngData, fileName(card, "png"));
                    toast("PNG (HD) formatda muvaffaqiyatli yuklab olindi!");
                }
            })
            .catch(function (err) {
                if (window.console) { console.error("Yuklab olishda xatolik:", err); }
                toast("Rasmni yaratishda xatolik yuz berdi.");
            })
            .finally(function () {
                if (btn) {
                    btn.disabled = false;
                    if (labelEl && origText) {
                        labelEl.textContent = origText;
                    }
                }
            });
    }

    // ── Kartochkani faollashtirish ─────────────────────────────────────────────
    function enhanceCard(card) {
        if (!card || card.hasAttribute("data-yr-enhanced")) { return; }
        card.setAttribute("data-yr-enhanced", "1");

        layoutStage(card);
        if (document.fonts && document.fonts.ready) {
            document.fonts.ready.then(function () {
                layoutStage(card);
            }).catch(function () {});
        }

        // Faqat PNG va JPG yuklab olish tugmalari
        card.querySelectorAll("[data-yr-download]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var type = btn.getAttribute("data-yr-download");
                if (type === "png" || type === "jpg") {
                    downloadImage(card, type, btn);
                }
            });
        });
    }

    function enhanceAll() {
        document.querySelectorAll(".yr-card").forEach(enhanceCard);
    }

    // ── Modal oynasi ─────────────────────────────────────────────────────────
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
        if (window.location.pathname.indexOf("/yil-yakuni") === 0) { return; }

        var KEY = "kitob-yr-shown";
        try {
            if (sessionStorage.getItem(KEY)) { return; }
        } catch (e) { /* ignore */ }

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
            .catch(function () {});
    }

    window.YearReview = {
        enhanceAll: enhanceAll,
        enhanceCard: enhanceCard,
        autoPopup: autoPopup,
        openModal: openModal,
        downloadImage: downloadImage
    };

    function init() {
        enhanceAll();
        window.addEventListener("resize", scheduleRelayout);
        window.addEventListener("orientationchange", scheduleRelayout);
        var authed = document.body && document.body.getAttribute("data-authenticated") === "true";
        if (authed) { autoPopup(); }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
