// Big floating mascot on the landing page whose eyes track the cursor. Purely
// decorative — no effect on layout or any real app data.
(function initCursorMascot() {
    const eyes = document.querySelectorAll(".cursor-mascot .mascot-eye");
    if (!eyes.length) return;
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    const maxOffset = 5;

    function update(clientX, clientY) {
        eyes.forEach(eye => {
            const pupil = eye.querySelector(".pupil");
            if (!pupil) return;
            const baseCx = parseFloat(pupil.dataset.cx);
            const baseCy = parseFloat(pupil.dataset.cy);
            const rect = eye.getBoundingClientRect();
            const originX = rect.left + rect.width / 2;
            const originY = rect.top + rect.height / 2;
            const dx = clientX - originX;
            const dy = clientY - originY;
            const angle = Math.atan2(dy, dx);
            const dist = Math.min(Math.hypot(dx, dy) / 20, maxOffset);
            pupil.setAttribute("cx", baseCx + Math.cos(angle) * dist);
            pupil.setAttribute("cy", baseCy + Math.sin(angle) * dist);
        });
    }

    window.addEventListener("mousemove", e => update(e.clientX, e.clientY));
    window.addEventListener("touchmove", e => {
        if (e.touches && e.touches[0]) update(e.touches[0].clientX, e.touches[0].clientY);
    }, { passive: true });
})();
