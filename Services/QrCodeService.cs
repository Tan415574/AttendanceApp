using QRCoder;

namespace AttendanceApp.Services;

public class QrCodeService
{
    // Encodes a deep link straight to the check-in page with the code pre-filled,
    // so scanning drops the student straight into "confirm your student number"
    // rather than a generic homepage.
    public string BuildCheckInUrl(HttpRequest request, string joinCode)
    {
        var baseUrl = $"{request.Scheme}://{request.Host}";
        return $"{baseUrl}/Student/CheckIn?code={joinCode}";
    }

    public string GeneratePngDataUrl(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(data);
        byte[] bytes = pngQr.GetGraphic(10);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
