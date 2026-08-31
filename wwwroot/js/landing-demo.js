// Purely cosmetic showcase for the landing page — same physics engine and avatar
// palette as the real live board (Hubs/AttendanceHub.cs + wwwroot/js/board.js), but
// self-contained and driven by a timer instead of real SignalR check-ins. No real
// data involved.
(function initLandingDemo() {
    const holder = document.getElementById("landingCanvas");
    if (!holder || typeof Matter === "undefined") return;

    const { Engine, Render, Runner, Bodies, Composite, Body } = Matter;

    const AVATARS = [
        { shape: "round", color: "#F17FB0" },
        { shape: "clover", color: "#9B8CFB" },
        { shape: "wave", color: "#5AC8FA" },
        { shape: "cloud", color: "#3FD9C7" },
        { shape: "round", color: "#6C7BF0" },
        { shape: "hex", color: "#F5B942" },
        { shape: "round", color: "#8EF07F" },
        { shape: "triangle", color: "#5FCE63" },
        { shape: "square", color: "#FF7A6B" },
    ];
    const NAMES = ["Alex Kim", "Priya N", "Sam O", "Jordan M", "Riya P", "Chen W", "Liam B", "Zoe T", "Noah R", "Amara D", "Kofi A", "Mei L"];

    const width = holder.clientWidth || 600;
    const height = holder.clientHeight || 420;

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
    const countEl = document.getElementById("landingCount");

    function spawn() {
        const avatar = AVATARS[Math.floor(Math.random() * AVATARS.length)];
        const x = 32 + Math.random() * (width - 64);
        const radius = 22;

        let body;
        switch (avatar.shape) {
            case "triangle":
                body = Bodies.polygon(x, -30, 3, radius, { restitution: 0.45, friction: 0.6 });
                break;
            case "square":
                body = Bodies.rectangle(x, -30, radius * 1.7, radius * 1.7, { restitution: 0.45, friction: 0.6, chamfer: { radius: 8 } });
                break;
            case "hex":
                body = Bodies.polygon(x, -30, 6, radius, { restitution: 0.45, friction: 0.6 });
                break;
            default:
                body = Bodies.circle(x, -30, radius, { restitution: 0.45, friction: 0.6 });
        }

        body.render.fillStyle = avatar.color;
        body.render.strokeStyle = "rgba(0,0,0,0.15)";
        body.render.lineWidth = 2;
        Body.setAngularVelocity(body, (Math.random() - 0.5) * 0.2);
        Composite.add(engine.world, body);

        const name = NAMES[Math.floor(Math.random() * NAMES.length)];
        bodyLabels.set(body.id, name.split(" ").map(p => p[0]).slice(0, 2).join("").toUpperCase());

        checkedIn++;
        if (countEl) countEl.textContent = `${checkedIn} checked in`;

        // Keep the demo from piling up forever — drop the oldest once it gets crowded.
        const dynamicBodies = Composite.allBodies(engine.world).filter(b => !b.isStatic);
        if (dynamicBodies.length > 22) {
            const oldest = dynamicBodies[0];
            Composite.remove(engine.world, oldest);
            bodyLabels.delete(oldest.id);
        }
    }

    Matter.Events.on(render, "afterRender", () => {
        const ctx = render.context;
        ctx.font = "10px -apple-system, sans-serif";
        ctx.fillStyle = "#0c0c14";
        ctx.textAlign = "center";
        Composite.allBodies(engine.world).forEach(b => {
            const label = bodyLabels.get(b.id);
            if (label) ctx.fillText(label, b.position.x, b.position.y + 3);
        });
    });

    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (!reduceMotion) {
        for (let i = 0; i < 5; i++) setTimeout(spawn, i * 250);
        setInterval(spawn, 1100);
    } else {
        spawn();
    }
})();
