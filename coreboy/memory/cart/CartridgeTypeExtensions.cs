using System.Text.RegularExpressions;

namespace coreboy.memory.cart;

public static class CartridgeTypeExtensions
{
	public static IEnumerable<CartridgeType> Values(this CartridgeType src)
	{
		return Enum.GetValues(typeof(CartridgeType)).Cast<CartridgeType>();
	}

	public static bool IsMbc1(this CartridgeType src)
	{
		return src.NameContainsSegment("MBC1");
	}

	public static bool IsMbc2(this CartridgeType src)
	{
		return src.NameContainsSegment("MBC2");
	}

	public static bool IsMbc3(this CartridgeType src)
	{
		return src.NameContainsSegment("MBC3");
	}

	public static bool IsMbc5(this CartridgeType src)
	{
		return src.NameContainsSegment("MBC5");
	}

	public static bool IsMmm01(this CartridgeType src)
	{
		return src.NameContainsSegment("MMM01");
	}

	public static bool IsRam(this CartridgeType src)
	{
		return src.NameContainsSegment("RAM");
	}

	public static bool IsSram(this CartridgeType src)
	{
		return src.NameContainsSegment("SRAM");
	}

	public static bool IsTimer(this CartridgeType src)
	{
		return src.NameContainsSegment("TIMER");
	}

	public static bool IsBattery(this CartridgeType src)
	{
		return src.NameContainsSegment("BATTERY");
	}

	public static bool IsRumble(this CartridgeType src)
	{
		return src.NameContainsSegment("RUMBLE");
	}

	private static bool NameContainsSegment(this CartridgeType src, string segment)
	{
		return new Regex("(^|_)" +
			Regex.Escape(segment) + "($|_)").IsMatch(src.ToString());
	}

	public static CartridgeType GetById(int id)
	{
		return (CartridgeType)id;
	}
}
