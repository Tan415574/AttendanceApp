// Live check-in board: each student who checks in "falls" onto the board as their
// assigned blob character and settles among the others (physics via matter.js).
// Real-time push comes from SignalR (AttendanceHub) rather than polling.

// Mirrors Services/AvatarAssigner.cs — keep these two lists in sync if you add avatars.
const AVATARS = [
    { shape: "round", color: "#F6A8D8" },
    { shape: "clover", color: "#EC6FB6" },
    { shape: "wave", color: "#B58AD9" },
    { shape: "cloud", color: "#C9B6F0" },
    { shape: "round", color: "#38B6E8" },
    { shape: "hex", color: "#1FA9C9" },
    { shape: "round", color: "#2FA84F" },
    { shape: "triangle", color: "#3D9B4A" },
    { shape: "square", color: "#E8641C" },
    { shape: "round", color: "#F08A1C" },
    { shape: "pill", color: "#F5A623" },
    { shape: "round", color: "#F6C93B" },
];

(function initBoard() {
    const { Engine, Render, Runner, Bodies, Composite, Body } = Matter;

    const holder = document.getElementById("boardCanvas");
    const width = holder.clientWidth || 700;
    const height = holder.clientHeight || 560;

    const engine = Engine.create();
    engine.gravity.y = 0.8;

    const render = Render.create({
        element: holder,
        engine,
        options: { width, height, wireframes: false, background: "transparent" },
    });

    // Floor + side walls so avatars pile up instead of falling off-screen.
    const floor = Bodies.rectangle(width / 2, height + 20, width, 40, { isStatic: true, render: { visible: false } });
    const leftWall = Bodies.rectangle(-20, height / 2, 40, height, { isStatic: true, render: { visible: false } });
    const rightWall = Bodies.rectangle(width + 20, height / 2, 40, height, { isStatic: true, render: { visible: false } });
    Composite.add(engine.world, [floor, leftWall, rightWall]);

    Render.run(render);
    Runner.run(Runner.create(), engine);

    const bodyLabels = new Map(); // matter body id -> {name}

    function spawnAvatar(name, avatarIndex) {
        const avatar = AVATARS[avatarIndex % AVATARS.length];
        const x = 40 + Math.random() * (width - 80);
        const radius = 28;

        let body;
        switch (avatar.shape) {
            case "triangle":
                body = Bodies.polygon(x, -40, 3, radius, { restitution: 0.4, friction: 0.6 });
                break;
            case "square":
                body = Bodies.rectangle(x, -40, radius * 1.7, radius * 1.7, { restitution: 0.4, friction: 0.6, chamfer: { radius: 10 } });
                break;
            case "hex":
                body = Bodies.polygon(x, -40, 6, radius, { restitution: 0.4, friction: 0.6 });
                break;
            default: // round, clover, wave, cloud, pill — all render as a soft circle body
                body = Bodies.circle(x, -40, radius, { restitution: 0.4, friction: 0.6 });
        }

        body.render.fillStyle = avatar.color;
        body.render.strokeStyle = "rgba(0,0,0,0.08)";
        body.render.lineWidth = 2;
        Body.setAngularVelocity(body, (Math.random() - 0.5) * 0.2);

        Composite.add(engine.world, body);
        bodyLabels.set(body.id, name.split(" ").map(p => p[0]).slice(0, 2).join("").toUpperCase());
    }

    // Draw initials on top of each settled body after each physics tick.
    Matter.Events.on(render, "afterRender", () => {
        const ctx = render.context;
        ctx.font = "11px -apple-system, sans-serif";
        ctx.fillStyle = "#1c1c1c";
        ctx.textAlign = "center";
        Composite.allBodies(engine.world).forEach((b) => {
            const label = bodyLabels.get(b.id);
            if (label) ctx.fillText(label, b.position.x, b.position.y + 4);
        });
    });

    function updateCount(n) {
        const el = document.getElementById("checkedInCount");
        if (el) el.textContent = `${n} checked in`;
    }

    // Seed with anyone who'd already checked in before the board page loaded/refreshed.
    let count = 0;
    (window.INITIAL_CHECKINS || []).forEach((c, i) => {
        setTimeout(() => spawnAvatar(c.name, c.avatarIndex), i * 150);
        count++;
    });
    updateCount(count);

    // Live updates via SignalR
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/attendance")
        .withAutomaticReconnect()
        .build();

    connection.on("CheckIn", (evt) => {
        spawnAvatar(evt.studentName, evt.avatarIndex);
        updateCount(evt.totalCheckedIn);
    });

    connection.start().then(() => {
        connection.invoke("JoinBoard", window.BOARD_SESSION_ID);
    });
})();
