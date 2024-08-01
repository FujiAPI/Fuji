using System.ComponentModel;

namespace Celeste64.Mod;

public class OnScreenKeyboardMenu : Menu
{
	public Target Target;
	public Target GameTarget;

	private int currentPage = 0;
	private int currentRow = 0;
	private int currentColumn = 0;

	private const int rows = 7;
	private const int columns = 9;

	private int CurrentPageStart => currentPage * columns * rows;
	private int CurrentIndex => currentRow * columns + currentColumn;

	private bool shiftMode;
	private bool allowShift;

	private string textValue = string.Empty;

	private InputField Owner;

	private Dictionary<string, string> KeyValueType;
	private Dictionary<string, string> KeyValueShiftType;

	private List<string> KeyValueTypeList;
	private List<string> KeyValueShiftTypeList;

	internal OnScreenKeyboardMenu(Menu? rootMenu, InputField owner, Dictionary<string, string> keyboardType, bool allowShiftMode = true)
	{
		Target = new Target(Overworld.CardWidth, Overworld.CardHeight);
		Game.OnResolutionChanged += () => Target = new Target(Overworld.CardWidth, Overworld.CardHeight);
		GameTarget = new Target(Game.Width, Game.Height);
		RootMenu = rootMenu;

		KeyValueType = keyboardType;
		KeyValueShiftType = KeyboardHandler.GetShiftDict(KeyValueType);

		KeyValueTypeList = KeyboardHandler.KeyValuesToList(KeyValueType);
		KeyValueShiftTypeList = KeyboardHandler.KeyValuesToList(KeyValueShiftType);

		allowShift = allowShiftMode;

		Owner = owner;
	}

	public override void Initialized()
	{
		base.Initialized();
		currentColumn = 0;
		currentRow = 0;
		currentPage = 0;

		textValue = Owner.GetFieldText();
	}

	public override void Closed()
	{
		Owner.SetFieldText(textValue);
	}

	private void RenderCharacter(Batcher batch, string character, Vec2 pos, Vec2 size)
	{
		if (character == " ")
			character = Loc.Str("KeyboardSpace");
		batch.PushMatrix(Matrix3x2.CreateScale(1.1f) * Matrix3x2.CreateTranslation((pos + new Vec2(size.X * 0.4f - 20, size.Y * 0.4f - 20)) * Game.RelativeScale));
		batch.Text(Language.Current.SpriteFont, character, Vec2.Zero, new Vec2(0.5f, 0), Color.Black);

		batch.PopMatrix();

		batch.PushMatrix(Matrix3x2.CreateScale(0.9f) * Matrix3x2.CreateTranslation((pos + new Vec2(size.X * 0.4f - 20, size.Y * 0.4f - 20)) * Game.RelativeScale));
		batch.Text(Language.Current.SpriteFont, character, Vec2.Zero, new Vec2(0.5f, 0), Color.White);

		batch.PopMatrix();
	}

	private void RenderCurrentCharacter(Batcher batch, string character, Vec2 pos, Vec2 size)
	{
		if (character == " ")
			character = Loc.Str("KeyboardSpace");
		batch.PushMatrix(Matrix3x2.CreateScale(1.1f) * Matrix3x2.CreateTranslation((pos + new Vec2(size.X * 0.4f - 20, size.Y * 0.4f - 20)) * Game.RelativeScale));
		batch.Text(Language.Current.SpriteFont, character, Vec2.Zero, new Vec2(0.5f, 0), Color.Black);

		batch.PopMatrix();

		batch.PushMatrix(Matrix3x2.CreateScale(0.9f) * Matrix3x2.CreateTranslation((pos + new Vec2(size.X * 0.4f - 20, size.Y * 0.4f - 20)) * Game.RelativeScale));
		batch.Text(Language.Current.SpriteFont, character, Vec2.Zero, new Vec2(0.5f, 0), (Time.BetweenInterval(0.1f) ? 0x84FF54 : 0xFCFF59));

		batch.PopMatrix();
	}

	private void RenderCharacters(Batcher batch)
	{
		var bounds = GameTarget.Bounds;
		Vec2 size = new Vec2(bounds.Width, bounds.Height);
		Vec2 offset = new Vec2(20, 20);

		int index = 0;
		for (int i = 0; i < rows && CurrentPageStart + index < (KeyboardHandler.TrimKeyList(!shiftMode ? KeyValueTypeList : KeyValueShiftTypeList).Count); i++)
		{
			for (int j = 0; j < columns && CurrentPageStart + index < (KeyboardHandler.TrimKeyList(!shiftMode ? KeyValueTypeList : KeyValueShiftTypeList).Count); j++)
			{
				if (index == currentRow * columns + currentColumn)
				{
					string keyName = KeyboardHandler.TrimKeyList(!shiftMode ? KeyValueTypeList : KeyValueShiftTypeList)[CurrentPageStart + index];
					RenderCurrentCharacter(batch, keyName, new Vec2(j, i) * offset, size);
				}
				else
				{
					string keyName = KeyboardHandler.TrimKeyList(!shiftMode ? KeyValueTypeList : KeyValueShiftTypeList)[CurrentPageStart + index];
					RenderCharacter(batch, keyName, new Vec2(j, i) * offset, size);
				}
				index++;
			}
		}
	}

	protected override void HandleInput()
	{
		if (Controls.Menu.Horizontal.Positive.Pressed)
		{
			if (currentColumn == columns - 1)
			{
				if (((currentPage + 1) * columns * rows) + (currentRow * columns) < KeyboardHandler.TrimKeyList(!shiftMode ? KeyValueTypeList : KeyValueShiftTypeList).Count)
				{
					currentPage++;
					currentColumn = 0;
				}
				else if ((currentPage + 1) * columns * rows < KeyboardHandler.TrimKeyList(!shiftMode ? KeyValueTypeList : KeyValueShiftTypeList).Count)
				{
					currentPage++;
					currentColumn = 0;
					currentRow = 0;
				}
			}
			else if (CurrentPageStart + CurrentIndex + 1 < KeyboardHandler.TrimKeyList(!shiftMode ? KeyValueTypeList : KeyValueShiftTypeList).Count)
			{
				currentColumn += 1;
			}
			Audio.Play(DownSound);
		}
		if (Controls.Menu.Horizontal.Negative.Pressed)
		{
			if (currentColumn == 0)
			{
				if (currentPage > 0)
				{
					currentPage--;
					currentColumn = columns - 1;
				}
			}
			else
			{
				currentColumn -= 1;
			}
			Audio.Play(DownSound);
		}

		if (Controls.Menu.Vertical.Positive.Pressed && (currentRow + 1) < rows && CurrentPageStart + CurrentIndex + columns < KeyboardHandler.TrimKeyList(!shiftMode ? KeyValueTypeList : KeyValueShiftTypeList).Count)
		{
			currentRow += 1;
			Audio.Play(DownSound);
		}
		if (Controls.Menu.Vertical.Negative.Pressed && (currentRow - 1) >= 0)
		{
			currentRow -= 1;
			Audio.Play(DownSound);
		}
		/*
		* The next four controls are only relevant to gamepads.
		* We check if a keyboard key is pressed to avoid conflicts.
		*/
		if (Controls.CopyFile.Pressed && KeyboardHandler.Instance.GetPressedKey() == null && allowShift)
			shiftMode = !shiftMode;

		if (Controls.RenameFile.Pressed && textValue.Length > 0 && KeyboardHandler.Instance.GetPressedKey() == null)
		{
			textValue = textValue.Remove(textValue.Length - 1);
		}

		if (Controls.Confirm.Pressed && KeyboardHandler.Instance.GetPressedKey() == null)
		{
			textValue += KeyboardHandler.TrimKeyList(!shiftMode ? KeyValueTypeList : KeyValueShiftTypeList)[CurrentPageStart + CurrentIndex];
			Audio.Play(UpSound);
		}

		if (Controls.Cancel.ConsumePress() && KeyboardHandler.Instance.GetPressedKey() == null)
		{
			Owner.RootMenu.PopSubMenu();
		}

		if (KeyboardHandler.Instance.GetPressedKey() is Keys.Enter or Keys.Enter2 or Keys.KeypadEnter)
		{
			Owner.RootMenu.PopSubMenu();
		}

		ReadKey();
	}

	protected override void RenderItems(Batcher batch)
	{
		batch.PushMatrix(new Vec2(GameTarget.Bounds.TopLeft.X, GameTarget.Bounds.TopLeft.Y), false);

		if (textValue.Length == 0)
			batch.Text(Language.Current.SpriteFont, "Enter text", new Vec2(GameTarget.Bounds.TopCenter.X - Language.Current.SpriteFont.WidthOf("Enter text") / 2, GameTarget.Bounds.TopCenter.Y + 96), Color.CornflowerBlue * 0.6f);

		batch.Text(Language.Current.SpriteFont, textValue, new Vec2(GameTarget.Bounds.TopCenter.X - Language.Current.SpriteFont.WidthOf(textValue) / 2, GameTarget.Bounds.TopCenter.Y + 96), Color.CornflowerBlue);
		batch.Text(Language.Current.SpriteFont, Loc.Str("OSKUserInstructions"), new Vec2(GameTarget.Bounds.TopCenter.X - Language.Current.SpriteFont.WidthOf(Loc.Str("OSKUserInstructions")) / 2, GameTarget.Bounds.TopCenter.Y + 284), Color.CornflowerBlue);
		RenderCharacters(batch);
	}

	public void ReadKey()
	{
		Keys? key = KeyboardHandler.Instance.GetPressedKey();

		if (key != null)
		{
			if (key == Keys.Backspace || key == Keys.KeypadBackspace)
			{
				if (textValue.Length > 0)
					textValue = textValue.Remove(textValue.Length - 1);
			}
			else if (KeyValueTypeList.Contains(KeyboardHandler.GetKeyName(key)))
			{
				textValue += KeyboardHandler.GetKeyName(key);
			}
		}
	}
}
