using System.Globalization;
using System.Text;

namespace SmartMetering.Infrastructure.Storage;

internal static class SimplePdfWriter
{
    public static byte[] Create(string text)
    {
        var lines = PrepareLines(text).Take(52).ToList();
        var content = BuildContent(lines);
        var contentBytes = Encoding.ASCII.GetBytes(content);

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {contentBytes.Length} >>\nstream\n{content}\nendstream",
        };

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };
        writer.WriteLine("%PDF-1.4");

        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            writer.Flush();
            offsets.Add(stream.Position);
            writer.WriteLine($"{i + 1} 0 obj");
            writer.WriteLine(objects[i]);
            writer.WriteLine("endobj");
        }

        writer.Flush();
        var xrefOffset = stream.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Count + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
        {
            writer.WriteLine($"{offset.ToString("D10", CultureInfo.InvariantCulture)} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
        writer.WriteLine("%%EOF");
        writer.Flush();
        return stream.ToArray();
    }

    private static string BuildContent(IEnumerable<string> lines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BT");
        sb.AppendLine("/F1 10 Tf");
        sb.AppendLine("14 TL");
        sb.AppendLine("50 800 Td");

        foreach (var line in lines)
        {
            sb.AppendLine($"({Escape(line)}) Tj");
            sb.AppendLine("T*");
        }

        sb.Append("ET");
        return sb.ToString();
    }

    private static IEnumerable<string> PrepareLines(string text)
    {
        foreach (var rawLine in text.Replace("\r\n", "\n").Split('\n'))
        {
            var ascii = ToPdfAscii(rawLine);
            if (ascii.Length <= 92)
            {
                yield return ascii;
                continue;
            }

            for (var index = 0; index < ascii.Length; index += 92)
            {
                yield return ascii.Substring(index, Math.Min(92, ascii.Length - index));
            }
        }
    }

    private static string ToPdfAscii(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            sb.Append(ch is >= ' ' and <= '~' ? ch : '?');
        }

        return sb.ToString();
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
