/* =============================================================================
   Yillik Kitob Yakuni — uchayotgan Qorbobo (Santa) + bug'ular, Three.js ekran-effekti.
   • Poster (rasmga tushadigan hudud) DAN TASHQARIDA joylashadi — shu sababli PNG/JPG/PDF
     eksportiga TUSHMAYDI. Bu shunchaki jonli ekran bezagi.
   • Three.js CSP ruxsat bergan cdnjs'dan (r128 UMD, global THREE) yuklanadi.
   • WebGL yo'q / yuklanmasa — jimgina chekinadi (Razor'dagi SVG chana CSS'da uchadi).
   • Kartochka DOM'dan olib tashlansa (modal yopilsa) — resurslar tozalanadi.
   ============================================================================= */
(function () {
    "use strict";

    if (window.YearReviewScene) { return; }

    var THREE_SRC = "https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js";
    var threePromise = null;

    function loadThree() {
        if (window.THREE) { return Promise.resolve(window.THREE); }
        if (threePromise) { return threePromise; }
        threePromise = new Promise(function (resolve, reject) {
            var s = document.createElement("script");
            s.src = THREE_SRC;
            s.async = true;
            s.onload = function () { window.THREE ? resolve(window.THREE) : reject(new Error("THREE topilmadi")); };
            s.onerror = function () { reject(new Error("three.js yuklanmadi")); };
            document.head.appendChild(s);
        });
        return threePromise;
    }

    function webglOk() {
        try {
            var c = document.createElement("canvas");
            return !!(window.WebGLRenderingContext &&
                (c.getContext("webgl") || c.getContext("experimental-webgl")));
        } catch (e) { return false; }
    }

    // ── Model bo'laklari (stilizatsiya, primitivlardan) ─────────────────────────────
    function makeReindeer(THREE, lead) {
        var g = new THREE.Group();
        var hide = new THREE.MeshStandardMaterial({ color: lead ? 0x8a5a2b : 0x74491f, roughness: 0.85, metalness: 0.05 });
        var dark = new THREE.MeshStandardMaterial({ color: 0x4a2d13, roughness: 0.9 });

        var body = new THREE.Mesh(new THREE.BoxGeometry(1.5, 0.72, 0.62), hide);
        body.position.y = 0.9;
        g.add(body);

        var neck = new THREE.Mesh(new THREE.BoxGeometry(0.42, 0.6, 0.42), hide);
        neck.position.set(0.82, 1.18, 0);
        neck.rotation.z = -0.5;
        g.add(neck);

        var head = new THREE.Mesh(new THREE.BoxGeometry(0.62, 0.44, 0.44), hide);
        head.position.set(1.12, 1.42, 0);
        g.add(head);

        var snout = new THREE.Mesh(new THREE.BoxGeometry(0.3, 0.26, 0.3), hide);
        snout.position.set(1.42, 1.34, 0);
        g.add(snout);

        // Rudolf — yetakchida qizil burun
        var noseMat = new THREE.MeshStandardMaterial({
            color: lead ? 0xff3b30 : 0x2a1a0c,
            emissive: lead ? 0xff2a20 : 0x000000,
            emissiveIntensity: lead ? 0.7 : 0
        });
        var nose = new THREE.Mesh(new THREE.SphereGeometry(0.13, 12, 12), noseMat);
        nose.position.set(1.58, 1.32, 0);
        g.add(nose);

        // Shoxlar
        [-1, 1].forEach(function (s) {
            var a1 = new THREE.Mesh(new THREE.CylinderGeometry(0.03, 0.05, 0.5, 6), dark);
            a1.position.set(1.05, 1.78, s * 0.16);
            a1.rotation.z = s * 0.3;
            g.add(a1);
            var a2 = new THREE.Mesh(new THREE.CylinderGeometry(0.025, 0.03, 0.28, 6), dark);
            a2.position.set(1.16, 2.0, s * 0.22);
            a2.rotation.z = s * 0.9;
            g.add(a2);
        });

        // Oyoqlar (yugurish uchun animatsiya qilinadi)
        var legs = [];
        [[-0.5, 0.28], [-0.5, -0.28], [0.5, 0.28], [0.5, -0.28]].forEach(function (p) {
            var leg = new THREE.Mesh(new THREE.BoxGeometry(0.17, 0.62, 0.17), dark);
            leg.geometry.translate(0, -0.31, 0);   // menteşe tepada bo'lsin
            leg.position.set(p[0], 0.58, p[1]);
            g.add(leg);
            legs.push(leg);
        });
        g.userData.legs = legs;

        var tail = new THREE.Mesh(new THREE.SphereGeometry(0.12, 8, 8), new THREE.MeshStandardMaterial({ color: 0xf2ead6 }));
        tail.position.set(-0.78, 1.0, 0);
        g.add(tail);

        return g;
    }

    function makeSleighWithSanta(THREE) {
        var g = new THREE.Group();

        // Chana korpusi (yon profil → ekstruziya)
        var shape = new THREE.Shape();
        shape.moveTo(0, 0);
        shape.lineTo(2.6, 0);
        shape.quadraticCurveTo(3.25, 0.1, 3.1, 0.95);
        shape.quadraticCurveTo(3.05, 1.5, 2.4, 1.5);
        shape.lineTo(0.7, 1.5);
        shape.lineTo(0.5, 2.15);
        shape.lineTo(0, 2.15);
        shape.lineTo(0, 0);
        var geo = new THREE.ExtrudeGeometry(shape, { depth: 1.25, bevelEnabled: true, bevelThickness: 0.06, bevelSize: 0.06, bevelSegments: 2 });
        geo.translate(0, 0, -0.625);
        var red = new THREE.MeshStandardMaterial({ color: 0xc0392b, roughness: 0.55, metalness: 0.15 });
        var sleigh = new THREE.Mesh(geo, red);
        sleigh.position.y = 0.55;
        g.add(sleigh);

        // Oltin shoxli chang'i (runner)
        var gold = new THREE.MeshStandardMaterial({ color: 0xe0b44a, roughness: 0.35, metalness: 0.6 });
        [-0.62, 0.62].forEach(function (z) {
            var runner = new THREE.Mesh(new THREE.BoxGeometry(3.2, 0.12, 0.12), gold);
            runner.position.set(1.4, 0.42, z);
            g.add(runner);
            var curl = new THREE.Mesh(new THREE.TorusGeometry(0.32, 0.06, 8, 16, Math.PI * 1.3), gold);
            curl.position.set(3.0, 0.62, z);
            curl.rotation.z = -0.6;
            g.add(curl);
        });

        // Sovg'alar qopi
        var sack = new THREE.Mesh(new THREE.SphereGeometry(0.62, 16, 16), new THREE.MeshStandardMaterial({ color: 0x8a5a2b, roughness: 0.8 }));
        sack.position.set(0.35, 1.55, 0);
        sack.scale.set(1, 1.15, 1);
        g.add(sack);
        var gift = new THREE.Mesh(new THREE.BoxGeometry(0.4, 0.4, 0.4), new THREE.MeshStandardMaterial({ color: 0x2e8b57 }));
        gift.position.set(0.95, 1.75, 0.2);
        gift.rotation.y = 0.4;
        g.add(gift);

        // ── Qorbobo ──
        var santa = new THREE.Group();
        var suit = new THREE.MeshStandardMaterial({ color: 0xd0392b, roughness: 0.6 });
        var skin = new THREE.MeshStandardMaterial({ color: 0xffd9b3, roughness: 0.7 });
        var white = new THREE.MeshStandardMaterial({ color: 0xfbfbfb, roughness: 0.8 });

        var torso = new THREE.Mesh(new THREE.CylinderGeometry(0.42, 0.55, 1.0, 16), suit);
        torso.position.y = 1.55;
        santa.add(torso);
        var belt = new THREE.Mesh(new THREE.CylinderGeometry(0.57, 0.57, 0.2, 16), new THREE.MeshStandardMaterial({ color: 0x2a2a2a }));
        belt.position.y = 1.2;
        santa.add(belt);
        var head = new THREE.Mesh(new THREE.SphereGeometry(0.32, 16, 16), skin);
        head.position.y = 2.2;
        santa.add(head);
        var beard = new THREE.Mesh(new THREE.SphereGeometry(0.3, 16, 16), white);
        beard.position.set(0.06, 2.02, 0.12);
        beard.scale.set(1, 1.2, 0.8);
        santa.add(beard);
        var hat = new THREE.Mesh(new THREE.ConeGeometry(0.34, 0.55, 16), suit);
        hat.position.set(-0.05, 2.6, 0);
        hat.rotation.z = 0.25;
        santa.add(hat);
        var pom = new THREE.Mesh(new THREE.SphereGeometry(0.12, 12, 12), white);
        pom.position.set(-0.2, 2.86, 0);
        santa.add(pom);
        var brim = new THREE.Mesh(new THREE.TorusGeometry(0.3, 0.09, 8, 20), white);
        brim.position.set(0, 2.42, 0);
        brim.rotation.x = Math.PI / 2;
        santa.add(brim);
        santa.position.set(0.15, 0, 0);
        g.add(santa);

        return g;
    }

    function makeReins(THREE, from, to) {
        var gold = new THREE.MeshStandardMaterial({ color: 0xc9a24a, roughness: 0.5 });
        var len = Math.hypot(to.x - from.x, to.y - from.y);
        var rope = new THREE.Mesh(new THREE.CylinderGeometry(0.03, 0.03, len, 6), gold);
        var mid = { x: (from.x + to.x) / 2, y: (from.y + to.y) / 2 };
        rope.position.set(mid.x, mid.y, 0);
        rope.rotation.z = Math.atan2(to.y - from.y, to.x - from.x) - Math.PI / 2;
        return rope;
    }

    // ── Mount ───────────────────────────────────────────────────────────────────────
    function mount(host) {
        if (!host || host.dataset.yrThreeMounted) { return; }
        if (!webglOk()) { return; }            // WebGL yo'q → SVG zaxira uchadi

        host.dataset.yrThreeMounted = "1";

        loadThree().then(function (THREE) {
            var card = host.closest(".yr-card");
            var w = host.clientWidth || (card && card.clientWidth) || 600;
            var h = host.clientHeight || (card && card.clientHeight) || 400;

            var renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true });
            renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
            renderer.setSize(w, h, false);
            renderer.setClearColor(0x000000, 0);
            renderer.domElement.setAttribute("aria-hidden", "true");
            host.appendChild(renderer.domElement);
            if (card) { card.classList.add("yr-has-three"); }   // SVG zaxirani yashiradi

            var scene = new THREE.Scene();
            var camera = new THREE.PerspectiveCamera(45, w / h, 0.1, 100);
            camera.position.set(0, 0, 16);

            scene.add(new THREE.AmbientLight(0xffffff, 0.72));
            var dir = new THREE.DirectionalLight(0xfff1d0, 0.95);
            dir.position.set(6, 10, 8);
            scene.add(dir);
            var rim = new THREE.DirectionalLight(0x9fc0ff, 0.35);
            rim.position.set(-8, 2, 4);
            scene.add(rim);

            // Jamoa: 2 bug'u + jilov + chana(+Qorbobo)
            var team = new THREE.Group();
            var deer1 = makeReindeer(THREE, false);   // orqa bug'u
            deer1.position.set(-2.1, 0, 0);
            team.add(deer1);
            var deer2 = makeReindeer(THREE, true);     // yetakchi (Rudolf)
            deer2.position.set(0.4, 0.25, 0);
            team.add(deer2);

            var sleigh = makeSleighWithSanta(THREE);
            sleigh.position.set(-6.2, -0.1, 0);
            team.add(sleigh);

            team.add(makeReins(THREE, { x: -4.3, y: 1.2 }, { x: -1.4, y: 1.0 }));
            team.add(makeReins(THREE, { x: -1.8, y: 1.0 }, { x: 1.2, y: 1.35 }));

            // Butun jamoani chapga qaratamiz (harakat yo'nalishi bo'yicha uchsin)
            team.scale.setScalar(0.62);
            team.rotation.z = 0.08;
            scene.add(team);

            // Ko'rinadigan kenglikni hisoblab, uchish diapazonini aniqlaymiz
            function visibleWidthAt(z) {
                var vh = 2 * Math.tan((camera.fov * Math.PI / 180) / 2) * (camera.position.z - z);
                return { w: vh * camera.aspect, h: vh };
            }
            var span = visibleWidthAt(0);
            var startX, endX, flyY;
            function recalcRange() {
                span = visibleWidthAt(0);
                startX = -span.w / 2 - 6;
                endX = span.w / 2 + 6;
                flyY = span.h * 0.26;
            }
            recalcRange();

            var t = Math.random() * (endX - startX);
            var running = true;
            var rafId = null;
            var clock = new THREE.Clock();

            function resize() {
                var nw = host.clientWidth || w;
                var nh = host.clientHeight || h;
                if (nw < 2 || nh < 2) { return; }
                renderer.setSize(nw, nh, false);
                camera.aspect = nw / nh;
                camera.updateProjectionMatrix();
                recalcRange();
            }
            var ro = ("ResizeObserver" in window) ? new ResizeObserver(resize) : null;
            if (ro) { ro.observe(host); } else { window.addEventListener("resize", resize); }

            function cleanup() {
                running = false;
                if (rafId) { cancelAnimationFrame(rafId); }
                if (ro) { ro.disconnect(); } else { window.removeEventListener("resize", resize); }
                scene.traverse(function (o) {
                    if (o.geometry) { o.geometry.dispose(); }
                    if (o.material) {
                        (Array.isArray(o.material) ? o.material : [o.material]).forEach(function (m) { m.dispose(); });
                    }
                });
                renderer.dispose();
                if (renderer.domElement && renderer.domElement.parentNode) {
                    renderer.domElement.parentNode.removeChild(renderer.domElement);
                }
            }

            function frame() {
                if (!running) { return; }
                // Kartochka DOM'dan ketgan bo'lsa (modal yopildi) — tozalash
                if (!host.isConnected) { cleanup(); return; }

                var dt = Math.min(clock.getDelta(), 0.05);
                if (!document.hidden) {
                    t += dt * (endX - startX) / 9.5;      // ~9.5s da to'liq kesib o'tadi
                    if (t > (endX - startX)) { t = 0; }

                    var x = startX + t;
                    team.position.x = x;
                    team.position.y = flyY + Math.sin(t * 0.9) * 0.5;
                    team.rotation.z = 0.08 + Math.sin(t * 0.9) * 0.03;

                    // Bug'ular oyog'ini "yugurtiramiz"
                    var gallop = clock.elapsedTime * 9;
                    [deer1, deer2].forEach(function (d, di) {
                        var legs = d.userData.legs || [];
                        legs.forEach(function (leg, li) {
                            leg.rotation.z = Math.sin(gallop + li * 1.6 + di) * 0.5;
                        });
                    });
                }
                renderer.render(scene, camera);
                rafId = requestAnimationFrame(frame);
            }
            frame();

            host._yrSceneCleanup = cleanup;
        }).catch(function () {
            // three.js yuklanmadi — SVG zaxira uchishda davom etadi (hech narsa qilmaymiz)
            host.removeAttribute("data-yr-three-mounted");
        });
    }

    window.YearReviewScene = { mount: mount };
})();
