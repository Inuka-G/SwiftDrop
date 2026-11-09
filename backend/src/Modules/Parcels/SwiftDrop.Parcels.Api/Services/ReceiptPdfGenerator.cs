
using System.Text;
using SwiftDrop.Parcels.Business.Services;

namespace SwiftDrop.Parcels.Api.Services;

public static class ReceiptPdfGenerator
{
    public static byte[] Generate(ParcelDetailsDto parcel)
    {
        var text = new StringBuilder();

        text.AppendLine("SWIFTDROP DELIVERY RECEIPT");
        text.AppendLine("============================================");
        text.AppendLine($"Receipt Date:    {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        text.AppendLine($"Tracking Number: {parcel.TrackingNumber}");
        text.AppendLine($"Status:          {parcel.CurrentStatus}");
        text.AppendLine("--------------------------------------------");
        text.AppendLine($"Recipient:       {parcel.RecipientName}");
        text.AppendLine($"Drop-off:        {parcel.DropoffAddress}");
        text.AppendLine($"Pickup Address:  {parcel.PickupAddress}");
        text.AppendLine($"Weight:          {parcel.WeightKg} kg");
        text.AppendLine("--------------------------------------------");
        text.AppendLine($"Estimated Total: LKR {parcel.EstimatedFee:F2}");
        text.AppendLine("============================================");
        text.AppendLine("Thank you for choosing SwiftDrop!");

        return CreateSimplePdf(text.ToString());
    }

    private static byte[] CreateSimplePdf(string content)
    {
        var lines = content.Split(
            new[] { "\r\n", "\r", "\n" },
            StringSplitOptions.None);

        var sbText = new StringBuilder();
        var y = 720;

        foreach (var line in lines)
        {
            var escaped = line
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");

            sbText.AppendLine($"1 0 0 1 50 {y} Tm ({escaped}) Tj");
            y -= 18;
        }

        var streamContent = $"BT /F1 11 Tf\n{sbText}ET";

        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",

            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",

            "<< /Type /Page /Parent 2 0 R /Resources << /Font << /F1 4 0 R >> >> /MediaBox [0 0 595 842] /Contents 5 0 R >>",

            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>",

            $"<< /Length {Encoding.ASCII.GetByteCount(streamContent)} >>\nstream\n{streamContent}\nendstream"
        };

        using var output = new MemoryStream();

        using var writer = new StreamWriter(
            output,
            Encoding.ASCII,
            leaveOpen: true);

        writer.NewLine = "\n";

        writer.Write("%PDF-1.4\n");

        var offsets = new List<long> { 0 };

        for (var i = 0; i < objects.Length; i++)
        {
            writer.Flush();

            offsets.Add(output.Position);

            writer.Write($"{i + 1} 0 obj\n");
            writer.Write(objects[i]);
            writer.Write("\nendobj\n");
        }

        writer.Flush();

        var xrefPosition = output.Position;

        writer.Write($"xref\n0 {objects.Length + 1}\n");
        writer.Write("0000000000 65535 f \n");

        foreach (var offset in offsets.Skip(1))
        {
            writer.Write($"{offset:D10} 00000 n \n");
        }

        writer.Write(
            $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\n");

        writer.Write($"startxref\n{xrefPosition}\n%%EOF");

        writer.Flush();

        return output.ToArray();
    }
}
