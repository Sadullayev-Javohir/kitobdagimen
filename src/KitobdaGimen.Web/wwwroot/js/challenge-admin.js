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

    // E'lon qilish oynasi (g'oliblarga sovg'a biriktirish + e'lon)
    const announceModal = document.querySelector("[data-announce-modal]");
    function openAnnounce() {
        if (announceModal) { announceModal.hidden = false; document.body.style.overflow = "hidden"; }
    }
    function closeAnnounce() {
        if (announceModal) { announceModal.hidden = true; document.body.style.overflow = ""; }
    }
    document.querySelectorAll("[data-announce-open]").forEach(function (b) {
        b.addEventListener("click", openAnnounce);
    });
    document.querySelectorAll("[data-announce-close]").forEach(function (b) {
        b.addEventListener("click", closeAnnounce);
    });
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") { closeAnnounce(); }
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

    // Custom File Uploader boshqaruvi (muqova rasmlarini yuklash, preview va bekor qilish)
    function initFileUploader(uploader) {
        var input = uploader.querySelector(".ch-file-uploader-input");
        var zone = uploader.querySelector(".ch-file-uploader-zone");
        var bar = uploader.querySelector(".ch-file-uploader-preview-bar");
        var thumb = uploader.querySelector(".ch-file-uploader-thumb");
        var nameEl = uploader.querySelector(".ch-file-uploader-name");
        var sizeEl = uploader.querySelector(".ch-file-uploader-size");
        var clearBtn = uploader.querySelector(".ch-file-uploader-clear");

        if (!input || !zone || !bar) { return; }

        function formatSize(bytes) {
            if (bytes == null || isNaN(bytes)) return "";
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + " KB";
            return (bytes / (1024 * 1024)).toFixed(1) + " MB";
        }

        function showFile(file) {
            if (!file) {
                clearFile();
                return;
            }
            if (nameEl) { nameEl.textContent = file.name; }
            if (sizeEl) { sizeEl.textContent = formatSize(file.size); }

            if (file.type && file.type.indexOf("image/") === 0) {
                var reader = new FileReader();
                reader.onload = function (e) {
                    if (thumb) {
                        thumb.src = e.target.result;
                        thumb.style.display = "";
                    }
                };
                reader.readAsDataURL(file);
            } else if (thumb) {
                thumb.style.display = "none";
            }

            zone.hidden = true;
            bar.hidden = false;
            uploader.classList.add("has-file");
        }

        function clearFile() {
            input.value = "";
            if (thumb) { thumb.src = ""; thumb.style.display = ""; }
            if (nameEl) { nameEl.textContent = ""; }
            if (sizeEl) { sizeEl.textContent = ""; }
            bar.hidden = true;
            zone.hidden = false;
            uploader.classList.remove("has-file");
        }

        input.addEventListener("change", function () {
            if (input.files && input.files[0]) {
                showFile(input.files[0]);
            } else {
                clearFile();
            }
        });

        if (clearBtn) {
            clearBtn.addEventListener("click", function (e) {
                e.preventDefault();
                e.stopPropagation();
                clearFile();
            });
        }

        zone.addEventListener("dragover", function (e) {
            e.preventDefault();
            uploader.classList.add("is-dragging");
        });
        zone.addEventListener("dragleave", function () {
            uploader.classList.remove("is-dragging");
        });
        zone.addEventListener("drop", function (e) {
            e.preventDefault();
            uploader.classList.remove("is-dragging");
            if (e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files[0]) {
                input.files = e.dataTransfer.files;
                showFile(e.dataTransfer.files[0]);
            }
        });
    }

    document.querySelectorAll("[data-ch-uploader]").forEach(initFileUploader);
})();
