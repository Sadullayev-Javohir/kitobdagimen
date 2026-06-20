// 🦉 Bilimdon boyo'g'li — /chat sahifasidagi 3D ko'makchi.
// Protsedural three.js modeli (asset fayl shart emas). WebGL/CDN bo'lmasa 2D SVG zaxira.
// Tashqi API (chat skripti ishlatadi):
//   const owl = mountOwl(canvas);
//   owl.lookAt(clientX, clientY);  owl.alert();  owl.idle();  owl.happy();  owl.dispose();

const THREE_URL = "https://unpkg.com/three@0.160.0/build/three.module.js";

export function mountOwl(canvas) {
    // Shared, backend-agnostic state. The 3D and 2D renderers both read this each frame.
    const state = {
        mode: "idle",          // idle | curious | alert | happy
        target: { x: 0, y: 0 },// normalized look direction (-1..1)
        curiousUntil: 0,
        modeUntil: 0,
        disposed: false
    };

    const api = {
        lookAt(clientX, clientY) {
            const r = canvas.getBoundingClientRect();
            const cx = r.left + r.width / 2;
            const cy = r.top + r.height / 2;
            // Map cursor offset to a clamped look direction.
            state.target.x = clamp((clientX - cx) / (window.innerWidth / 2), -1, 1);
            state.target.y = clamp((clientY - cy) / (window.innerHeight / 2), -1, 1);
            if (state.mode === "idle") { state.mode = "curious"; }
            state.curiousUntil = now() + 2500;
        },
        alert() { state.mode = "alert"; state.modeUntil = now() + 4000; state.target = { x: 0, y: -0.15 }; },
        happy() { state.mode = "happy"; state.modeUntil = now() + 1600; },
        idle() { state.mode = "idle"; },
        setState(m) { state.mode = m; },
        dispose() { state.disposed = true; if (state.cleanup) state.cleanup(); }
    };

    // Try the 3D owl; fall back to a 2D SVG owl on any failure (no WebGL / no CDN).
    init3D(canvas, state).catch((err) => {
        console.warn("3D boyo'g'li ishlamadi, 2D zaxira:", err);
        try { mount2D(canvas, state); } catch (e) { /* give up silently */ }
    });

    return api;
}

function now() { return performance.now(); }
function clamp(v, lo, hi) { return v < lo ? lo : v > hi ? hi : v; }
function lerp(a, b, t) { return a + (b - a) * t; }

// ───────────────────────── 3D (three.js) ─────────────────────────
async function init3D(canvas, state) {
    if (!window.WebGLRenderingContext) throw new Error("WebGL yo'q");

    const THREE = await import(/* @vite-ignore */ THREE_URL);

    const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
    renderer.setClearColor(0x000000, 0);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    renderer.setSize(220, 240, false);

    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(40, 220 / 240, 0.1, 100);
    camera.position.set(0, 0.2, 5);
    camera.lookAt(0, 0.1, 0);

    scene.add(new THREE.HemisphereLight(0xfff4e6, 0x1b4d3e, 1.1));
    const dir = new THREE.DirectionalLight(0xffffff, 0.7);
    dir.position.set(2, 3, 4);
    scene.add(dir);

    // Materials (project palette) — owl body is a lighter accent orange (#e8703a dan ochroq).
    const bodyMat = new THREE.MeshStandardMaterial({ color: 0xf2915e, roughness: 0.8 });
    const bellyMat = new THREE.MeshStandardMaterial({ color: 0xf3e4c8, roughness: 0.9 });
    const eyeMat = new THREE.MeshStandardMaterial({ color: 0xfffdf8, roughness: 0.4 });
    const pupilMat = new THREE.MeshStandardMaterial({ color: 0x1f2a24, roughness: 0.3 });
    const beakMat = new THREE.MeshStandardMaterial({ color: 0xe07d4a, roughness: 0.5 });
    const tuftMat = new THREE.MeshStandardMaterial({ color: 0xe07d4a, roughness: 0.8 });

    const owlRoot = new THREE.Group();
    scene.add(owlRoot);

    // Body (egg).
    const bodyGroup = new THREE.Group();
    owlRoot.add(bodyGroup);
    const body = new THREE.Mesh(new THREE.SphereGeometry(1.15, 32, 32), bodyMat);
    body.scale.set(1, 1.25, 1);
    body.position.y = -0.7;
    bodyGroup.add(body);
    const belly = new THREE.Mesh(new THREE.SphereGeometry(0.78, 24, 24), bellyMat);
    belly.scale.set(1, 1.2, 0.6);
    belly.position.set(0, -0.65, 0.7);
    bodyGroup.add(belly);

    // Wings (children of body so they flap relative to it).
    function makeWing(side) {
        const wing = new THREE.Mesh(new THREE.SphereGeometry(0.55, 16, 16), bodyMat);
        wing.scale.set(0.3, 1.0, 0.7);
        wing.position.set(side * 1.05, -0.6, 0.1);
        bodyGroup.add(wing);
        return wing;
    }
    const leftWing = makeWing(-1);
    const rightWing = makeWing(1);

    // Head group (yaw + pitch live here).
    const headGroup = new THREE.Group();
    headGroup.position.y = 0.55;
    owlRoot.add(headGroup);
    const head = new THREE.Mesh(new THREE.SphereGeometry(0.95, 32, 32), bodyMat);
    headGroup.add(head);

    // Eyes + pupils.
    function makeEye(side) {
        const eye = new THREE.Mesh(new THREE.SphereGeometry(0.36, 24, 24), eyeMat);
        eye.position.set(side * 0.38, 0.12, 0.72);
        headGroup.add(eye);
        const pupil = new THREE.Mesh(new THREE.SphereGeometry(0.16, 16, 16), pupilMat);
        pupil.position.set(0, 0, 0.28);
        eye.add(pupil);
        return { eye, pupil, baseScale: 1 };
    }
    const leftEye = makeEye(-1);
    const rightEye = makeEye(1);

    // Beak.
    const beak = new THREE.Mesh(new THREE.ConeGeometry(0.16, 0.34, 4), beakMat);
    beak.rotation.x = Math.PI / 2;
    beak.position.set(0, -0.18, 0.92);
    headGroup.add(beak);

    // Ear tufts.
    function makeTuft(side) {
        const tuft = new THREE.Mesh(new THREE.ConeGeometry(0.18, 0.5, 8), tuftMat);
        tuft.position.set(side * 0.5, 0.92, 0);
        tuft.rotation.z = side * -0.3;
        headGroup.add(tuft);
        return tuft;
    }
    const leftTuft = makeTuft(-1);
    const rightTuft = makeTuft(1);

    // Animation bookkeeping.
    let nextLook = now() + 2000;
    let nextBlink = now() + 3000;
    let blink = 0;            // 0 = open, 1 = closed
    let blinking = false;
    let raf = null;

    function pickIdleTarget() {
        state.target.x = (Math.random() * 2 - 1) * 0.8;
        state.target.y = (Math.random() * 2 - 1) * 0.5;
        nextLook = now() + 1500 + Math.random() * 2500;
    }

    function frame() {
        if (state.disposed) return;
        const t = now();

        // Mode lifetime: alert/happy revert to idle when expired.
        if ((state.mode === "alert" || state.mode === "happy") && t > state.modeUntil) state.mode = "idle";
        if (state.mode === "curious" && t > state.curiousUntil) state.mode = "idle";
        if (state.mode === "idle" && t > nextLook) pickIdleTarget();

        // Head easing toward target.
        const yaw = state.target.x * 0.9;
        const pitch = -state.target.y * 0.5;
        headGroup.rotation.y = lerp(headGroup.rotation.y, yaw, 0.08);
        headGroup.rotation.x = lerp(headGroup.rotation.x, pitch, 0.08);

        // Pupils drift toward target inside the eye.
        const px = state.target.x * 0.12, py = -state.target.y * 0.1;
        leftEye.pupil.position.x = lerp(leftEye.pupil.position.x, px, 0.15);
        leftEye.pupil.position.y = lerp(leftEye.pupil.position.y, py, 0.15);
        rightEye.pupil.position.x = lerp(rightEye.pupil.position.x, px, 0.15);
        rightEye.pupil.position.y = lerp(rightEye.pupil.position.y, py, 0.15);

        // Eye widening on alert.
        const eyeScale = state.mode === "alert" ? 1.28 : 1;
        leftEye.eye.scale.x = lerp(leftEye.eye.scale.x, eyeScale, 0.15);
        rightEye.eye.scale.x = lerp(rightEye.eye.scale.x, eyeScale, 0.15);

        // Ear tufts perk up on alert.
        const tuftZ = state.mode === "alert" ? 0 : 0.3;
        leftTuft.rotation.z = lerp(leftTuft.rotation.z, tuftZ, 0.12);
        rightTuft.rotation.z = lerp(rightTuft.rotation.z, -tuftZ, 0.12);

        // Blink schedule.
        if (!blinking && t > nextBlink) { blinking = true; }
        if (blinking) {
            blink += 0.25;
            if (blink >= 2) { blink = 0; blinking = false; nextBlink = t + 2000 + Math.random() * 4000; }
        }
        const lid = blink <= 1 ? (1 - blink) : (blink - 1); // 1→0→1
        const openY = 0.05 + 0.95 * lid;
        leftEye.eye.scale.y = openY * (state.mode === "alert" ? 1.28 : 1);
        rightEye.eye.scale.y = openY * (state.mode === "alert" ? 1.28 : 1);

        // Breathing (idle) + happy head bob + alert wing flap.
        const breathe = 1 + Math.sin(t / 700) * 0.02;
        bodyGroup.scale.setScalar(breathe);
        if (state.mode === "happy") {
            owlRoot.rotation.z = Math.sin(t / 90) * 0.12;
        } else {
            owlRoot.rotation.z = lerp(owlRoot.rotation.z, 0, 0.1);
        }
        const flap = state.mode === "alert" ? Math.sin(t / 70) * 0.5 : 0;
        leftWing.rotation.z = lerp(leftWing.rotation.z, flap, 0.2);
        rightWing.rotation.z = lerp(rightWing.rotation.z, -flap, 0.2);

        renderer.render(scene, camera);
        raf = requestAnimationFrame(frame);
    }

    function onVisibility() {
        if (document.hidden) { if (raf) { cancelAnimationFrame(raf); raf = null; } }
        else if (!raf && !state.disposed) { raf = requestAnimationFrame(frame); }
    }
    document.addEventListener("visibilitychange", onVisibility);

    state.cleanup = () => {
        if (raf) cancelAnimationFrame(raf);
        document.removeEventListener("visibilitychange", onVisibility);
        scene.traverse((o) => { if (o.geometry) o.geometry.dispose(); if (o.material) o.material.dispose(); });
        renderer.dispose();
    };

    raf = requestAnimationFrame(frame);
}

// ───────────────────────── 2D SVG fallback ─────────────────────────
function mount2D(canvas, state) {
    canvas.style.display = "none";
    const wrap = canvas.parentElement;
    const holder = document.createElement("div");
    holder.style.cssText = "width:160px;height:200px;display:flex;align-items:center;justify-content:center;transition:transform .15s;";
    holder.innerHTML = `
      <svg viewBox="0 0 160 200" width="160" height="200" aria-hidden="true">
        <ellipse cx="80" cy="130" rx="56" ry="64" fill="#f2915e"/>
        <ellipse cx="80" cy="140" rx="34" ry="44" fill="#f3e4c8"/>
        <circle cx="80" cy="70" r="50" fill="#f2915e"/>
        <polygon points="44,28 58,58 30,52" fill="#e07d4a"/>
        <polygon points="116,28 102,58 130,52" fill="#e07d4a"/>
        <circle cx="60" cy="70" r="20" fill="#fffdf8"/>
        <circle cx="100" cy="70" r="20" fill="#fffdf8"/>
        <circle id="o2p-l" cx="60" cy="70" r="8" fill="#1f2a24"/>
        <circle id="o2p-r" cx="100" cy="70" r="8" fill="#1f2a24"/>
        <polygon points="80,80 73,90 87,90" fill="#e07d4a"/>
      </svg>`;
    wrap.appendChild(holder);
    const pl = holder.querySelector("#o2p-l");
    const pr = holder.querySelector("#o2p-r");
    let raf = null;
    let cx = 0, cy = 0;

    function frame() {
        if (state.disposed) return;
        cx = lerp(cx, state.target.x * 7, 0.15);
        cy = lerp(cy, state.target.y * 6, 0.15);
        pl.setAttribute("transform", `translate(${cx},${cy})`);
        pr.setAttribute("transform", `translate(${cx},${cy})`);
        holder.style.transform = state.mode === "alert"
            ? `translateX(${Math.sin(now() / 60) * 3}px)`
            : (state.mode === "happy" ? `rotate(${Math.sin(now() / 90) * 6}deg)` : "none");
        raf = requestAnimationFrame(frame);
    }
    state.cleanup = () => { if (raf) cancelAnimationFrame(raf); holder.remove(); };
    raf = requestAnimationFrame(frame);
}
