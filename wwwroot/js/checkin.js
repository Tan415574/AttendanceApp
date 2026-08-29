// Lets a student scan the board's QR code without leaving the app (as an alternative
// to just using their phone's native camera, which opens the encoded URL directly).
document.getElementById("scanBtn").addEventListener("click", () => {
    const readerDiv = document.getElementById("qr-reader");
    readerDiv.style.display = "block";

    const scanner = new Html5Qrcode("qr-reader");
    scanner.start(
        { facingMode: "environment" },
        { fps: 10, qrbox: 220 },
        (decodedText) => {
            // Board encodes a full check-in URL; pull the "code" query param out of it,
            // but fall back to treating the scanned text as the raw code itself.
            let code = decodedText;
            try {
                const url = new URL(decodedText);
                code = url.searchParams.get("code") || decodedText;
            } catch { /* not a URL — use raw text */ }

            document.getElementById("codeInput").value = code.toUpperCase();
            document.getElementById("scannedInput").value = "true";
            scanner.stop().then(() => { readerDiv.style.display = "none"; });
        },
        () => { /* ignore per-frame decode failures */ }
    );
});

// Pre-fill from ?code= in the URL, e.g. when the student scans with their native
// camera app and lands here directly.
const params = new URLSearchParams(window.location.search);
if (params.get("code")) {
    document.getElementById("codeInput").value = params.get("code").toUpperCase();
    document.getElementById("scannedInput").value = "true";
}

// Any manual edit to the code after a scan means we can no longer call this a scan.
document.getElementById("codeInput").addEventListener("input", () => {
    document.getElementById("scannedInput").value = "false";
});
