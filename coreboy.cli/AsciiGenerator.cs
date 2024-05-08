using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace coreboy.cli;

public abstract class AsciiGenerator
{
	private static readonly Dictionary<float, string> Map = new()
	{
		{000, "█"},
		{020, "▓"},
		{040, "▒"},
		{060, "░"},
		{080, "%"},
		{100, "@"},
		{110, "#"},
		{120, "+"},
		{130, "O"},
		{140, "o"},
		{150, "."},
		{160, " "},
		{170, " "},
		{180, " "},
		{190, " "},
		{200, " "}
	};

	public static string GenerateFrame(byte[] frameBytes)
	{
		using var image = Image.Load<Rgba32>(frameBytes);
		image.Mutate(x => x.Resize(160, 72));

		StringBuilder sb = new();

		for (int y = 0; y < image.Height; y++)
		{
			for (int x = 0; x < image.Width; x++)
			{
				var pixelVal = image[x, y];
				string currentChar = MapToAscii(Map, pixelVal);
				sb.Append(currentChar);
			}
			
			sb.Append(Environment.NewLine);
		}

		return ProcessOutput(sb);
	}

	private static string MapToAscii(Dictionary<float, string> map, Rgba32 pixelVal)
	{
		string currentChar = string.Empty;

		foreach ((float key, string value) in map)
		{
			if (key <= pixelVal.R)
			{
				currentChar = value;
			}
		}

		return currentChar;
	}

	private static string ProcessOutput(StringBuilder sb)
	{
		string output = sb.ToString();
		sb.Clear();
		return output[..^Environment.NewLine.Length];
	}
}
