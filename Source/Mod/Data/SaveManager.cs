﻿namespace Celeste64.Mod.Data;

internal sealed class SaveManager
{
	internal static SaveManager Instance = new();

	[DisallowHooks]
	internal string GetLastLoadedSave()
	{
		if (File.Exists(Path.Join(App.UserPath, "save.metadata")))
			return File.ReadAllText(Path.Join(App.UserPath, "save.metadata"));
		else
		{
			File.WriteAllText(Path.Join(App.UserPath, "save.metadata"), Save.DefaultFileName);
			return Save.DefaultFileName;
		}
	}

	[DisallowHooks]
	internal void SetLastLoadedSave(string save_name)
	{
		if (File.Exists(Path.Join(App.UserPath, "save.metadata")))
			File.WriteAllText(Path.Join(App.UserPath, "save.metadata"), save_name);
	}

	[DisallowHooks]
	internal List<string> GetSaves()
	{
		List<string> saves = new List<string>();

		foreach (string savefile in Directory.GetFiles(App.UserPath))
		{
			var saveFileName = Path.GetFileName(savefile);
			if (saveFileName.EndsWith(".json") && saveFileName.StartsWith("save"))
				saves.Add(saveFileName);
		}

		return saves;
	}

	[DisallowHooks]
	internal void CopySave(string filename)
	{
		if (File.Exists(Path.Join(App.UserPath, filename)))
		{
			string new_file_name = $"{filename.Split(".json")[0]}(copy).json";
			File.Copy(Path.Join(App.UserPath, filename), Path.Join(App.UserPath, new_file_name));
		}
	}

	[DisallowHooks]
	internal void NewSave(string? name = null)
	{
		if (string.IsNullOrEmpty(name)) name = $"save_{GetSaveCount()}.json";
		var savePath = Path.Join(App.UserPath, name);
		var tempPath = Path.Join(App.UserPath, name + ".backup");

		// first save to a temporary file
		{
			using var stream = File.Create(tempPath);
			Save.Instance.Serialize(stream, new Save_V02());
			stream.Flush();
		}

		// validate that the temp path worked, and overwrite existing if it did.
		if (File.Exists(tempPath) && Save.Instance.Deserialize<Save_V02>(File.ReadAllText(tempPath)) != null)
		{
			File.Copy(tempPath, savePath, true);
		}
	}

	[DisallowHooks]
	internal int GetSaveCount()
	{
		return GetSaves().Count;
	}

	[DisallowHooks]
	internal void DeleteSave(string save)
	{
		if (File.Exists(Path.Join(App.UserPath, save)))
		{
			File.Delete(Path.Join(App.UserPath, save));
		}

		if (save == Save.DefaultFileName)
		{
			NewSave(Save.DefaultFileName);
		}
	}

	[DisallowHooks]
	internal void LoadSaveByFileName(string fileName)
	{
		Save.LoadSaveByFileName(fileName);
		Instance.SetLastLoadedSave(fileName);
	}
}
