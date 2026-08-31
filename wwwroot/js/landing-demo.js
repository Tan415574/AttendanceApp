// Purely cosmetic showcase for the landing page — same physics engine, mascot
// palette, and face-drawing logic as the real live board (wwwroot/js/board.js), but
// self-contained and driven by a timer instead of real SignalR check-ins. No real
// data involved. Initializes one independent instance per [data-landing-demo] holder
// found on the page, so the landing page can show more than one live-feeling board.
(function initLandingDemos() {
    if (typeof Matter === "undefined") return;
    document.querySelectorAll(".board-canvas-holder[data-landing-demo]").forEach(initOne);

    function initOne(holder) {
        const { Engine, Render, Runner, Bodies, Composite, Body } = Matter;

        const AVATARS = ["#FF6B4A", "#6BCB77", "#FFC93C", "#4DA6FF", "#FF6FA5"];
        const NAMES = ["Alex Kim", "Priya N", "Sam O", "Jordan M", "Riya P", "Chen W", "Liam B", "Zoe T", "Noah R", "Amara D", "Kofi A", "Mei L"];
        const FACE_INK = "#22201C";

        function drawMascotFace(ctx, x, y, radius, faceIndex) {
            ctx.strokeStyle = FACE_INK;
            ctx.fillStyle = FACE_INK;
            ctx.lineWidth = Math.max(2, radius * 0.09);
            ctx.lineCap = "round";
            const eyeOffsetX = radius * 0.32;
            const eyeY = y - radius * 0.06;

            if (faceIndex % 3 !== 0) {
                const eyeR = radius * 0.09;
                ctx.beginPath(); ctx.arc(x - eyeOffsetX, eyeY, eyeR, 0, Math.PI * 2); ctx.fill();
                ctx.beginPath(); ctx.arc(x + eyeOffsetX, eyeY, eyeR, 0, Math.PI * 2); ctx.fill();
                ctx.beginPath();
                ctx.moveTo(x - radius * 0.3, y + radius * 0.16);
                ctx.quadraticCurveTo(x, y + radius * 0.46, x + radius * 0.3, y + radius * 0.16);
                ctx.stroke();
            } else {
                const eyeR = radius * 0.15;
                ctx.beginPath(); ctx.arc(x - eyeOffsetX, eyeY, eyeR, 0, Math.PI * 2); ctx.fill();
                ctx.beginPath(); ctx.arc(x + eyeOffsetX, eyeY, eyeR, 0, Math.PI * 2); ctx.fill();
                ctx.beginPath(); ctx.arc(x, y + radius * 0.32, radius * 0.09, 0, Math.PI * 2); ctx.fill();
            }
        }

        const width = holder.clientWidth || 600;
        const height = holder.clientHeight || 420;
        const RADIUS = parseInt(holder.dataset.blobRadius || "26", 10);

        const engine = Engine.create();
        engine.gravity.y = 0.7;

        const render = Render.create({
            element: holder,
            engine,
            options: { width, height, wireframes: false, background: "transparent" },
        });

        const floor = Bodies.rectangle(width / 2, height + 20, width, 40, { isStatic: true, render: { visible: false } });
        const leftWall = Bodies.rectangle(-20, height / 2, 40, height, { isStatic: true, render: { visible: false } });
        const rightWall = Bodies.rectangle(width + 20, height / 2, 40, height, { isStatic: true, render: { visible: false } });
        Composite.add(engine.world, [floor, leftWall, rightWall]);

        Render.run(render);
        Runner.run(Runner.create(), engine);

        const bodyLabels = new Map();
        let checkedIn = 0;
        const countEl = holder.querySelector(".board-count");
        let spawnCount = 0;

        function spawn() {
            const colorIndex = Math.floor(Math.random() * AVATARS.length);
            const x = 34 + Math.random() * (width - 68);
            const body = Bodies.circle(x, -30, RADIUS, { restitution: 0.45, friction: 0.6 });
            body.render.fillStyle = AVATARS[colorIndex];
            body.render.strokeStyle = "rgba(34,32,28,0.12)";
            body.render.lineWidth = 2;
            Body.setAngularVelocity(body, (Math.random() - 0.5) * 0.06);
            Composite.add(engine.world, body);

            const name = NAMES[Math.floor(Math.random() * NAMES.length)];
            bodyLabels.set(body.id, {
                initials: name.split(" ").map(p => p[0]).slice(0, 2).join("").toUpperCase(),
                faceIndex: spawnCount++,
            });

            checkedIn++;
            if (countEl) countEl.textContent = `${checkedIn} checked in`;

            // Keep the demo from piling up forever — drop the oldest once it gets crowded.
            const dynamicBodies = Composite.allBodies(engine.world).filter(b => !b.isStatic);
            if (dynamicBodies.length > 20) {
                const oldest = dynamicBodies[0];
                Composite.remove(engine.world, oldest);
                bodyLabels.delete(oldest.id);
            }
        }

        Matter.Events.on(render, "afterRender", () => {
            const ctx = render.context;
            Composite.allBodies(engine.world).forEach(b => {
                const entry = bodyLabels.get(b.id);
                if (!entry) return;
                drawMascotFace(ctx, b.position.x, b.position.y, RADIUS, entry.faceIndex);
                ctx.font = `700 9px 'Baloo 2', sans-serif`;
                ctx.fillStyle = "rgba(34,32,28,0.65)";
                ctx.textAlign = "center";
                ctx.fillText(entry.initials, b.position.x, b.position.y + RADIUS * 0.78);
            });
        });

        const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        if (!reduceMotion) {
            const spawnDelay = parseInt(holder.dataset.spawnDelay || "1100", 10);
            for (let i = 0; i < 5; i++) setTimeout(spawn, i * 250);
            setInterval(spawn, spawnDelay);
        } else {
            spawn();
        }
    }
})();
