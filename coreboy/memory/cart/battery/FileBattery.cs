#nullable disable

using Newtonsoft.Json;

namespace coreboy.memory.cart.battery;

public class FileBattery(string romName) : IBattery
{
	private readonly FileInfo _saveFile = new($"{romName}.sav.json");

	public void LoadRam(int[] ram)
	{
		if (!_saveFile.Exists)
		{
			return;
		}

		var loaded = JsonConvert.DeserializeObject<SaveState>(
			File.ReadAllText(_saveFile.FullName));
		loaded.Ram.CopyTo(ram, 0);
	}

	public void LoadRamWithClock(int[] ram, long[] clockData)
	{
		if (!_saveFile.Exists)
		{
			return;
		}

		var loaded = JsonConvert.DeserializeObject<SaveState>(
			File.ReadAllText(_saveFile.FullName));
		loaded.Ram.CopyTo(ram, 0);
		loaded.ClockData.CopyTo(clockData, 0);
	}

	public void SaveRam(int[] ram)
	{
		SaveRamWithClock(ram, null);
	}

	public void SaveRamWithClock(int[] ram, long[] clockData)
	{
		SaveState dto = new() { Ram = ram, ClockData = clockData };
		string asText = JsonConvert.SerializeObject(dto);
		File.WriteAllText(_saveFile.FullName, asText);
	}

	public class SaveState
	{
		public int[] Ram { get; set; }
		public long[] ClockData { get; set; }
	}
}
