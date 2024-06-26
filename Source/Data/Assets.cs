﻿using Celeste64.Mod;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Celeste64;

public static class Assets
{
	public static float FontSize => Game.RelativeScale * 16;
	public const string AssetFolder = "Content";

	public const string MapsFolder = "Maps";
	public const string MapsExtension = "map";

	public const string TexturesFolder = "Textures";
	public const string TexturesExtension = "png";

	public const string FacesFolder = "Faces";
	public const string FacesExtension = "png";

	public const string ModelsFolder = "Models";
	public const string ModelsExtension = "glb";

	public const string TextFolder = "Text";
	public const string TextExtension = "json";

	public const string AudioFolder = "Audio";
	public const string AudioExtension = "bank";

	public const string SoundsFolder = "Sounds";
	public const string SoundsExtension = "wav";

	public const string MusicFolder = "Music";
	public const string MusicExtension = "wav";

	public const string ShadersFolder = "Shaders";
	public const string ShadersExtension = "glsl";

	public const string FontsFolder = "Fonts";
	public const string FontsExtensionTTF = "ttf";
	public const string FontsExtensionOTF = "otf";

	public const string SpritesFolder = "Sprites";
	public const string SpritesExtension = "png";

	public const string SkinsFolder = "Skins";
	public const string SkinsExtension = "json";

	public const string LibrariesFolder = "Libraries";
	public const string LibrariesExtensionAssembly = "dll";
	public const string LibrariesExtensionSymbol = "pdb";

	public const string FujiJSON = "Fuji.json";
	public const string LevelsJSON = "Levels.json";

	private static string? contentPath = null;

	public static string ContentPath
	{
		get
		{
			if (contentPath == null)
			{
				var baseFolder = AppContext.BaseDirectory;
				var searchUpPath = "";
				int up = 0;
				while (!Directory.Exists(Path.Join(baseFolder, searchUpPath, AssetFolder)) && up++ < 6)
					searchUpPath = Path.Join(searchUpPath, "..");
				if (!Directory.Exists(Path.Join(baseFolder, searchUpPath, AssetFolder)))
					throw new Exception($"Unable to find {AssetFolder} Directory from '{baseFolder}'");
				contentPath = Path.Join(baseFolder, searchUpPath, AssetFolder);
			}

			return contentPath;
		}
	}

	public static readonly ModAssetDictionary<Map> Maps = new(gameMod => gameMod.Maps);
	public static readonly ModAssetDictionary<Shader> Shaders = new(gameMod => gameMod.Shaders);
	public static readonly ModAssetDictionary<Texture> Textures = new(gameMod => gameMod.Textures);
	public static readonly ModAssetDictionary<SkinnedTemplate> Models = new(gameMod => gameMod.Models);
	public static readonly ModAssetDictionary<Subtexture> Subtextures = new(gameMod => gameMod.Subtextures);
	public static readonly ModAssetDictionary<Font> Fonts = new(gameMod => gameMod.Fonts);
	public static readonly ModAssetDictionary<FMOD.Sound> Sounds = new(gameMod => gameMod.Sounds);
	public static readonly ModAssetDictionary<FMOD.Sound> Music = new(gameMod => gameMod.Music);
	public static readonly Dictionary<string, Language> Languages = new(StringComparer.OrdinalIgnoreCase);

	public static List<SkinInfo> EnabledSkins =>
		ModManager.Instance.EnabledMods
			.Where(mod => mod.Loaded)
			.SelectMany(mod => mod.Skins)
			.Where(skin => skin.IsUnlocked())
			.ToList();

	public static List<LevelInfo> Levels { get; private set; } = [];

	internal static Queue<GameMod> LoadQueue = [];

	/// <summary>
	/// Load a mod's assets.
	/// </summary>
	/// <param name="mod">The mod to load</param>
	internal static void LoadAssetsForMod(GameMod mod)
	{
		var maps = new ConcurrentBag<Map>();
		var images = new ConcurrentBag<(string, Image)>();
		var models = new ConcurrentBag<(string, SkinnedTemplate)>();
		var sounds = new ConcurrentBag<(string, FMOD.Sound)>();
		var music = new ConcurrentBag<(string, FMOD.Sound)>();
		var langs = new ConcurrentBag<Language>();
		var tasks = new List<Task>();

		Log.Info($"Loading assets for {mod.ModInfo.Id}");

		IModFilesystem modFs = mod.Filesystem;

		if (modFs == null)
		{
			Log.Error($"Failed to load assets for {mod.ModInfo.Id}. Mod FileSystem not initialized.");
			return;
		}

		// Load maps
		foreach (var file in modFs.FindFilesInDirectoryRecursive(MapsFolder, MapsExtension))
		{
			// Skip the "autosave" folder
			if (file.StartsWith($"{MapsFolder}/autosave", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			tasks.Add(Task.Run(() =>
			{
				if (modFs.TryOpenFile(file,
						stream => new Map(GetResourceNameFromVirt(file, MapsFolder), file, stream), out var map))
				{
					maps.Add(map);
				}
			}));
		}

		// Load textures
		foreach (var file in modFs.FindFilesInDirectoryRecursive(TexturesFolder, TexturesExtension))
		{
			tasks.Add(Task.Run(() =>
			{
				if (modFs.TryLoadImage(file, out var image))
				{
					images.Add((GetResourceNameFromVirt(file, TexturesFolder), image));
				}
			}));
		}

		// Load character faces
		foreach (var file in modFs.FindFilesInDirectoryRecursive(FacesFolder, FacesExtension))
		{
			tasks.Add(Task.Run(() =>
			{
				if (modFs.TryLoadImage(file, out var image))
				{
					var name = $"faces/{GetResourceNameFromVirt(file, FacesFolder)}";
					images.Add((name, image));
				}
			}));
		}

		// Load glb models
		foreach (var file in modFs.FindFilesInDirectoryRecursive(ModelsFolder, ModelsExtension))
		{
			tasks.Add(Task.Run(() =>
			{
				if (modFs.TryOpenFile(file, stream => SharpGLTF.Schema2.ModelRoot.ReadGLB(stream), out var input))
				{
					var model = new SkinnedTemplate(input);
					models.Add((GetResourceNameFromVirt(file, ModelsFolder), model));
				}
			}));
		}

		// Load language files
		foreach (var file in modFs.FindFilesInDirectoryRecursive(TextFolder, TextExtension))
		{
			tasks.Add(Task.Run(() =>
			{
				if (modFs.TryLoadText(file, out var data))
				{
					if (JsonSerializer.Deserialize(data, LanguageContext.Default.Language) is { } lang)
					{
						langs.Add(lang);
					}
				}
			}));
		}

		// Load wav sounds - Fuji Custom
		foreach (var file in modFs.FindFilesInDirectoryRecursive(SoundsFolder, SoundsExtension))
		{
			tasks.Add(Task.Run(() =>
			{
				if (modFs.TryOpenFile(file, Audio.LoadWavFromStream, out var sound))
				{
					if (sound != null)
					{
						sounds.Add((GetResourceNameFromVirt(file, SoundsFolder), sound.Value));
					}
				}
			}));
		}

		// Load wav music - Fuji Custom
		foreach (var file in modFs.FindFilesInDirectoryRecursive(MusicFolder, MusicExtension))
		{
			tasks.Add(Task.Run(() =>
			{
				if (modFs.TryOpenFile(file, Audio.LoadWavFromStream, out var song))
				{
					if (song != null)
					{
						music.Add((GetResourceNameFromVirt(file, MusicFolder), song.Value));
					}
				}
			}));
		}

		// Load FMOD audio banks
		var allBankFiles = modFs.FindFilesInDirectoryRecursive(AudioFolder, AudioExtension).ToList();
		// load strings first
		foreach (var file in allBankFiles)
		{
			if (file.EndsWith($".strings.{AudioExtension}"))
				modFs.TryOpenFile(file, Audio.LoadBankFromStream);
		}
		// load banks second
		foreach (var file in allBankFiles)
		{
			if (file.EndsWith($".{AudioExtension}") && !file.EndsWith($".strings.{AudioExtension}"))
				modFs.TryOpenFile(file, Audio.LoadBankFromStream);
		}

		// load glsl shaders
		foreach (var file in modFs.FindFilesInDirectoryRecursive(ShadersFolder, ShadersExtension))
		{
			if (modFs.TryOpenFile(file, stream => LoadShader(file, stream), out var shader))
			{
				shader.Name = GetResourceNameFromVirt(file, ShadersFolder);
				Shaders.Add(shader.Name, shader, mod);
			}
		}

		// load font files
		foreach (var file in modFs.FindFilesInDirectoryRecursive(FontsFolder, ""))
		{
			if (file.EndsWith($".{FontsExtensionTTF}") || file.EndsWith($".{FontsExtensionOTF}"))
			{
				if (modFs.TryOpenFile(file, stream => new Font(stream), out var font))
					Fonts.Add(GetResourceNameFromVirt(file, FontsFolder), font, mod);
			}
		}

		// load levels
		mod.Levels.Clear();
		if (modFs.TryOpenFile(LevelsJSON,
				stream => JsonSerializer.Deserialize(stream, LevelInfoListContext.Default.ListLevelInfo) ?? [],
				out var levels))
		{
			foreach (LevelInfo level in levels) // Assign the mod id to level infos
			{
				level.ModId = mod.ModInfo.Id;
			}
			mod.Levels.AddRange(levels);
			Levels.AddRange(levels);
		}

		// pack sprites into single texture
		{
			var packer = new Packer
			{
				Trim = false,
				CombineDuplicates = false,
				Padding = 1
			};
			foreach (var file in modFs.FindFilesInDirectoryRecursive(SpritesFolder, SpritesExtension))
			{
				if (modFs.TryOpenFile(file, stream => new Image(stream), out var img))
				{
					packer.Add($"{mod.ModInfo.Id}:{GetResourceNameFromVirt(file, SpritesFolder)}", img);
				}
			}

			var result = packer.Pack();
			var pages = new List<Texture>();
			foreach (var it in result.Pages)
			{
				it.Premultiply();
				pages.Add(new Texture(it));
			}

			foreach (var it in result.Entries)
			{
				string[] nameSplit = it.Name.Split(':');
				Subtextures.Add(nameSplit[1], new Subtexture(pages[it.Page], it.Source, it.Frame), mod);
			}
		}

		// Load Skins
		foreach (var file in modFs.FindFilesInDirectoryRecursive(SkinsFolder, SkinsExtension))
		{
			if (modFs.TryOpenFile(file,
					stream => JsonSerializer.Deserialize(stream, SkinInfoContext.Default.SkinInfo), out var skin) && skin.IsValid())
			{
				mod.Skins.Add(skin);
			}
			else
			{
				Log.Warning($"Improperly configured skin: {file}");
			}
		}

		// wait for tasks to finish before adding them.
		{
			foreach (var task in tasks)
			{
				task.Wait();
			}

			foreach (var (name, img) in images)
				Textures.Add(name, new Texture(img) { Name = name }, mod);
			foreach (var map in maps)
				Maps.Add(map.Name, map, mod);
			foreach (var (name, sound) in sounds)
				Sounds.Add(name, sound, mod);
			foreach (var (name, song) in music)
				Music.Add(name, song, mod);
			foreach (var (name, model) in models)
			{
				model.ConstructResources();
				Models.Add(name, model, mod);
			}
			foreach (var lang in langs)
			{
				if (Languages.TryGetValue(lang.ID, out var existing))
				{
					existing.Absorb(lang, mod);
				}
				else
				{
					lang.OnCreate(mod);
					Languages.Add(lang.ID, lang);
				}
			}
		}
	}

	/// <summary>
	/// Load the vanilla mod.
	/// </summary>
	internal static void LoadVanillaMod()
	{
		GameMod? vanilla = ModManager.Instance.VanillaGameMod;

		if (vanilla is null)
		{
			throw new Exception("Vanilla mod does not exist. This means something went horribly wrong!");
		}

		LoadAssetsForMod(vanilla);

		// make sure the active language is ready for use
		Language.Current.Use();
	}

	/// <summary>
	/// Unload currently loaded assets.
	/// If a GameMod is passed, unload assets for that mod.
	/// Otherwise, unload all assets.
	/// </summary>
	internal static void Unload(GameMod? mod)
	{
		if (mod == null) { Levels.Clear(); }
		else
		{
			Levels = Levels.Where((LevelInfo levelInfo) => { return levelInfo.ModId != mod.ModInfo.Id; }).ToList();
		}

		Maps.Clear(mod);
		Shaders.Clear(mod);
		Textures.Clear(mod);
		Subtextures.Clear(mod);
		Models.Clear(mod);
		Fonts.Clear(mod);
		Sounds.Clear(mod);
		Music.Clear(mod);
		if (mod == null) Languages.Clear(); // Language files should upsert safely
		if (mod == null) Audio.Unload(); // I don't have the patience to figure this out right now.

		if (mod == null) { Map.ModActorFactories.Clear(); }
		else
		/*
		 https://stackoverflow.com/a/2131680
		 Remove mod actor factories owned by the specified mod.
		*/
		{
			foreach (KeyValuePair<string, Map.ActorFactory> kvp in Map.ModActorFactories.Where(
				(kvp) => { return kvp.Value.Mod == mod; }
			).ToList())
			{
				Map.ModActorFactories.Remove(kvp.Key);
			}
		}
	}

	/// <summary>
	/// Fills the asset load queue with all enabled mods.
	/// </summary>
	/// <returns>The new length of the load queue.</returns>
	internal static int FillLoadQueue()
	{
		LoadQueue = new Queue<GameMod>(ModManager.Instance.EnabledMods.Where(gm => gm is not VanillaGameMod));

		return LoadQueue.Count;
	}

	/// <summary>
	/// Move the load queue forward by a step, loading one mod.
	/// </summary>
	/// <returns>True if a mod exists and was loaded, false if there's nothing left in the queue</returns>
	internal static bool MoveLoadQueue()
	{
		if (!LoadQueue.Any())
		{
			return false;
		}

		LoadAssetsForMod(LoadQueue.First());
		LoadQueue.Dequeue();

		return true;
	}

	/// <summary>
	/// Loads every mod in the queue.
	/// </summary>
	internal static void LoadAllQueued()
	{
		while (LoadQueue.Any())
		{
			MoveLoadQueue();
		}
	}

	/* 
		Asset loading was redone in 0.7.0. However, this function is preserved here.
		For compatibility and ease of use, it has the exact same behaviour on the outside as before.
	*/
	/// <summary>
	/// All-in-one function to purge assets, then re-register all mods, then load assets.
	/// </summary>
	public static void Load()
	{
		var timer = Stopwatch.StartNew();

		// Purge any existing assets...
		Unload(null);

		/*
			Refresh our instance of the vanilla mod.
			Did you know that if vanilla isn't the first mod, trying to load assets throws a cryptic error?
			Me neither, until recently.
		*/
		ModLoader.CreateVanillaMod();

		ModLoader.RegisterAllMods();

		// Load vanilla assets first
		LoadVanillaMod();

		FillLoadQueue();

		// NOTE: Make sure to update ModManager.OnModFileChanged() as well, for hot-reloading to work!

		// Go through all of the mods in queue and load them
		LoadAllQueued();

		ModManager.Instance.OnAssetsLoaded();

		Log.Info($"Loaded Assets in {timer.ElapsedMilliseconds}ms");
	}

	/// <summary>
	/// Convert a virtual path into a real path
	/// </summary>
	/// <param name="virtPath">The virtual path</param>
	/// <param name="folder">The folder type</param>
	/// <returns>A real path</returns>
	private static string GetResourceNameFromVirt(string virtPath, string folder)
	{
		var ext = Path.GetExtension(virtPath);
		// +1 to account for the forward slash
		return virtPath.AsSpan((folder.Length + 1)..^ext.Length).ToString();
	}

	/// <summary>
	/// Loads a shader from a file stream.
	/// </summary>
	/// <param name="virtPath">The Virtual Path to the file. Used if the shader includes things from other files.</param>
	/// <param name="file">The File Stream for the file.</param>
	/// <returns>The shader that was loaded.</returns>
	/// <exception cref="Exception">Throws if we try to include something that doesn't exist.</exception>
	private static Shader? LoadShader(string virtPath, Stream file)
	{
		using var reader = new StreamReader(file);
		var code = reader.ReadToEnd();

		StringBuilder vertex = new();
		StringBuilder fragment = new();
		StringBuilder? target = null;

		foreach (var l in code.Split('\n'))
		{
			var line = l.Trim('\r');

			if (line.StartsWith("VERTEX:"))
				target = vertex;
			else if (line.StartsWith("FRAGMENT:"))
				target = fragment;
			else if (line.StartsWith("#include"))
			{
				var path = $"{Path.GetDirectoryName(virtPath)}/{line[9..]}";

				if (ModManager.Instance.GlobalFilesystem.TryLoadText(path, out var include))
					target?.Append(include);
				else
					throw new Exception($"Unable to find shader include: '{path}'");
			}
			else
				target?.AppendLine(line);
		}

		return new Shader(new(
			vertexShader: vertex.ToString(),
			fragmentShader: fragment.ToString()
		));
	}
}
