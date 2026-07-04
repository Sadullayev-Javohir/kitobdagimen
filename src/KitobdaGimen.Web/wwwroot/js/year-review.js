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

    // ── Bayram effekti (yog'ayotgan qor) ───────────────────────────────────────
    function spawnFx(host) {
        if (!host) { return; }
        // Emoji/glif ishlatmaymiz — sof CSS "qor" nuqtalari (yumaloq, yumshoq porlash).
        var count = 34;
        for (var i = 0; i < count; i++) {
            var f = document.createElement("span");
            f.className = "yr-flake";
            var size = (4 + Math.random() * 6).toFixed(1);
            var op = (0.4 + Math.random() * 0.5).toFixed(2);
            f.style.left = (Math.random() * 100) + "%";
            f.style.width = size + "px";
            f.style.height = size + "px";
            f.style.background = "rgba(255,255,255," + op + ")";
            f.style.boxShadow = "0 0 6px rgba(255,255,255," + op + ")";
            f.style.animationDuration = (5 + Math.random() * 7) + "s";
            f.style.animationDelay = (Math.random() * 7) + "s";
            host.appendChild(f);
        }
    }

    // ── Yuklab olish ─────────────────────────────────────────────────────────────
    function fileName(card, ext) {
        var year = card.getAttribute("data-yr-year") || "";
        return "kitobdagimen-yillik-yakun-" + year + "." + ext;
    }

    // Qat'iy dizayn kengligi — poster shu kenglikda tuziladi va shu kenglikda eksport qilinadi.
    var DESIGN_W = 1080;

    // HD eksport masshtabi. Iloji boricha yuqori sifat (>= 2160px keng), ammo brauzer
    // kanvas chegarasida xavfsiz. iOS/Safari umumiy kanvas yuzasini ~16.7M px (4096²) bilan
    // cheklaydi — undan oshsa bo'sh (qora/shaffof) rasm chiqadi. Shuning uchun poster
    // yuzasiga qarab eng yuqori xavfsiz masshtabni hisoblaymiz.
    var SAFE_CANVAS_AREA = 16000000;   // ~iOS chegarasidan bir oz past (xavfsizlik uchun)
    function computeExportScale(w, h) {
        var maxByArea = Math.sqrt(SAFE_CANVAS_AREA / (w * h));
        // Eng kami 2× (2160px keng — Full HD dan yuqori), eng ko'pi 3× (o'ta tiniq).
        var scale = Math.min(3, Math.max(2, maxByArea));
        // Yuza chegarasidan oshib ketmasligini kafolatlaymiz.
        if (w * h * scale * scale > SAFE_CANVAS_AREA) { scale = maxByArea; }
        return scale;
    }

    // Poster'ni konteyner (.yr-stage) kengligiga moslab masshtablaydi.
    // Bu WYSIWYG asosi: ekranda ko'ringan narsa aynan yuklab olinadi.
    function layoutStage(card) {
        if (!card) { return; }
        var stage = card.querySelector("[data-yr-stage]");
        var poster = card.querySelector("[data-yr-capture]");
        if (!stage || !poster) { return; }

        var avail = stage.clientWidth;
        if (!avail) { return; }                     // hali ko'rinmayapti — keyin qayta chaqiriladi

        poster.style.width = DESIGN_W + "px";
        poster.style.transform = "none";            // tabiiy balandlikni o'lchash uchun
        var naturalH = poster.offsetHeight;

        var scale = Math.min(1, avail / DESIGN_W);  // katta ekranda 1:1, kichikda kichraytiriladi
        poster.style.transform = "scale(" + scale + ")";
        stage.style.aspectRatio = "auto";           // endi aniq balandlik beramiz
        stage.style.height = Math.round(naturalH * scale) + "px";
    }

    // Oyna o'lchami o'zgarganda barcha kartochkalarni qayta masshtablaymiz (debounce).
    var _relayoutRaf = 0;
    function scheduleRelayout() {
        if (_relayoutRaf) { cancelAnimationFrame(_relayoutRaf); }
        _relayoutRaf = requestAnimationFrame(function () {
            _relayoutRaf = 0;
            document.querySelectorAll(".yr-card").forEach(layoutStage);
        });
    }

    // Eksportdan oldin poster shriftlarini kafolatli yuklaymiz. Aks holda html2canvas
    // zaxira shriftda (boshqa metrik bilan) chizib, matnni siljitib/ustma-ust tushirib yuboradi.
    function ensureFonts() {
        if (!document.fonts || !document.fonts.load) { return Promise.resolve(); }
        return Promise.all([
            document.fonts.load('700 158px "Lora"'),
            document.fonts.load('700 104px "Lora"'),
            document.fonts.load('700 54px "Lora"'),
            document.fonts.load('400 19px "Source Sans 3"'),
            document.fonts.load('600 19px "Source Sans 3"'),
            document.fonts.load('700 19px "Source Sans 3"')
        ]).catch(function () { /* zaxira shrift bilan davom etamiz */ });
    }

    function renderCanvas(card) {
        var poster = card.querySelector("[data-yr-capture]") || card;

        // Eksport uchun poster'ni HAQIQIY tabiiy o'lchamiga qo'yamiz: 1080px, transformasiz,
        // ekrandan tashqarida (position:fixed, ko'rinmas). Sabab: ekranda poster
        // transform:scale(...) bilan kichraytirilgan; agar html2canvas o'sha masshtablangan
        // elementni o'lchasa — matn/konteynerlarni siljitib, ma'lumotlarni tashqariga
        // chiqarib yuboradi. Tabiiy 1080px'da esa joylashuv aniq va barqaror bo'ladi.
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
            layoutStage(card);   // ekrandagi masshtabni tiklaymiz
        }

        return ensureFonts()
            .then(function () {
                // Shriftlar TO'LIQ yuklanguncha kutamiz. Aks holda html2canvas zaxira
                // shriftda (boshqa kenglikda) chizib, so'zlarni konteynerdan chiqaradi.
                return (document.fonts && document.fonts.ready) ? document.fonts.ready : null;
            })
            .then(function () { return loadScript(H2C_SRC); })
            .then(function () {
                card.classList.add("yr-capturing");   // ekran-effektlarini (qor/Qorbobo) yashiradi
                // Poster'ni ekrandan tashqarida, tabiiy 1080px'da joylashtiramiz (miltillamaydi).
                poster.style.position = "fixed";
                poster.style.top = "0";
                poster.style.left = "0";
                poster.style.zIndex = "-2147483648";
                poster.style.transform = "none";
                poster.style.width = DESIGN_W + "px";
                poster.style.maxWidth = "none";
                void poster.offsetWidth;               // reflow — aniq o'lchamlar

                var naturalH = poster.offsetHeight || Math.round(DESIGN_W * 1.5);
                var exportScale = computeExportScale(DESIGN_W, naturalH);

                return window.html2canvas(poster, {
                    scale: exportScale,                // HD: iloji boricha yuqori, iOS chegarasida xavfsiz
                    useCORS: true,
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
                        doc.querySelectorAll("[data-yr-fx]").forEach(function (fx) {
                            fx.style.display = "none";
                        });
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
            .then(function (canvas) { restore(); return canvas; })
            .catch(function (e) { restore(); throw e; });
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
    function canvasToBlob(canvas, type, quality) {
        return new Promise(function (resolve) {
            if (canvas.toBlob) {
                canvas.toBlob(function (b) { resolve(b); }, type, quality);
            } else {
                // Juda eski brauzer — dataURL orqali blob yasaymiz.
                try {
                    var dataUrl = canvas.toDataURL(type, quality);
                    var parts = dataUrl.split(",");
                    var bin = atob(parts[1]);
                    var arr = new Uint8Array(bin.length);
                    for (var i = 0; i < bin.length; i++) { arr[i] = bin.charCodeAt(i); }
                    resolve(new Blob([arr], { type: type }));
                } catch (e) { resolve(null); }
            }
        });
    }

    function shareMeta(card) {
        var url = card.getAttribute("data-yr-share-url") || window.location.href;
        var name = card.getAttribute("data-yr-name") || "Kitobxon";
        var year = card.getAttribute("data-yr-year") || "";
        return {
            url: url,
            title: name + " — " + year + "-yil kitob yakuni",
            text: "Mening " + year + "-yilgi kitob yakunim — kitobdagimen.uz"
        };
    }

    function shareLinkFallback(meta) {
        if (navigator.share) {
            navigator.share({ title: meta.title, text: meta.text, url: meta.url }).catch(function () { /* bekor */ });
            return;
        }
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(meta.url).then(function () {
                toast("Havola nusxalandi!");
            }, function () {
                prompt("Ulashish havolasi:", meta.url);
            });
            return;
        }
        prompt("Ulashish havolasi:", meta.url);
    }

    // HD rasmni fayl sifatida ulashamiz (Web Share Level 2). Qo'llab-quvvatlanmasa —
    // havola bilan (yoki havolani nusxalash bilan) chekinamiz.
    function shareCard(card, btn) {
        var meta = shareMeta(card);

        // Fayl bilan ulashish imkoni bormi (mobil brauzerlar odatda qo'llab-quvvatlaydi)?
        var supportsFileShare = !!(navigator.canShare && navigator.share);

        if (!supportsFileShare) {
            shareLinkFallback(meta);
            return Promise.resolve();
        }

        return withBusy(btn, function () {
            return renderCanvas(card)
                .then(function (canvas) { return canvasToBlob(canvas, "image/png"); })
                .then(function (blob) {
                    if (!blob) { shareLinkFallback(meta); return; }
                    var file = new File([blob], fileName(card, "png"), { type: "image/png" });
                    if (navigator.canShare && navigator.canShare({ files: [file] })) {
                        return navigator.share({
                            files: [file],
                            title: meta.title,
                            text: meta.text + " " + meta.url
                        }).catch(function (e) {
                            // Foydalanuvchi bekor qilgan bo'lishi mumkin — jimgina o'tamiz.
                            if (e && e.name !== "AbortError") { shareLinkFallback(meta); }
                        });
                    }
                    // Fayl ulashish yo'q — havola bilan.
                    shareLinkFallback(meta);
                })
                .catch(function () { shareLinkFallback(meta); });
        });
    }

    // ── Kartochkani jonlantirish ──────────────────────────────────────────────────
    function enhanceCard(card) {
        if (!card || card.hasAttribute("data-yr-enhanced")) { return; }
        card.setAttribute("data-yr-enhanced", "1");

        // Poster'ni konteynerga moslab masshtablaymiz (WYSIWYG). Shriftlar yuklangach
        // matn balandligi o'zgaradi — shuning uchun qayta hisoblaymiz.
        layoutStage(card);
        if (document.fonts && document.fonts.ready) {
            document.fonts.ready.then(function () { layoutStage(card); }).catch(function () {});
        }

        spawnFx(card.querySelector("[data-yr-fx]"));

        // Uchayotgan Qorbobo (Three.js ekran-effekti) — poster tashqarisida, rasmga tushmaydi.
        // Three.js/WebGL bo'lmasa jimgina chekinadi; SVG chana CSS'da uchishda davom etadi.
        var threeHost = card.querySelector("[data-yr-three]");
        if (threeHost && window.YearReviewScene) {
            try { window.YearReviewScene.mount(threeHost); } catch (e) { /* zaxira SVG uchadi */ }
        }

        card.querySelectorAll("[data-yr-download]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var type = btn.getAttribute("data-yr-download");
                if (type === "pdf") { downloadPdf(card, btn); }
                else { downloadImage(card, type, btn); }
            });
        });

        var shareBtn = card.querySelector("[data-yr-share]");
        if (shareBtn) {
            shareBtn.addEventListener("click", function () { shareCard(card, shareBtn); });
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
