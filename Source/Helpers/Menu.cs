using Celeste64.Mod;

namespace Celeste64;

public class Menu
{
	public static float Spacing => 4 * Game.RelativeScale;
	public static float SpacerHeight => 12 * Game.RelativeScale;
	protected float TitleScale = 0.75f;

	public abstract class Item
	{
		// Whether this item can be selected when scrolling through the menu.
		public virtual bool Selectable { get; } = true;
		public virtual bool Pressed() => false;
		public virtual void Slide(int dir) { }

		// LocString is the base localized string object, before any changes.
		// This is kept separate from the label so we can get substrings from the LocString like the Description.
		public virtual Loc.Localized? LocString { get; set; } = null;

		// Get the localized string or the string's key from the LocString. If LocString is null, return an empty string instead.
		// This can be overridden in subclasses for more control over how the label is displayed.
		public virtual string Label => LocString?.ToString() ?? "";

		// Return the localized description string from the loc string.
		// If a description override has been set, use that instead.
		public virtual string Description => descriptionOverride ?? LocString?.GetSub("desc").StringOrEmpty() ?? "";

		private string? descriptionOverride = null;

		/// <summary>
		/// Set the Description Override.
		/// This is used to force the description to be a given string without automatically being localized.
		/// </summary>
		/// <param name="description"></param>
		/// <returns></returns>
		public Item Describe(Loc.Localized description)
		{
			this.descriptionOverride = description;

			return this;
		}
	}

	/// <summary>
	/// Represents a Input Binding Menu Item.
	/// This will show all the current bindings for a virtual buttons, and if you click it, it will open a binding menu to add new bindings.
	/// </summary>
	/// <param name="locString">Localization key for label that shows. Shold represent the button name.</param>
	/// <param name="button">Virtual button we are trying to bind to</param>
	/// <param name="rootMenu">Root menu for the menu we are currently in</param>
	/// <param name="isForController">True if this is a controller binding, false for keyboard</param>
	public class InputBind(Loc.Localized locString, VirtualButton button, Menu? rootMenu, bool isForController, GameMod? mod = null) : Item
	{
		public override Loc.Localized? LocString => locString;

		public float DeadZone;
		public bool RequiresBinding;
		public bool IsForController => isForController;
		public GameMod? Mod => mod;
		public override bool Pressed()
		{
			Audio.Play(Sfx.ui_select);
			rootMenu?.PushSubMenu(new BindControlMenu(rootMenu, button, locString, isForController, DeadZone, mod));
			return true;
		}

		public virtual List<Subtexture> GetTextures()
		{
			return Controls.GetPrompts(button, isForController, Mod?.ModSettingsData?.ModControlBindings);
		}

		public VirtualButton GetButton()
		{
			return button;
		}
	}

	/// <summary>
	/// Represents a submenu menu item.
	/// When pressed, it will open the given submenu
	/// </summary>
	/// <param name="locString">Localization key for the submenu label</param>
	/// <param name="rootMenu">Root Menu for the menu that we are currently inside of.</param>
	/// <param name="submenu">Submenu to open when clicked.</param>
	public class Submenu(Loc.Localized locString, Menu? rootMenu, Menu? submenu = null) : Item
	{
		public override Loc.Localized? LocString => locString;

		public override bool Pressed()
		{
			if (submenu != null)
			{
				Audio.Play(Sfx.ui_select);
				submenu.Index = 0;
				rootMenu?.PushSubMenu(submenu);
				return true;
			}

			return false;
		}
	}

	/// <summary>
	/// Spacer menu item to allow a row of separation between menu items.
	/// </summary>
	public class Spacer : Item
	{
		public override bool Selectable => false;
	}

	/// <summary>
	/// Basic slider component that allows you to pick an int value between a min and a max.
	/// </summary>
	public class Slider : Item
	{
		private readonly List<string> labels = [];
		private readonly int min;
		private readonly int max;
		private readonly Func<int> get;
		private readonly Action<int> set;

		public Slider(Loc.Localized locString, int min, int max, Func<int> get, Action<int> set)
		{
			LocString = locString;
			for (int i = 0, n = (max - min); i <= n; i++)
				labels.Add($"[{new string('|', i)}{new string('.', n - i)}]");
			this.min = min;
			this.max = max;
			this.get = get;
			this.set = set;
		}

		public override string Label => $"{LocString} {labels[get() - min]}";
		public override void Slide(int dir) => set(Calc.Clamp(get() + dir, min, max));
	}

	/// <summary>
	/// Represents a subheader menu item.
	/// This allows a basic text only entry in a list of menu items to represent a subheader for a group of items.
	/// </summary>
	/// <param name="locString"></param>
	public class SubHeader(Loc.Localized locString) : Item
	{
		public override Loc.Localized? LocString => locString;
		public override bool Selectable { get; } = false;
	}

	/// <summary>
	/// Menu item that allows you to pick an item from a list of strings.
	/// Note: For static lists of options, a Multiselect using an enum is recommended. This item is more for dynamic lists where the options can change dynamically.
	/// </summary>
	public class OptionList : Item
	{
		private readonly int min;
		private readonly Func<string> get;
		private readonly Func<int> getMax;
		private readonly Func<List<string>> getLabels;
		private readonly Action<string> set;

		public OptionList(Loc.Localized locString, Func<List<string>> getLabels, Func<string> get, Action<string> set)
		{
			this.getLabels = getLabels;
			this.min = 0;
			this.getMax = () => getLabels().Count;
			this.get = get;
			this.set = set;
			this.LocString = locString;
		}

		public OptionList(Loc.Localized locString, Func<List<string>> getLabels, int min, Func<int> getMax, Func<string> get, Action<string> set)
		{
			this.getLabels = getLabels;
			this.min = min;
			this.getMax = getMax;
			this.get = get;
			this.set = set;
			this.LocString = locString;
		}

		public override string Label => $"{LocString} : {getLabels()[getId() - min]}";
		public override void Slide(int dir)
		{
			if (getLabels().Count > 1)
			{
				set(getLabels()[(getMax() + getId() + dir) % getMax()]);
			}
		}

		private int getId()
		{
			int id = getLabels().IndexOf(get());
			return id > -1 ? id : 0;
		}
	}

	/// <summary>
	/// Basic menu item that will run an action when clicked
	/// </summary>
	/// <param name="locString">Localization key for Option Label</param>
	/// <param name="action">Action to run when pressed</param>
	public class Option(Loc.Localized locString, Action? action = null) : Item
	{
		public override Loc.Localized? LocString => locString;

		public override bool Pressed()
		{
			if (action != null)
			{
				Audio.Play(Sfx.ui_select);
				action();
				return true;
			}
			return false;
		}
	}

	/// <summary>
	/// Menu item representing a basic boolean value that can be turned off and on.
	/// </summary>
	/// <param name="locString">Localization key for Toggle Label</param>
	/// <param name="action">action to run when pressed. This should be where you set the boolean value</param>
	/// <param name="get">function that gets the current state of the boolean value</param>
	public class Toggle(Loc.Localized locString, Action action, Func<bool> get) : Item
	{
		private string labelOff => $"{locString} : {Loc.Str("OptionsToggleOff")}";
		private string labelOn => $"{locString} :  {Loc.Str("OptionsToggleOn")}";

		public override Loc.Localized? LocString => locString;
		public override string Label => get() ? labelOn : labelOff;

		public override bool Pressed()
		{
			action();
			if (get())
				Audio.Play(Sfx.main_menu_toggle_on);
			else
				Audio.Play(Sfx.main_menu_toggle_off);
			return true;
		}
	}

	/// <summary>
	/// Multiselect component that lets you pick a list of strings.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="locString"></param>
	/// <param name="set"></param>
	/// <param name="get"></param>
	public class MultiSelect(Loc.Localized locString, List<string> options, Func<int> get, Action<int> set) : Item
	{
		public override Loc.Localized LocString => locString;
		public override string Label => $"{LocString} : {options[get()]}";

		public override void Slide(int dir)
		{
			Audio.Play(Sfx.ui_select);

			int index = get();
			if (index < options.Count() - 1 && dir == 1)
				index++;
			if (index > 0 && dir == -1)
				index--;
			set(index);
		}
	}

	/// <summary>
	/// Multiselect component that lets you pick a value from an enum.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="locString"></param>
	/// <param name="set"></param>
	/// <param name="get"></param>
	public class MultiSelect<T>(Loc.Localized locString, Action<T> set, Func<T> get)
		: MultiSelect(locString, GetEnumOptions(), () => (int)(object)get(), i => set((T)(object)i))
		where T : struct, Enum
	{
		private static List<string> GetEnumOptions()
		{
			var list = new List<string>();
			foreach (var it in Enum.GetNames<T>())
				list.Add(it);
			return list;
		}
	}

	public class InputField : Item
	{
		public OnScreenKeyboardMenu keyboardMenu;

		private Action<string> setter;
		private Func<string> getter;

		public Menu RootMenu { get; protected set; }

		private string fieldText;
		public override string Label => $"{LocString} : {getter()}";

		public void SetFieldText(string text)
		{
			fieldText = text;
			setter(fieldText);
		}

		public string GetFieldText()
		{
			return getter();
		}

		public override bool Pressed()
		{
			RootMenu.PushSubMenu(keyboardMenu);
			return true;
		}

		public InputField(Loc.Localized locString, Action<string> set, Func<string> get, Menu rootMenu, Dictionary<string, string>? characters = null)
		{
			LocString = locString;
			setter = set;
			getter = get;
			RootMenu = rootMenu;
			fieldText = getter();
			if (characters == null) characters = KeyboardHandler.AllCharactersList;
			keyboardMenu = new OnScreenKeyboardMenu(rootMenu, this, characters);
		}
	}

	public int Index;
	public string Title = string.Empty;
	public bool Focused = true;

	protected readonly List<Item> items = [];
	protected readonly Stack<Menu> submenus = [];
	public Menu? RootMenu { get; protected set; }

	public string UpSound = Sfx.ui_move;
	public string DownSound = Sfx.ui_move;

	public bool IsInMainMenu => submenus.Count <= 0;
	public Menu CurrentMenu => GetDeepestActiveSubmenu(this);

	protected virtual int maxItemsCount { get; set; } = 12;
	protected int scrolledAmount = 0;
	protected bool showScrollbar = true;

	public Menu GetDeepestActiveSubmenu(Menu target)
	{
		if (target.submenus.Count <= 0)
		{
			return target;
		}
		else
		{
			return GetDeepestActiveSubmenu(target.submenus.Peek());
		}
	}

	public Menu GetSecondDeepestMenu(Menu target)
	{
		if (target.submenus.Peek() != null && target.submenus.Peek().submenus.Count <= 0)
		{
			return target;
		}
		else
		{
			return GetSecondDeepestMenu(target.submenus.Peek());
		}
	}

	public Vec2 Size
	{
		get
		{
			var size = Vec2.Zero;
			var font = Language.Current.SpriteFont;

			if (!string.IsNullOrEmpty(Title))
			{
				size.X = font.WidthOf(Title) * TitleScale;
				size.Y += font.HeightOf(Title) * TitleScale;
				size.Y += SpacerHeight + Spacing;
			}

			for (int i = scrolledAmount; i < items.Count && i < scrolledAmount + maxItemsCount; i++)
			{
				if (string.IsNullOrEmpty(items[i].Label))
				{
					size.Y += SpacerHeight;
				}
				else
				{
					size.X = MathF.Max(size.X, font.WidthOf(items[i].Label));
					size.Y += font.LineHeight;
				}
				size.Y += Spacing;
			}

			if (items.Count > 0)
				size.Y -= Spacing;

			return size;
		}
	}

	public Menu(Menu? rootMenu)
	{
		RootMenu = rootMenu;
	}

	public Menu()
	{

	}

	public virtual void Initialized()
	{

	}

	public virtual void Closed()
	{

	}

	public Menu Add(Item item)
	{
		items.Add(item);
		return this;
	}

	internal void PushSubMenu(Menu menu)
	{
		menu.RootMenu = RootMenu ?? this;
		submenus.Push(menu);
		menu.Initialized();
	}

	internal void PopSubMenu()
	{
		var popped = submenus.Pop();
		popped.Closed();
	}

	internal void PopRootSubMenu()
	{
		if (RootMenu != null)
		{
			RootMenu.PopSubMenu();
		}
		else
		{
			PopSubMenu();
		}
	}

	internal void PushRootSubMenu(Menu menu)
	{
		if (RootMenu != null)
		{
			RootMenu.PushSubMenu(menu);
		}
		else
		{
			PopSubMenu();
		}
	}

	public void CloseSubMenus()
	{
		foreach (var submenu in submenus)
		{
			submenu.Closed();
		}
		submenus.Clear();
	}

	protected virtual void HandleInput()
	{
		VirtualStick MControl = Controls.Menu;
		VirtualAxis MControlH = MControl.Horizontal;
		VirtualAxis MControlV = MControl.Vertical;

		if (items.Count > 0)
		{
			var was = Index;
			var step = 0;

			if (MControlV.Positive.Pressed || MControlV.Positive.Repeated)
				step = 1;
			if (MControlV.Negative.Pressed || MControlV.Negative.Repeated)
				step = -1;

			Index += step;
			while (step != 0 && !items[(items.Count + Index) % items.Count].Selectable)
				Index += step;
			Index = (items.Count + Index) % items.Count;

			if (items.Count > maxItemsCount)
			{
				if (Index >= scrolledAmount + (maxItemsCount - 3))
				{
					scrolledAmount = Index - (maxItemsCount - 3);
				}
				else if (Index < scrolledAmount + 2)
				{
					scrolledAmount = Index - 2;
				}
				scrolledAmount = Math.Clamp(scrolledAmount, 0, items.Count - maxItemsCount);
			}

			if (was != Index)
				Audio.Play(step < 0 ? UpSound : DownSound);

			if (MControlH.Negative.Pressed || MControlH.Negative.Repeated)
				items[Index].Slide(-1);
			if (MControlH.Positive.Pressed || MControlH.Positive.Repeated)
				items[Index].Slide(1);

			if (Controls.Confirm.Pressed && items[Index].Pressed())
				Controls.Consume();

			if (items[Index] is InputBind bind)
			{
				if (Controls.ResetBindings.ConsumePress())
				{
					// Reset current binding to it's default value
					Controls.ResetBinding(bind.GetButton(), bind.IsForController, bind.Mod);
				}
				else if (Controls.ClearBindings.ConsumePress())
				{
					// Clear current binding so there are no bindings (Or 1 binding if RequiredBinding flag is true, where it will keep just the last binding)
					Controls.ClearBinding(bind.GetButton(), bind.IsForController, bind.RequiresBinding, bind.Mod?.ModSettingsData?.ModControlBindings);
				}
			}
		}
	}

	public void Update()
	{
		if (Focused)
		{
			CurrentMenu.HandleInput();

			if (!IsInMainMenu && Controls.Cancel.ConsumePress())
			{
				Audio.Play(Sfx.main_menu_toggle_off);
				var popped = GetSecondDeepestMenu(this).submenus.Pop();
				popped.Closed();
			}
		}
	}

	protected virtual void RenderItems(Batcher batch)
	{
		var font = Language.Current.SpriteFont;
		var size = Size;
		var position = Vec2.Zero;
		batch.PushMatrix(new Vec2(0, -size.Y / 2));

		if (!string.IsNullOrEmpty(Title))
		{
			var text = Title;
			var justify = new Vec2(0.5f, 0);
			var color = new Color(8421504);

			batch.PushMatrix(
				Matrix3x2.CreateScale(TitleScale) *
				Matrix3x2.CreateTranslation(position));
			UI.Text(batch, text, Vec2.Zero, justify, color);
			batch.PopMatrix();

			position.Y += font.HeightOf(Title) * TitleScale;
			position.Y += SpacerHeight + Spacing;
		}

		for (int i = scrolledAmount; i < items.Count && i < scrolledAmount + maxItemsCount; i++)
		{
			if (string.IsNullOrEmpty(items[i].Label))
			{
				position.Y += SpacerHeight;
				continue;
			}

			var text = items[i].Label;
			var justify = new Vec2(0.5f, 0);
			var color = Index == i && Focused ? (Time.BetweenInterval(0.1f) ? 0x84FF54 : 0xFCFF59) : Color.White;

			if (items[i] is SubHeader)
			{
				color = new Color(8421504);
				position.Y += Spacing;
				batch.PushMatrix(
					Matrix3x2.CreateScale(TitleScale) *
					Matrix3x2.CreateTranslation(position));
				UI.Text(batch, text, Vec2.Zero, justify, color);
				batch.PopMatrix();
				position.Y += font.LineHeight;
			}
			else if (items[i] is InputBind)
			{
				UI.Text(batch, text, position, new Vec2(1.0f, 0), color);

				InputBind item = (InputBind)items[i];
				var textures = item.GetTextures();
				foreach (var texture in textures)
				{
					batch.PushMatrix(
						Matrix3x2.CreateScale(TitleScale) *
						Matrix3x2.CreateTranslation(position));
					position.X += (24 * Game.RelativeScale);
					UI.Icon(batch, texture, "", Vec2.Zero);
					batch.PopMatrix();
				}
				position.Y += font.LineHeight;
				position.Y += Spacing;
				position.X = 0;
			}
			else
			{
				UI.Text(batch, text, position, justify, color);
				position.Y += font.LineHeight;
				position.Y += Spacing;
			}
		}
		batch.PopMatrix();

		// Render a scrollbar if there are too many items to show on screen at once
		if (showScrollbar && items.Count > maxItemsCount)
		{
			// TODO: This will need to be redone if we implement mouse support and want it to interact with menus.
			float padding = 4 * Game.RelativeScale;
			float scrollSize = 16 * Game.RelativeScale;
			float xPos = Game.Width - scrollSize - padding;
			float scrollBarHeight = Game.Height - (scrollSize * 2) - padding * 4;
			float scrollStartPos = padding * 2 + scrollSize;
			batch.PushMatrix(Vec2.Zero, false);
			batch.Rect(new Rect(xPos, padding, scrollSize, scrollSize), Color.White);
			batch.Rect(new Rect(xPos, scrollStartPos, scrollSize, scrollBarHeight), Color.Gray);
			float scrollYPos = (int)MathF.Ceiling(scrollStartPos + (scrolledAmount * scrollBarHeight / items.Count));
			float scrollYHeight = scrollBarHeight * maxItemsCount / items.Count;
			batch.Rect(new Rect(xPos, scrollYPos, scrollSize, scrollYHeight), Color.White);
			batch.Rect(new Rect(xPos, Game.Height - scrollSize - padding, scrollSize, scrollSize), Color.White);
			batch.PopMatrix();
		}
	}

	public virtual void Render(Batcher batch, Vec2 position)
	{
		batch.PushMatrix(position);
		CurrentMenu.RenderItems(batch);
		batch.PopMatrix();

		// Don't render the description if the menu has no items.
		if (CurrentMenu.items.Count > 0)
		{
			var currentItem = CurrentMenu.items[CurrentMenu.Index];

			var text = currentItem.Description;
			var justify = new Vec2(0.5f, -8f);
			var color = Color.LightGray;

			UI.Text(batch, text, position, justify, color);
		}
	}
}
