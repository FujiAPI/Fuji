using Celeste64.Mod;
using Celeste64.Mod.Data;
using System.Diagnostics;

namespace Celeste64;

/// <summary>
/// Creates a slight delay so the window looks OK before we load Assets
/// </summary>
public class Startup : Scene
{
	private int assetQueueSize;
	private int queueIndex = 0;
	private bool areModsRegistered = false;
	private string lastLoadedModName = string.Empty;
	private int delay = 5;
	private Stopwatch timer;

	public Startup()
	{
		timer = Stopwatch.StartNew();

		// Register vanilla mod so that it can load its assets
		// Assume this will be overridden later
		ModLoader.CreateVanillaMod();
		if (ModManager.Instance.VanillaGameMod is not null)
		{
			ModManager.Instance.RegisterMod(ModManager.Instance.VanillaGameMod);
		}

		// load save file
		{
			SaveManager.Instance.LoadSaveByFileName(SaveManager.Instance.GetLastLoadedSave());
		}

		// load settings file
		{
			Settings.LoadSettingsByFileName(Settings.DefaultFileName);
		}

		// load mod settings file
		{
			ModSettings.LoadModSettingsByFileName(ModSettings.DefaultFileName);
		}

		// load vanilla assets
		Assets.LoadVanillaMod();

		// try to load controls, or overwrite with defaults if they don't exist
		{
			Controls.LoadControlsFromFile(Controls.DefaultFileName);
		}
	}
  
	public override void Update()
	{
		if (delay > 0)
		{
			delay--;
			return;
		}

		if (!areModsRegistered)
		{
			// this also loads mods, which get their saved settings from the save file.
			ModLoader.RegisterAllMods();
			assetQueueSize = Assets.FillLoadQueue();
			areModsRegistered = true;
			return;
		}

		// load assets
		lastLoadedModName = Assets.LoadQueue.Any() ?
			(Assets.LoadQueue.First().ModInfo.Name ?? Assets.LoadQueue.First().ModInfo.Id)
			: string.Empty;
		bool finishedLoading = !Assets.MoveLoadQueue();
		queueIndex++;
		/*
			We introduce a tiny bit of delay after each mod so the game has time to render to the screen.
			This makes the loading screen look less choppy overall. Since the delay is so small, any impact 
			it has on speed should be negligible.
		*/
		delay = 2;

		if (finishedLoading && !Game.Instance.IsMidTransition)
		{
			App.VSync = Settings.VSync;

			Log.Info($"Loaded Assets in {timer.ElapsedMilliseconds}ms");
			ModManager.Instance.OnAssetsLoaded();

			// enter game
			if (Settings.EnableQuickStart && Input.Keyboard.CtrlOrCommand)
			{
				var entry = new Overworld.Entry(Assets.Levels[0], null);
				entry.Level.Enter();
			}
			else
			{
				Game.Instance.Goto(new Transition()
				{
					Mode = Transition.Modes.Replace,
					Scene = () => new Titlescreen(),
					ToBlack = null,
					FromBlack = new AngledWipe(),
				});
			}
		}
	}

	public override void Render(Target target)
	{
		target.Clear(Color.FromHexStringRGB("#282c42"));

		Batcher batcher = new();
		Rect bounds = new(0, 0, target.Width, target.Height);

		string loadInfo;

		if (!areModsRegistered)
		{
			loadInfo = Loc.Str("FujiLoaderStatusRegistering");
		}
		else
		{
			loadInfo = String.Format(Loc.Str("FujiLoaderStatusNormal"), lastLoadedModName, queueIndex, assetQueueSize);
		}
		if (Assets.Textures.TryGetValue("overworld/splashscreen", out Texture? splashTexture))
		{
			batcher.Image(splashTexture, bounds.TopLeft, bounds.TopRight, bounds.BottomRight, bounds.BottomLeft, new Vec2(0, 0), new Vec2(1, 0), new Vec2(1, 1), new Vec2(0, 1), Color.White);
		}

		UI.Text(batcher, loadInfo, bounds.BottomLeft + new Vec2(4 * Game.RelativeScale, -28 * Game.RelativeScale), Vec2.Zero, Color.White);

		batcher.PushMatrix(Matrix3x2.CreateScale(0.75f));
		UI.Text(batcher, Game.Instance.GetFullVersionString(), bounds.TopLeft + new Vec2(4 * Game.RelativeScale, 4 * Game.RelativeScale), Vec2.Zero, Color.LightGray);
		batcher.PopMatrix();

		if (areModsRegistered && assetQueueSize > 0)
		{
			batcher.Rect(new Rect(0, bounds.Bottom - (6 * Game.RelativeScale), target.Width / assetQueueSize * queueIndex, bounds.Bottom), Color.White); // Progress bar
		}

		batcher.Render(target);
		batcher.Clear();
	}
}
