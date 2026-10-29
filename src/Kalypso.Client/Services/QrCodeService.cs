using QRCoder;

namespace Kalypso.Client.Services;

public sealed class QrCodeService : IQrCodeService
{
    private static readonly byte[] Navy = [0x0B, 0x1F, 0x4B];
    private static readonly byte[] White = [0xFF, 0xFF, 0xFF];

    public byte[] CreatePng(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        return new PngByteQRCode(data).GetGraphic(12, Navy, White);
    }
}
