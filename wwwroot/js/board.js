// Live check-in board: each student who checks in "falls" onto the board as a
// mascot blob and settles among the others (physics via matter.js). Real-time push
// comes from SignalR (AttendanceHub) rather than polling.
//
// Mascot system: one blob shape, colored per the Roll Call palette, with a face
// drawn on top each frame (matches the style guide's "one shape, many faces"
// principle — every check-in is a good-news moment, so faces alternate between a
// big smile and a wide-eyed "streak" look rather than encoding attendance state).

// Mirrors Services/AvatarAssigner.cs — keep these two lists in sync if you add avatars.
const AVATARS = [
    "#FF6B4A", // coral
    "#6BCB77", // grass
    "#FFC93C", // sunny
    "#4DA6FF", // sky
    "#FF6FA5", // bubblegum
    "#6BCB77",
    "#FF6B4A",
    "#4DA6FF",
    "#FFC93C",
    "#FF6FA5",
    "#6BCB77",
    "#FF6B4A",
];

const FACE_INK = "#22201C";

function drawMascotFace(ctx, x, y, radius, faceIndex) {
    ctx.strokeStyle = FACE_INK;
    ctx.fillStyle = FACE_INK;
    ctx.lineWidth = Math.max(2, radius * 0.09);
    ctx.lineCap = "round";

    const eyeOffsetX = radius * 0.32;
    const eyeY = y - radius * 0.06;

    if (faceIndex % 3 !== 0) {
        // Big open smile — the default "present, glad to be here" face.
        const eyeR = radius * 0.09;
        ctx.beginPath(); ctx.arc(x - eyeOffsetX, eyeY, eyeR, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(x + eyeOffsetX, eyeY, eyeR, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath();
        ctx.moveTo(x - radius * 0.3, y + radius * 0.16);
        ctx.quadraticCurveTo(x, y + radius * 0.46, x + radius * 0.3, y + radius * 0.16);
        ctx.stroke();
    } else {
        // Wide-eyed "streak" face — sprinkled in for variety.
        const eyeR = radius * 0.15;
        ctx.beginPath(); ctx.arc(x - eyeOffsetX, eyeY, eyeR, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(x + eyeOffsetX, eyeY, eyeR, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(x, y + radius * 0.32, radius * 0.09, 0, Math.PI * 2); ctx.fill();
    }
}

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

    const bodyLabels = new Map(); // matter body id -> initials
    const BLOB_RADIUS = 30;

    function spawnAvatar(name, avatarIndex) {
        const color = AVATARS[avatarIndex % AVATARS.length];
        const x = 44 + Math.random() * (width - 88);

        const body = Bodies.circle(x, -40, BLOB_RADIUS, { restitution: 0.4, friction: 0.6 });
        body.render.fillStyle = color;
        body.render.strokeStyle = "rgba(34,32,28,0.12)";
        body.render.lineWidth = 2;
        Body.setAngularVelocity(body, (Math.random() - 0.5) * 0.08); // gentle — faces should stay mostly upright

        Composite.add(engine.world, body);
        bodyLabels.set(body.id, { initials: name.split(" ").map(p => p[0]).slice(0, 2).join("").toUpperCase(), faceIndex: avatarIndex });
    }

    // Draw a face + initials on top of each settled body after each physics tick.
    Matter.Events.on(render, "afterRender", () => {
        const ctx = render.context;
        Composite.allBodies(engine.world).forEach((b) => {
            const entry = bodyLabels.get(b.id);
            if (!entry) return;
            drawMascotFace(ctx, b.position.x, b.position.y, BLOB_RADIUS, entry.faceIndex);
            ctx.font = `700 10px 'Baloo 2', sans-serif`;
            ctx.fillStyle = "rgba(34,32,28,0.65)";
            ctx.textAlign = "center";
            ctx.fillText(entry.initials, b.position.x, b.position.y + BLOB_RADIUS * 0.78);
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
