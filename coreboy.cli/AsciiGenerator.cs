using System.Drawing;
using System.Text;

namespace coreboy.cli;

public class AsciiGenerator
{
    private static readonly Dictionary<float, string> Map;

    static AsciiGenerator()
    {
        Map = new()
        {
            {200, " "},
            {190, " "},
            {180, " "},
            {170, " "},
            {160, " "},
            {150, "."},
            {140, "o"},
            {130, "O"},
            {120, "+"},
            {110, "#"},
            {100, "@"},
            {080, "%"},
            {060, "░"},
            {040, "▒"},
            {020, "▓"},
            {000, "█"},
        };
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public static string GenerateFrame(byte[] frameData)
    {
        using MemoryStream ms = new(frameData);
        var image = Image.FromStream(ms);

        Bitmap result = new(106, 72);
        using var graphics = Graphics.FromImage(result);
        graphics.DrawImage(image, 0, 0, 106, 72);

        var map = Map.OrderBy(kvp => kvp.Key).ToDictionary();
        StringBuilder sb = new();

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Color pixelValue = result.GetPixel(x, y);
                string currentChar = MapToAscii(map, pixelValue);
                sb.Append(currentChar);
            }

            sb.Append(Environment.NewLine);
        }

        return PostProcessOutput(sb);
    }

    private static string MapToAscii(Dictionary<float, string> map, Color pixelValue)
    {
        string currentChar = string.Empty;

        foreach (var (key, value) in map)
        {
            if (key <= pixelValue.R)
            {
                currentChar = value;
            }
        }
        
        return currentChar;
    }

    private static string PostProcessOutput(StringBuilder sb)
    {
        string output = sb.ToString();
        sb.Clear();
        return output[..^Environment.NewLine.Length];
    }
}
