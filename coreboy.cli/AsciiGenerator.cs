﻿using System.Text;
using SkiaSharp;

namespace coreboy.cli;

public abstract class AsciiGenerator
{
	private static readonly Dictionary<float, string> Map = new()
	{
		{000, "\u2588"}, // █
		{020, "\u2593"}, // ▓
		{040, "\u2592"}, // ▒
		{060, "\u2591"}, // ░
		{080, "\u0025"}, // %
		{100, "\u0040"}, // @
		{110, "\u0023"}, // #
		{120, "\u002B"}, // +
		{130, "\u004F"}, // O
		{140, "\u006F"}, // o
		{150, "\u002E"}, // .
		{160, "\u0020"}, // [space]
		{170, "\u0020"}, // [space]
		{180, "\u0020"}, // [space]
		{190, "\u0020"}, // [space]
		{200, "\u0020"}, // [space]
	};

	public static string GenerateFrame(byte[] frameBytes)
	{
		using SKBitmap bitmap = SKBitmap.Decode(frameBytes);
		using SKBitmap scaledBitmap = new(160, 72);
		SKSamplingOptions samplingOpts = new(SKFilterMode.Nearest, SKMipmapMode.None);
		bitmap.ScalePixels(scaledBitmap, samplingOpts);

		StringBuilder sb = new();

		for (int y = 0; y < scaledBitmap.Height; y++)
		{
			for (int x = 0; x < scaledBitmap.Width; x++)
			{
				byte pixelVal = scaledBitmap.GetPixel(x, y).Red;
				string currentChar = MapToAscii(Map, pixelVal);
				sb.Append(currentChar);
			}

			sb.Append(Environment.NewLine);
		}

		return ProcessOutput(sb);
	}

	private static string MapToAscii(Dictionary<float, string> map, byte pixelVal)
	{
		string? mappedChar = map.LastOrDefault(kvp => kvp.Key <= pixelVal).Value;
		if (string.IsNullOrEmpty(mappedChar))
		{
			return "\u0020";
		}
		return mappedChar;
	}

	private static string ProcessOutput(StringBuilder sb)
	{
		string output = sb.ToString();
		sb.Clear();
		return output[..^Environment.NewLine.Length];
	}
}
