using Celeste64.Mod;
using System.Reflection;

namespace Celeste64;

[DisallowHooks]
public static class Controls
{
	#region Default Controls
	[DefaultStickBinding(StickDirection.Up, Keys.Up)]
	[DefaultStickBinding(StickDirection.Up, Buttons.Up)]
	[DefaultStickBinding(StickDirection.Up, Axes.LeftY, 0.0f, true)]
	[DefaultStickBinding(StickDirection.Down, Keys.Down)]
	[DefaultStickBinding(StickDirection.Down, Buttons.Down)]
	[DefaultStickBinding(StickDirection.Down, Axes.LeftY, 0.0f, false)]
	[DefaultStickBinding(StickDirection.Left, Keys.Left)]
	[DefaultStickBinding(StickDirection.Left, Buttons.Left)]
	[DefaultStickBinding(StickDirection.Left, Axes.LeftX, 0.0f, true)]
	[DefaultStickBinding(StickDirection.Right, Keys.Right)]
	[DefaultStickBinding(StickDirection.Right, Buttons.Right)]
	[DefaultStickBinding(StickDirection.Right, Axes.LeftX, 0.0f, false)]
	public static VirtualStick Move { get; private set; } = new("Move", VirtualAxis.Overlaps.TakeNewer, 0.35f);

	[DefaultStickBinding(StickDirection.Up, Keys.Up)]
	[DefaultStickBinding(StickDirection.Up, Buttons.Up)]
	[DefaultStickBinding(StickDirection.Up, Axes.LeftY, 0.50f, true)]
	[DefaultStickBinding(StickDirection.Down, Keys.Down)]
	[DefaultStickBinding(StickDirection.Down, Buttons.Down)]
	[DefaultStickBinding(StickDirection.Down, Axes.LeftY, 0.50f, false)]
	[DefaultStickBinding(StickDirection.Left, Keys.Left)]
	[DefaultStickBinding(StickDirection.Left, Buttons.Left)]
	[DefaultStickBinding(StickDirection.Left, Axes.LeftX, 0.50f, true)]
	[DefaultStickBinding(StickDirection.Right, Keys.Right)]
	[DefaultStickBinding(StickDirection.Right, Buttons.Right)]
	[DefaultStickBinding(StickDirection.Right, Axes.LeftX, 0.50f, false)]
	public static VirtualStick Menu { get; private set; } = new("Menu", VirtualAxis.Overlaps.TakeNewer, 0.35f);

	[DefaultStickBinding(StickDirection.Up, Keys.W)]
	[DefaultStickBinding(StickDirection.Up, Axes.RightY, 0.0f, true)]
	[DefaultStickBinding(StickDirection.Down, Keys.S)]
	[DefaultStickBinding(StickDirection.Down, Axes.RightY, 0.0f, false)]
	[DefaultStickBinding(StickDirection.Left, Keys.A)]
	[DefaultStickBinding(StickDirection.Left, Axes.RightX, 0.0f, true)]
	[DefaultStickBinding(StickDirection.Right, Keys.D)]
	[DefaultStickBinding(StickDirection.Right, Axes.RightX, 0.0f, false)]
	public static VirtualStick Camera { get; private set; } = new("Camera", VirtualAxis.Overlaps.TakeNewer, 0.35f);

	[DefaultBinding(Keys.C)]
	[DefaultBinding(Buttons.South)]
	[DefaultBinding(Buttons.North)]
	public static VirtualButton Jump { get; private set; } = new("Jump", .1f);

	[DefaultBinding(Keys.X)]
	[DefaultBinding(Buttons.West)]
	[DefaultBinding(Buttons.East)]
	public static VirtualButton Dash { get; private set; } = new("Dash", .1f);

	[DefaultBinding(Keys.Z)]
	[DefaultBinding(Keys.V)]
	[DefaultBinding(Keys.LeftShift)]
	[DefaultBinding(Keys.RightShift)]
	[DefaultBinding(Buttons.LeftShoulder)]
	[DefaultBinding(Buttons.RightShoulder)]
	[DefaultBinding(Axes.LeftTrigger, 0.4f, false)]
	[DefaultBinding(Axes.RightTrigger, 0.4f, false)]
	public static VirtualButton Climb { get; private set; } = new("Climb");

	[DefaultBinding(Keys.C)]
	[DefaultBinding(Buttons.South, [Gamepads.DualSense, Gamepads.DualShock4, Gamepads.Xbox])]
	[DefaultBinding(Buttons.East, [Gamepads.Nintendo])]
	public static VirtualButton Confirm { get; private set; } = new("Confirm");

	[DefaultBinding(Keys.X)]
	[DefaultBinding(Buttons.East, [Gamepads.DualSense, Gamepads.DualShock4, Gamepads.Xbox])]
	[DefaultBinding(Buttons.South, [Gamepads.Nintendo])]
	public static VirtualButton Cancel { get; private set; } = new("Cancel");

	[DefaultBinding(Keys.Escape)]
	[DefaultBinding(Keys.Enter)]
	[DefaultBinding(Buttons.Start)]
	[DefaultBinding(Buttons.Select)]
	[DefaultBinding(Buttons.Back)]
	public static VirtualButton Pause { get; private set; } = new("Pause");

	[DefaultBinding(Keys.V)]
	[DefaultBinding(Buttons.LeftShoulder)]
	public static VirtualButton CopyFile { get; private set; } = new("CopyFile");

	[DefaultBinding(Keys.B)]
	[DefaultBinding(Buttons.North)]
	public static VirtualButton DeleteFile { get; private set; } = new("DeleteFile");

	[DefaultBinding(Keys.N)]
	[DefaultBinding(Buttons.RightShoulder)]
	public static VirtualButton CreateFile { get; private set; } = new("CreateFile");


	[DefaultBinding(Keys.R)]
	[DefaultBinding(Buttons.West)]
	public static VirtualButton RenameFile { get; private set; } = new("RenameFile");

	[DefaultBinding(Keys.V)]
	[DefaultBinding(Buttons.LeftShoulder)]
	public static VirtualButton ResetBindings { get; private set; } = new("ResetBindings");

	[DefaultBinding(Keys.B)]
	[DefaultBinding(Buttons.North)]
	public static VirtualButton ClearBindings { get; private set; } = new("ClearBindings");

	[DefaultBinding(Keys.F6)]
	public static VirtualButton DebugMenu { get; private set; } = new("DebugMenu");

	[DefaultBinding(Keys.R)]
	public static VirtualButton Restart { get; private set; } = new("Restart");

	[DefaultBinding(Keys.F4)]
	public static VirtualButton FullScreen { get; private set; } = new("FullScreen");

	[DefaultBinding(Keys.F5)]
	public static VirtualButton ReloadAssets { get; private set; } = new("ReloadAssets");
	#endregion

	#region Additional Properties
	public static ControlsConfig_V01 Instance = new();

	public const string DefaultFileName = "controls.json";
	#endregion

	#region Saving and Loading

	/// <summary>
	/// Load Controls from a file
	/// </summary>
	/// <param name="file_name"></param>
	internal static void LoadControlsFromFile(string file_name)
	{
		if (file_name == string.Empty) file_name = DefaultFileName;
		var controlsFile = Path.Join(App.UserPath, file_name);

		ControlsConfig_V01? controls = null;
		if (File.Exists(controlsFile))
		{
			try
			{
				controls = Instance.Deserialize<ControlsConfig_V01>(File.ReadAllText(controlsFile)) ?? null;
			}
			catch
			{
				controls = null;
			}
		}

		if (controls == null)
		{
			controls = new ControlsConfig_V01();
		}
		Instance = controls;

		// Add any missing default sticks or buttons, if they weren't loaded from the file
		AddMissingBindings(Instance, typeof(Controls));

		LoadBindings(Instance, typeof(Controls));

		// Set up repeat intervals for menu controls
		foreach (VirtualAxis axis in new VirtualAxis[] { Menu.Horizontal, Menu.Vertical })
		{
			axis.Positive.RepeatInterval = 0.15f;
			axis.Negative.RepeatInterval = 0.15f;
		}
	}

	/// <summary>
	/// Save control bindings to a file
	/// </summary>
	internal static void SaveToFile()
	{
		var savePath = Path.Join(App.UserPath, DefaultFileName);
		var tempPath = Path.Join(App.UserPath, DefaultFileName + ".backup");

		// first save to a temporary file
		{
			using var stream = File.Create(tempPath);
			Instance.Serialize(stream, Instance);
			stream.Flush();
		}

		// validate that the temp path worked, and overwrite existing if it did.
		if (File.Exists(tempPath) &&
			Instance.Deserialize<ControlsConfig_V01>(File.ReadAllText(tempPath)) != null)
		{
			File.Copy(tempPath, savePath, true);
		}
	}
	#endregion

	/// <summary>
	/// Add any default bindings that are not currently set up.
	/// </summary>
	/// <param name="config"></param>
	/// <param name="controlsType"></param>
	/// <param name="controlsInstance"></param>
	private static void AddMissingBindings(ControlsConfig_V01 config, Type controlsType, object? controlsInstance = null)
	{
		foreach (var prop in controlsType.GetProperties())
		{
			if (prop.PropertyType == typeof(VirtualButton))
			{
				var vb = prop.GetValue(controlsInstance) as VirtualButton;
				if (vb != null && !config.Actions.ContainsKey(vb.Name))
				{
					config.Actions[vb.Name] = GetDefaultActionBindings(vb, controlsType, controlsInstance).ToList();
				}
			}
			else if (prop.PropertyType == typeof(VirtualStick))
			{
				var vs = prop.GetValue(controlsInstance) as VirtualStick;
				if (vs != null && !config.Sticks.ContainsKey(vs.Name))
				{
					config.Sticks[vs.Name] = new ControlsConfigStick()
					{
						Up = GetDefaultStickBindings(vs, controlsType, StickDirection.Up, controlsInstance).ToList(),
						Down = GetDefaultStickBindings(vs, controlsType, StickDirection.Down, controlsInstance).ToList(),
						Left = GetDefaultStickBindings(vs, controlsType, StickDirection.Left, controlsInstance).ToList(),
						Right = GetDefaultStickBindings(vs, controlsType, StickDirection.Right, controlsInstance).ToList(),
						Deadzone = vs.CircularDeadzone
					};
				}
			}
		}
	}

	/// <summary>
	/// Get default bindings for a virtual button, based on attributes.
	/// If button is from a virtual stick, get bindings from that instead.
	/// </summary>
	/// <param name="virtualButton"></param>
	/// <param name="instanceType"></param>
	/// <param name="instance"></param>
	/// <returns></returns>
	private static IEnumerable<ControlsConfigBinding> GetDefaultActionBindings(VirtualButton virtualButton, Type instanceType, object? instance = null)
	{
		string[] nameParts = virtualButton.Name.Split("/");

		if (nameParts.Length == 3)
		{
			StickDirection stickDir = StickDirection.Down;
			if (nameParts[1] == "Horizontal" && nameParts[2] == "Positive")
			{
				stickDir = StickDirection.Right;
			}
			else if (nameParts[1] == "Horizontal" && nameParts[2] == "Negative")
			{
				stickDir = StickDirection.Left;
			}
			else if (nameParts[1] == "Vertical" && nameParts[2] == "Negative")
			{
				stickDir = StickDirection.Up;
			}
			else if (nameParts[1] == "Vertical" && nameParts[2] == "Positive")
			{
				stickDir = StickDirection.Down;
			}

			var stickAttributes = instanceType
				.GetProperties()
				.FirstOrDefault(p => p.PropertyType == typeof(VirtualStick) && p.GetValue(instance) is VirtualStick vs && vs.Name == nameParts[0])?
				.GetCustomAttributes(typeof(DefaultStickBindingAttribute));
			if (stickAttributes != null)
			{
				return stickAttributes
					.Where(a => ((DefaultStickBindingAttribute)a).Direction == stickDir)
					.Select(a => ((DefaultStickBindingAttribute)a).Binding);
			}

		}
		else
		{
			var attributes = instanceType
				.GetProperties()
				.FirstOrDefault(p => p.PropertyType == typeof(VirtualButton) && p.GetValue(instance) is VirtualButton vb && vb.Name == virtualButton.Name)?
				.GetCustomAttributes(typeof(DefaultBindingAttribute));
			if (attributes != null)
			{
				var gamepad = Input.Controllers[0];

				return attributes
				.Where(a => a is DefaultBindingAttribute attr && (attr.Binding.ForGamepads == null || attr.Binding.ForGamepads.Contains(gamepad.Gamepad)))
				.Select(a => ((DefaultBindingAttribute)a).Binding);
			}
		}
		return Enumerable.Empty<ControlsConfigBinding>();
	}

	/// <summary>
	/// Get default bindings for a virtual stick.
	/// </summary>
	/// <param name="virtualStick"></param>
	/// <param name="instanceType"></param>
	/// <param name="stickDirection"></param>
	/// <param name="instance"></param>
	/// <returns></returns>
	private static IEnumerable<ControlsConfigBinding> GetDefaultStickBindings(VirtualStick virtualStick, Type instanceType, StickDirection? stickDirection = null, object? instance = null)
	{
		var attributes = instanceType
		.GetProperties()
			.FirstOrDefault(p => p.PropertyType == typeof(VirtualStick) && p.GetValue(instance) is VirtualStick vs && vs.Name == virtualStick.Name)?
			.GetCustomAttributes(typeof(DefaultStickBindingAttribute));
		if (attributes != null)
		{
			return attributes
				.Where(a => ((DefaultStickBindingAttribute)a).Direction == stickDirection)
				.Select(a => ((DefaultStickBindingAttribute)a).Binding);
		}
		return Enumerable.Empty<ControlsConfigBinding>();
	}

	/// <summary>
	/// Load bindings from a mod config.
	/// </summary>
	/// <param name="mod"></param>
	internal static void LoadModConfig(GameMod mod)
	{
		if (mod.SettingsType != null)
		{
			AddMissingBindings(mod.ModSettingsData.ModControlBindings, mod.SettingsType, mod.Settings);

			LoadBindings(mod.ModSettingsData.ModControlBindings, mod.SettingsType, mod.Settings);
		}
	}

	/// <summary>
	/// Bind a new Key to a virtual button.
	/// </summary>
	/// <param name="virtualButton"></param>
	/// <param name="key"></param>
	/// <param name="config"></param>
	internal static void AddBinding(VirtualButton virtualButton, Keys key, ControlsConfig_V01? config = null)
	{
		if (config == null)
		{
			config = Instance;
		}

		var bindings = GetButtonBindings(config, virtualButton);
		if (bindings != null && !bindings.Any(a => a.Key == key))
		{
			bindings.Add(new(key));
			bindings.Last().BindTo(virtualButton);
		}
		Consume();
	}

	/// <summary>
	/// Bind a new Controller button to a virtual button.
	/// </summary>
	/// <param name="virtualButton"></param>
	/// <param name="button"></param>
	/// <param name="config"></param>
	internal static void AddBinding(VirtualButton virtualButton, Buttons button, ControlsConfig_V01? config = null)
	{
		if (config == null)
		{
			config = Instance;
		}

		var bindings = GetButtonBindings(config, virtualButton);
		if (bindings != null && !bindings.Any(a => a.Button == button))
		{
			bindings.Add(new(button));
			bindings.Last().BindTo(virtualButton);
		}
		Consume();
	}

	/// <summary>
	/// Bind a new Mouse button to a virtual button.
	/// </summary>
	/// <param name="virtualButton"></param>
	/// <param name="mouseButton"></param>
	/// <param name="config"></param>
	internal static void AddBinding(VirtualButton virtualButton, MouseButtons mouseButton, ControlsConfig_V01? config = null)
	{
		if (config == null)
		{
			config = Instance;
		}

		var bindings = GetButtonBindings(config, virtualButton);
		if (bindings != null && !bindings.Any(a => a.MouseButton == mouseButton))
		{
			bindings.Add(new(mouseButton));
			bindings.Last().BindTo(virtualButton);
		}
		Consume();
	}

	/// <summary>
	/// Bind a new axis value to a virtual button.
	/// </summary>
	/// <param name="virtualButton"></param>
	/// <param name="axis"></param>
	/// <param name="inverted"></param>
	/// <param name="deadzone"></param>
	/// <param name="config"></param>
	internal static void AddBinding(VirtualButton virtualButton, Axes axis, bool inverted, float deadzone = 0.0f, ControlsConfig_V01? config = null)
	{
		if (config == null)
		{
			config = Instance;
		}

		var bindings = GetButtonBindings(config, virtualButton);
		if (bindings != null && !bindings.Any(a => a.Axis == axis && a.AxisInverted == inverted))
		{
			bindings.Add(new(axis, deadzone, inverted));
			bindings.Last().BindTo(virtualButton);
		}
		Consume();
	}

	/// <summary>
	/// Removes all bindings from a virtual button, unless requiresBinding is true, in which case it will keep the last one.
	/// </summary>
	/// <param name="virtualButton"></param>
	/// <param name="forController"></param>
	/// <param name="requiresBinding"></param>
	/// <param name="config"></param>
	internal static void ClearBinding(VirtualButton virtualButton, bool forController, bool requiresBinding = false, ControlsConfig_V01? config = null)
	{
		if (config == null)
		{
			config = Instance;
		}

		var bindings = GetButtonBindings(config, virtualButton);
		if (bindings != null)
		{
			virtualButton.Clear();
			// Remove all bindings for this control type
			if (requiresBinding)
			{
				var toRemove = bindings.Where(x => x.IsForController() == forController).SkipLast(1).ToList();
				foreach (var removeItem in toRemove)
				{
					bindings.Remove(removeItem);
				}
			}
			else
			{
				bindings.RemoveAll(x => x.IsForController() == forController);
			}

			// Rebind remaining bindings.
			// Needed to rebind other control type, i.e. to not lose controller binds when clearing for keyboard.
			foreach (var binding in bindings)
			{
				binding.BindTo(virtualButton);
			}
		}
	}

	/// <summary>
	/// Resets bindings for a single virtual button to their default values.
	/// </summary>
	/// <param name="virtualButton"></param>
	/// <param name="forController"></param>
	/// <param name="mod"></param>
	internal static void ResetBinding(VirtualButton virtualButton, bool forController, GameMod? mod = null)
	{
		ControlsConfig_V01 config;
		IEnumerable<ControlsConfigBinding> defaultBindings;
		if (mod != null && mod.SettingsType != null)
		{
			config = mod.ModSettingsData.ModControlBindings;
			defaultBindings = GetDefaultActionBindings(virtualButton, mod.SettingsType, mod.Settings);
		}
		else
		{
			config = Instance;
			defaultBindings = GetDefaultActionBindings(virtualButton, typeof(Controls));
		}

		var bindings = GetButtonBindings(config, virtualButton);

		if (bindings != null && defaultBindings != null)
		{
			virtualButton.Clear();

			// Remove all bindings for this control type
			bindings.RemoveAll(x => x.IsForController() == forController);

			// Readd all default bindings for this control type
			foreach (var binding in defaultBindings)
			{
				if (binding.IsForController() == forController)
				{
					bindings.Add(binding);
				}
			}

			// Rebind bindings to the virtual button.
			foreach (var binding in bindings)
			{
				binding.BindTo(virtualButton);
			}
		}
	}

	/// <summary>
	/// Resets all bindings for this control type to their default values.
	/// </summary>
	/// <param name="forController"></param>
	/// <param name="mod"></param>
	internal static void ResetAllBindings(bool forController, GameMod? mod = null)
	{
		ControlsConfig_V01 config;
		Type settingsType;
		object? settingsObject;

		if (mod != null && mod.SettingsType != null)
		{
			config = mod.ModSettingsData.ModControlBindings;
			settingsType = mod.SettingsType;
			settingsObject = mod.Settings;
		}
		else
		{
			config = Instance;
			settingsType = typeof(Controls);
			settingsObject = Instance;
		}

		// Remove all bindings for this control type
		foreach (var action in config.Actions)
		{
			config.Actions[action.Key].RemoveAll(x => x.IsForController() == forController);
		}
		foreach (var action in config.Sticks)
		{
			config.Sticks[action.Key].Up.RemoveAll(x => x.IsForController() == forController);
			config.Sticks[action.Key].Down.RemoveAll(x => x.IsForController() == forController);
			config.Sticks[action.Key].Left.RemoveAll(x => x.IsForController() == forController);
			config.Sticks[action.Key].Right.RemoveAll(x => x.IsForController() == forController);
		}

		// Readd default bindings for this control type.
		foreach (var prop in settingsType.GetProperties())
		{
			if (prop.PropertyType == typeof(VirtualButton))
			{
				var vb = prop.GetValue(settingsObject) as VirtualButton;
				if (vb != null)
				{
					config.Actions[vb.Name].AddRange(GetDefaultActionBindings(vb, settingsType, settingsObject).Where(b => b.IsForController() == forController));
				}
			}
			else if (prop.PropertyType == typeof(VirtualStick))
			{
				var vs = prop.GetValue(settingsObject) as VirtualStick;
				if (vs != null)
				{
					config.Sticks[vs.Name].Up.AddRange(GetDefaultStickBindings(vs, settingsType, StickDirection.Up, settingsObject).Where(b => b.IsForController() == forController));
					config.Sticks[vs.Name].Down.AddRange(GetDefaultStickBindings(vs, settingsType, StickDirection.Down, settingsObject).Where(b => b.IsForController() == forController));
					config.Sticks[vs.Name].Left.AddRange(GetDefaultStickBindings(vs, settingsType, StickDirection.Left, settingsObject).Where(b => b.IsForController() == forController));
					config.Sticks[vs.Name].Right.AddRange(GetDefaultStickBindings(vs, settingsType, StickDirection.Right, settingsObject).Where(b => b.IsForController() == forController));

					if (forController)
					{
						config.Sticks[vs.Name].Deadzone = vs.CircularDeadzone;
					}
				}
			}
		}

		// Reload bindings
		LoadBindings(config, settingsType, settingsObject);
	}

	/// <summary>
	/// Actually bind things to the Virtual Buttons.
	/// </summary>
	/// <param name="config"></param>
	/// <param name="controlsType"></param>
	/// <param name="controlsInstance"></param>
	internal static void LoadBindings(ControlsConfig_V01 config, Type controlsType, object? controlsInstance = null)
	{
		ClearBindingsForType(controlsType, controlsInstance);

		foreach (var prop in controlsType.GetProperties())
		{
			if (prop.PropertyType == typeof(VirtualButton))
			{
				var vb = prop.GetValue(controlsInstance) as VirtualButton;
				if (vb != null)
				{
					foreach (var it in FindAction(config, vb.Name))
						it.BindTo(vb);
				}
			}
			else if (prop.PropertyType == typeof(VirtualStick))
			{
				var vs = prop.GetValue(controlsInstance) as VirtualStick;
				if (vs != null)
				{
					FindStick(config, vs.Name).BindTo(vs);
				}
			}
		}
	}

	/// <summary>
	/// Find stick from its name
	/// </summary>
	/// <param name="config"></param>
	/// <param name="name"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	private static ControlsConfigStick FindStick(ControlsConfig_V01? config, string name)
	{
		if (config != null && config.Sticks.TryGetValue(name, out var stick))
			return stick;
		throw new Exception($"Missing Stick Binding for '{name}'");
	}

	/// <summary>
	/// Find an action binding from its name
	/// </summary>
	/// <param name="config"></param>
	/// <param name="name"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	private static List<ControlsConfigBinding> FindAction(ControlsConfig_V01? config, string name)
	{
		if (config != null && config.Actions.TryGetValue(name, out var action))
			return action;
		throw new Exception($"Missing Action Binding for '{name}'");
	}

	/// <summary>
	/// Reset virtual button bindings.
	/// </summary>
	/// <param name="controlsType"></param>
	/// <param name="controlsInstance"></param>
	internal static void ClearBindingsForType(Type controlsType, object? controlsInstance = null)
	{
		foreach (var prop in controlsType.GetProperties())
		{
			if (prop.PropertyType == typeof(VirtualButton))
			{
				var vb = prop.GetValue(controlsInstance) as VirtualButton;
				if (vb != null)
				{
					vb.Clear();
				}
			}
			else if (prop.PropertyType == typeof(VirtualStick))
			{
				var vs = prop.GetValue(controlsInstance) as VirtualStick;
				if (vs != null)
				{
					vs.Clear();
				}
			}
		}
	}

	/// <summary>
	/// Consume virtual button presses.
	/// </summary>
	public static void Consume()
	{
		foreach (var prop in typeof(Controls).GetProperties())
		{
			if (prop.PropertyType == typeof(VirtualButton))
			{
				var vb = prop.GetValue(null) as VirtualButton;
				if (vb != null)
				{
					vb.Consume();
				}
			}
			else if (prop.PropertyType == typeof(VirtualStick))
			{
				var vs = prop.GetValue(null) as VirtualStick;
				if (vs != null)
				{
					vs.Consume();
				}
			}
		}
	}

	private static readonly Dictionary<string, Dictionary<string, string>> prompts = [];

	/// <summary>
	/// Get the icon texture for the first binding of a virtual button.
	/// </summary>
	/// <param name="button">Virtual button to get an icon for.</param>
	/// <returns></returns>
	public static Subtexture GetPrompt(VirtualButton button, ControlsConfig_V01? config = null)
	{
		if (config == null)
		{
			config = Instance;
		}

		return Assets.Subtextures.GetValueOrDefault(GetPromptLocations(button, Input.Controllers.Any() && Input.Controllers[0].Connected, config).FirstOrDefault(""));
	}

	/// <summary>
	/// Get icon textures for all bindings for a virtual button.
	/// </summary>
	/// <param name="button">Virtual button to get icons for.</param>
	/// <param name="isForController">True if we should show controller icons. False to show keyboard icons.</param>
	/// <returns></returns>
	public static List<Subtexture> GetPrompts(VirtualButton button, bool isForController, ControlsConfig_V01? config = null)
	{
		if (config == null)
		{
			config = Instance;
		}

		List<Subtexture> subtextures = [];
		foreach (var location in GetPromptLocations(button, isForController, config))
			subtextures.Add(Assets.Subtextures.GetValueOrDefault(location));
		return subtextures;
	}

	private static List<string> GetPromptLocations(VirtualButton button, bool isForController, ControlsConfig_V01? config = null)
	{
		if (config == null)
		{
			config = Instance;
		}

		List<string> locations = [];
		var gamepad = Input.Controllers[0];
		var deviceTypeName =
			gamepad.Connected ? GetControllerName(gamepad.Gamepad) : "PC";

		var bindings = GetButtonBindings(config, button);

		if (bindings != null)
		{
			foreach (var binding in bindings)
			{
				if (binding.IsForController() != isForController)
				{
					continue;
				}

				string promptDeviceTypeName = "PC";
				var buttonName = binding.GetBindingName();
				if (binding.IsForController())
					promptDeviceTypeName = GetControllerName(gamepad.Gamepad);

				if (!prompts.TryGetValue(deviceTypeName, out var list))
					prompts[deviceTypeName] = list = [];

				buttonName = GetButtonOverrides(binding, buttonName, gamepad.Gamepad);

				if (!list.TryGetValue(buttonName, out var lookup))
					list[buttonName] = lookup = $"Controls/{promptDeviceTypeName}/{buttonName}";

				if (binding.ForGamepads is not Gamepads[] gamepads || !gamepads.Any() || gamepads.Contains(gamepad.Gamepad))
					locations.Add(lookup);
			}
		}

		return locations;
	}

	private static List<ControlsConfigBinding>? GetButtonBindings(ControlsConfig_V01 config, VirtualButton virtualButton)
	{
		string[] nameParts = virtualButton.Name.Split("/");

		if (nameParts.Length == 3)
		{
			var stickConfig = config.Sticks[nameParts[0]];

			if (stickConfig != null)
			{
				if (nameParts[1] == "Horizontal" && nameParts[2] == "Positive")
				{
					return stickConfig.Right;
				}
				else if (nameParts[1] == "Horizontal" && nameParts[2] == "Negative")
				{
					return stickConfig.Left;
				}
				else if (nameParts[1] == "Vertical" && nameParts[2] == "Negative")
				{
					return stickConfig.Up;
				}
				else if (nameParts[1] == "Vertical" && nameParts[2] == "Positive")
				{
					return stickConfig.Down;
				}
			}
		}
		else if (config.Actions.ContainsKey(virtualButton.Name))
		{
			return config.Actions[virtualButton.Name];
		}

		return null;
	}

	/// <summary>
	/// Gets a controller name for a gamepad. This is used to determine which folder we pull icons from.
	/// PS4 and PS5 are shared, since most of their icons are the same. This is different from how this was handled in vanilla Celeste 64, which had them split.
	/// </summary>
	/// <param name="pad"></param>
	/// <returns></returns>
	private static string GetControllerName(Gamepads pad) => pad switch
	{
		Gamepads.DualShock4 => "PlayStation",
		Gamepads.DualSense => "PlayStation",
		Gamepads.Nintendo => "Nintendo Switch",
		Gamepads.Xbox => "Xbox Series",
		_ => "Xbox Series",
	};

	/// <summary>
	/// Used for special cases where there may be multiple options for a button, like with different control types.
	/// </summary>
	/// <param name="buttonName"></param>
	/// <param name="gamepadType"></param>
	/// <returns></returns>
	private static string GetButtonOverrides(ControlsConfigBinding binding, string buttonName, Gamepads gamepadType)
	{
		// Mouse buttons need special names, since Left and Right could refer to Keyboard Left or Mouse Left.
		switch (binding.MouseButton)
		{
			case MouseButtons.Left:
				buttonName = "MouseLeft";
				break;
			case MouseButtons.Right:
				buttonName = "MouseRight";
				break;
			case MouseButtons.Middle:
				buttonName = "MouseMiddle";
				break;
			default:
				break;
		}

		// This is needed to workaround 2 problems with how foster's buttons are set up.
		// Normally, these can get their name from the enum name directly, but these have some special cases.
		// The first issue is that the face buttons have multiple enum entries for the same value, with some being obsolete. We need to force it to get the correct name.
		// The second issue is that Select and Back seem to currently be swapped. So we account for this here. If this gets fixed in foster, we should remove the last 2 cases.
		switch (binding.Button)
		{
			case Buttons.South:
				buttonName = "South";
				break;
			case Buttons.East:
				buttonName = "East";
				break;
			case Buttons.West:
				buttonName = "West";
				break;
			case Buttons.North:
				buttonName = "North";
				break;
			case Buttons.Up:
				buttonName = "ButtonUp";
				break;
			case Buttons.Down:
				buttonName = "ButtonDown";
				break;
			case Buttons.Left:
				buttonName = "ButtonLeft";
				break;
			case Buttons.Right:
				buttonName = "ButtonRight";
				break;
			case Buttons.Select:
				buttonName = "Back";
				break;
			case Buttons.Back:
				buttonName = "Select";
				break;
			default:
				break;
		}

		// PS4 has unique icons separate from PS5. We account for that here.
		if (gamepadType == Gamepads.DualShock4)
		{
			if (buttonName == "Start")
			{
				buttonName = "PS4_Start";
			}
			else if (buttonName == "Select")
			{
				buttonName = "PS4_Select";
			}
		}

		// Separate MacOS special names from Windows.
		if (OperatingSystem.IsMacOS())
		{
			if (buttonName == "LeftAlt" || buttonName == "RightAlt")
			{
				buttonName = "Option";
			}
			else if (buttonName == "LeftOS" || buttonName == "RightOS")
			{
				buttonName = "Command";
			}
		}

		return buttonName;
	}
}

[DisallowHooks]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class DefaultBindingAttribute : Attribute
{
	public ControlsConfigBinding Binding;

	public DefaultBindingAttribute(Keys key)
	{
		Binding = new ControlsConfigBinding(key);
	}

	public DefaultBindingAttribute(Buttons button)
	{
		Binding = new ControlsConfigBinding(button);
	}

	public DefaultBindingAttribute(Buttons button, Gamepads[] forGamepads)
	{
		Binding = new ControlsConfigBinding(button) { ForGamepads = forGamepads };
	}

	public DefaultBindingAttribute(MouseButtons mouseButton)
	{
		Binding = new ControlsConfigBinding(mouseButton);
	}

	public DefaultBindingAttribute(Axes axis, float deadzone, bool inverted)
	{
		Binding = new ControlsConfigBinding(axis, deadzone, inverted);
	}
}

public enum StickDirection
{
	Up, Down, Left, Right
}

[DisallowHooks]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class DefaultStickBindingAttribute : Attribute
{
	public StickDirection Direction;
	public ControlsConfigBinding Binding;

	public Gamepads? onlyFor;
	public Gamepads? notFor;

	public DefaultStickBindingAttribute(StickDirection direction, Keys key)
	{
		this.Direction = direction;
		Binding = new ControlsConfigBinding(key);
	}

	public DefaultStickBindingAttribute(StickDirection direction, Buttons button)
	{
		this.Direction = direction;
		Binding = new ControlsConfigBinding(button);
	}

	public DefaultStickBindingAttribute(StickDirection direction, Buttons button, Gamepads[] forGamepads)
	{
		this.Direction = direction;
		Binding = new ControlsConfigBinding(button) { ForGamepads = forGamepads };
	}

	public DefaultStickBindingAttribute(StickDirection direction, MouseButtons mouseButton)
	{
		this.Direction = direction;
		Binding = new ControlsConfigBinding(mouseButton);
	}

	public DefaultStickBindingAttribute(StickDirection direction, Axes axis, float deadzone, bool inverted)
	{
		this.Direction = direction;
		Binding = new ControlsConfigBinding(axis, deadzone, inverted);
	}
}
