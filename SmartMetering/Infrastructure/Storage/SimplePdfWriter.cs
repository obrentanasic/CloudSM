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
            sb.Append(ch is >= ' ' and <= '~' ? ch : Transliterate(ch));
        }

        return sb.ToString();
    }

    private static string Transliterate(char ch) => ch switch
    {
        'А' => "A", 'Б' => "B", 'В' => "V", 'Г' => "G", 'Д' => "D", 'Ђ' => "Dj", 'Е' => "E", 'Ж' => "Z",
        'З' => "Z", 'И' => "I", 'Ј' => "J", 'К' => "K", 'Л' => "L", 'Љ' => "Lj", 'М' => "M", 'Н' => "N",
        'Њ' => "Nj", 'О' => "O", 'П' => "P", 'Р' => "R", 'С' => "S", 'Т' => "T", 'Ћ' => "C", 'У' => "U",
        'Ф' => "F", 'Х' => "H", 'Ц' => "C", 'Ч' => "C", 'Џ' => "Dz", 'Ш' => "S",
        'а' => "a", 'б' => "b", 'в' => "v", 'г' => "g", 'д' => "d", 'ђ' => "dj", 'е' => "e", 'ж' => "z",
        'з' => "z", 'и' => "i", 'ј' => "j", 'к' => "k", 'л' => "l", 'љ' => "lj", 'м' => "m", 'н' => "n",
        'њ' => "nj", 'о' => "o", 'п' => "p", 'р' => "r", 'с' => "s", 'т' => "t", 'ћ' => "c", 'у' => "u",
        'ф' => "f", 'х' => "h", 'ц' => "c", 'ч' => "c", 'џ' => "dz", 'ш' => "s",
        'Č' or 'Ć' => "C", 'č' or 'ć' => "c",
        'Š' => "S", 'š' => "s",
        'Ž' => "Z", 'ž' => "z",
        'Đ' => "Dj", 'đ' => "dj",
        '–' or '—' => "-", '…' => "...", '“' or '”' => "\"", '„' => "\"", '’' => "'",
        _ => " "
    };

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
