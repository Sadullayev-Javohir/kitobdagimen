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

    // ── Kitob qidirish va yangi yaratish (Challenge Admin) ──────────────────────────
    let activeTargetContext = null;

    const newBookModal = document.getElementById("chCreateBookModal");
    const nbTitle = document.getElementById("chNewBookTitle");
    const nbAuthor = document.getElementById("chNewBookAuthor");
    const nbPages = document.getElementById("chNewBookPages");
    const nbError = document.getElementById("chNewBookError");
    const nbSaveBtn = document.getElementById("chSaveNewBookBtn");

    function openNewBookModal(context, initialTitle) {
        activeTargetContext = context;
        if (nbTitle) nbTitle.value = initialTitle || "";
        if (nbAuthor) nbAuthor.value = "";
        if (nbPages) nbPages.value = "200";
        if (nbError) { nbError.textContent = ""; nbError.style.display = "none"; }
        if (newBookModal) {
            newBookModal.style.display = "flex";
            newBookModal.hidden = false;
        }
        if (nbTitle) setTimeout(function () { nbTitle.focus(); }, 100);
    }

    function closeNewBookModal() {
        if (newBookModal) {
            newBookModal.style.display = "none";
            newBookModal.hidden = true;
        }
        activeTargetContext = null;
    }

    if (document.getElementById("chCloseNewBookModal")) {
        document.getElementById("chCloseNewBookModal").addEventListener("click", closeNewBookModal);
    }
    if (document.getElementById("chCancelNewBookModal")) {
        document.getElementById("chCancelNewBookModal").addEventListener("click", closeNewBookModal);
    }

    if (nbSaveBtn) {
        nbSaveBtn.addEventListener("click", async function () {
            const title = (nbTitle ? nbTitle.value : "").trim();
            const author = (nbAuthor ? nbAuthor.value : "").trim();
            const pages = parseInt(nbPages ? nbPages.value : "200", 10) || 200;

            if (!title) {
                showNbError("Kitob nomini kiriting.");
                return;
            }
            if (!author) {
                showNbError("Kitob muallifini kiriting.");
                return;
            }

            nbSaveBtn.disabled = true;
            nbSaveBtn.textContent = "Saqlanmoqda...";

            try {
                const res = await fetch("/books/create", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "X-Requested-With": "XMLHttpRequest"
                    },
                    body: JSON.stringify({
                        title: title,
                        author: author,
                        totalPages: pages,
                        genreId: 1
                    })
                });

                if (!res.ok) {
                    throw new Error("Kitob yaratishda xatolik yuz berdi.");
                }

                const createdBook = await res.json();
                if (activeTargetContext) {
                    applySelectedBook(activeTargetContext, createdBook);
                }
                closeNewBookModal();
            } catch (err) {
                showNbError(err.message || "Kitob yaratishda xatolik yuz berdi.");
            } finally {
                nbSaveBtn.disabled = false;
                nbSaveBtn.innerHTML = '<span class="material-symbols-outlined ic-16">save</span> Saqlash va biriktirish';
            }
        });
    }

    function showNbError(msg) {
        if (nbError) {
            nbError.textContent = msg;
            nbError.style.display = "block";
        }
    }

    function applySelectedBook(ctx, book) {
        if (!ctx || !book) return;
        if (ctx.titleInput) ctx.titleInput.value = book.title || "";
        if (ctx.authorInput) ctx.authorInput.value = book.author || "";
        if (ctx.coverInput && book.coverUrl) ctx.coverInput.value = book.coverUrl;

        if (ctx.previewImg && book.coverUrl) {
            ctx.previewImg.src = book.coverUrl;
            if (ctx.previewContainer) ctx.previewContainer.style.display = "flex";
        }
    }

    function setupBookAutocomplete(titleInput, authorInput, coverInput, suggestionsBox, previewImg, previewContainer, context) {
        if (!titleInput || !suggestionsBox) return;

        let searchTimer = null;

        titleInput.addEventListener("input", function () {
            const q = titleInput.value.trim();
            if (q.length < 2) {
                suggestionsBox.hidden = true;
                suggestionsBox.innerHTML = "";
                return;
            }

            clearTimeout(searchTimer);
            searchTimer = setTimeout(async function () {
                try {
                    const res = await fetch("/books/search?q=" + encodeURIComponent(q), {
                        headers: { "X-Requested-With": "XMLHttpRequest" }
                    });
                    if (!res.ok) return;
                    const books = await res.json();

                    suggestionsBox.innerHTML = "";

                    if (!books || books.length === 0) {
                        const emptyDiv = document.createElement("div");
                        emptyDiv.className = "ch-book-sug-empty";
                        emptyDiv.innerHTML =
                            '<div>⚠️ Bazada <b>«' + esc(q) + '»</b> kitobi topilmadi.</div>' +
                            '<button type="button" class="btn btn-primary btn-xs ch-book-sug-new-btn">' +
                            '<span class="material-symbols-outlined ic-14">add</span> Yangi kitob yaratish</button>';
                        emptyDiv.querySelector(".ch-book-sug-new-btn").addEventListener("click", function (e) {
                            e.preventDefault();
                            suggestionsBox.hidden = true;
                            openNewBookModal(context, q);
                        });
                        suggestionsBox.appendChild(emptyDiv);
                    } else {
                        books.forEach(function (b) {
                            const item = document.createElement("div");
                            item.className = "ch-book-sug-item";
                            const cover = b.coverUrl || "/img/default-book-cover.webp";
                            item.innerHTML =
                                '<img class="ch-book-sug-cover" src="' + esc(cover) + '" alt="" />' +
                                '<div class="ch-book-sug-info">' +
                                '<div class="ch-book-sug-title">' + esc(b.title) + '</div>' +
                                '<div class="ch-book-sug-author">' + esc(b.author || "") + '</div>' +
                                '</div>';
                            item.addEventListener("click", function () {
                                applySelectedBook(context, b);
                                suggestionsBox.hidden = true;
                            });
                            suggestionsBox.appendChild(item);
                        });

                        const footerDiv = document.createElement("div");
                        footerDiv.style.cssText = "padding:8px 12px; border-top:1px dashed var(--border); text-align:center;";
                        footerDiv.innerHTML =
                            '<button type="button" class="btn btn-ghost btn-xs" style="color:var(--primary); font-size:12px;">' +
                            '<span class="material-symbols-outlined ic-14">add</span> Ro\'yxatda yo\'qmi? Yangi yaratish</button>';
                        footerDiv.querySelector("button").addEventListener("click", function (e) {
                            e.preventDefault();
                            suggestionsBox.hidden = true;
                            openNewBookModal(context, q);
                        });
                        suggestionsBox.appendChild(footerDiv);
                    }

                    suggestionsBox.hidden = false;
                } catch (e) {
                    // xato bo'lsa jim o'tkazamiz
                }
            }, 250);
        });

        document.addEventListener("click", function (e) {
            if (!titleInput.contains(e.target) && !suggestionsBox.contains(e.target)) {
                suggestionsBox.hidden = true;
            }
        });
    }

    // 1-, 2-, 3-o'rin sovg'a kitoblari uchun ulash
    [1, 2, 3].forEach(function (rank) {
        const titleInput = document.getElementById("prizeTitle_" + rank);
        const authorInput = document.getElementById("prizeAuthor_" + rank);
        const coverInput = document.getElementById("prizeCoverUrl_" + rank);
        const suggestionsBox = document.getElementById("prizeSuggestions_" + rank);
        const card = titleInput ? titleInput.closest(".ch-prize-card") : null;
        const previewImg = card ? card.querySelector(".ch-prize-cover-img") : null;
        const previewContainer = card ? card.querySelector(".ch-prize-cover-preview") : null;

        const context = {
            type: "prize",
            rank: rank,
            titleInput: titleInput,
            authorInput: authorInput,
            coverInput: coverInput,
            previewImg: previewImg,
            previewContainer: previewContainer
        };

        setupBookAutocomplete(titleInput, authorInput, coverInput, suggestionsBox, previewImg, previewContainer, context);

        const newBtn = card ? card.querySelector('.ch-create-book-btn[data-target-rank="' + rank + '"]') : null;
        if (newBtn) {
            newBtn.addEventListener("click", function (e) {
                e.preventDefault();
                openNewBookModal(context, titleInput ? titleInput.value.trim() : "");
            });
        }
    });

    // Har bir g'olibga sovg'a berish formalari uchun ulash
    document.querySelectorAll(".ch-gift-form").forEach(function (form) {
        const titleInput = form.querySelector(".ch-gift-search-input");
        const authorInput = form.querySelector(".ch-gift-author-input");
        const coverInput = form.querySelector('input[name="coverUrl"]');
        const suggestionsBox = form.querySelector(".ch-book-suggestions");
        const rankBtn = form.querySelector(".ch-create-book-gift-btn");

        const context = {
            type: "gift",
            titleInput: titleInput,
            authorInput: authorInput,
            coverInput: coverInput,
            previewImg: null,
            previewContainer: null
        };

        setupBookAutocomplete(titleInput, authorInput, coverInput, suggestionsBox, null, null, context);

        if (rankBtn) {
            rankBtn.addEventListener("click", function (e) {
                e.preventDefault();
                openNewBookModal(context, titleInput ? titleInput.value.trim() : "");
            });
        }
    });
})();
