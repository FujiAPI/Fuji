﻿using System.Diagnostics;
using Celeste64.Mod;
using Celeste64.Mod.Data;

namespace Celeste64;

public class SaveSelectionMenu : Menu
{
    public Target Target;

    private int currentPage = 0;
    private int currentRow = 0;
    private int currentColumn = 0;

    private const int rows = 2;
    private const int columns = 4;

    private int CurrentPageStart { get { return currentPage * columns * rows; } }
    private int CurrentIndex { get { return currentRow * columns + currentColumn; } }

    private Subtexture postcardImage;
    private Subtexture strawberryImage;

    public Menu? RootMenu;
    private List<string> saves;
    
    internal SaveSelectionMenu()
    {
        Target = new Target(Overworld.CardWidth, Overworld.CardHeight);
        Game.OnResolutionChanged += () => Target = new Target(Overworld.CardWidth, Overworld.CardHeight);

        postcardImage = new(Assets.Textures["postcards/back-empty"]);
        strawberryImage = Assets.Subtextures["icon_strawberry"];

        saves = SaveManager.Instance.GetSaves();
        foreach (string s in saves)
            Debug.WriteLine(s);

    }

    public override void Initialized()
    {
        base.Initialized();
        currentColumn = 0;
        currentRow = 0;
        currentPage = 0;
    }

    private void RenderSave(Batcher batch, string save, Vec2 pos, Vec2 size)
    {
        float imgScale = 0.7f;
        Subtexture image = strawberryImage;
        Vec2 imageSize = new Vec2(size.X / image.Width, size.Y / image.Height);
        batch.Image(image, (pos - (size * imgScale) / 2) * Game.RelativeScale, imageSize * imgScale * Game.RelativeScale, imageSize * imgScale * Game.RelativeScale, 0, Color.White);
        batch.PushMatrix(Matrix3x2.CreateScale(.6f) * Matrix3x2.CreateTranslation((pos + new Vec2(0, size.Y * 0.4f)) * Game.RelativeScale));
        batch.Text(Language.Current.SpriteFont, save, Vec2.Zero, new Vec2(0.5f, 0), Color.Black * 0.7f);
        batch.PopMatrix();
    }

    private void RenderSelectedSave(Batcher batch, string save, Vec2 pos, Vec2 size)
    {
        float imgScale = 0.8f;
        Subtexture image = strawberryImage;
        Vec2 imageSize = new Vector2(size.X / image.Width, size.Y / image.Height);
        batch.Image(image, (pos - (size * imgScale) / 2) * Game.RelativeScale, imageSize * imgScale * Game.RelativeScale, imageSize * imgScale * Game.RelativeScale, 0, Color.White);
        batch.PushMatrix(Matrix3x2.CreateScale(.7f) * Matrix3x2.CreateTranslation((pos + new Vec2(0, size.Y * 0.4f)) * Game.RelativeScale));
        batch.Text(Language.Current.SpriteFont, save, Vec2.Zero, new Vec2(0.5f, 0), new Color(0x98d3ae));
        batch.PopMatrix();
    }

    private void RenderCurrentSelectedSave(Batcher batch, string save, Vec2 pos, Vec2 size)
    {
        float imgScale = 0.95f;
        Subtexture image = strawberryImage;
        Vec2 imageSize = new Vector2(size.X / image.Width, size.Y / image.Height);
        batch.Image(image, (pos - (size * imgScale) / 2) * Game.RelativeScale, imageSize * imgScale * Game.RelativeScale, imageSize * imgScale * Game.RelativeScale, 0, Color.White);
        batch.PushMatrix(Matrix3x2.CreateScale(.7f) * Matrix3x2.CreateTranslation((pos + new Vec2(0, size.Y * 0.4f)) * Game.RelativeScale));
        batch.Text(Language.Current.SpriteFont, save, Vec2.Zero, new Vec2(0.5f, 0), new Color(0x98d3ae));
        batch.PopMatrix();
    }

    private void RenderCurrentSave(Batcher batch, string save, Vec2 pos, Vec2 size)
    {
        float imgScale = 0.95f;
        Subtexture image = strawberryImage;
        Vec2 imageSize = new Vector2(size.X / image.Width, size.Y / image.Height);
        batch.Image(image, (pos - (size * imgScale) / 2) * Game.RelativeScale, imageSize * imgScale * Game.RelativeScale, imageSize * imgScale * Game.RelativeScale, 0, Color.White);
        batch.PushMatrix(Matrix3x2.CreateScale(.7f) * Matrix3x2.CreateTranslation((pos + new Vec2(0, size.Y * 0.4f)) * Game.RelativeScale));
        batch.Text(Language.Current.SpriteFont, save, Vec2.Zero, new Vec2(0.5f, 0), Color.CornflowerBlue);
        batch.PopMatrix();
    }

    private void RenderSaves(Batcher batch)
    {
        var bounds = Target.Bounds;
        var scale = MathF.Max(bounds.Width / postcardImage.Width, bounds.Height / postcardImage.Height);
        float sizeMin = MathF.Min(postcardImage.Width, postcardImage.Height) / 6;
        var size = new Vec2(sizeMin, sizeMin);

        var offset = new Vec2(postcardImage.Width * -0.19f, postcardImage.Height * 0.5f * -0.15f);

        int index = 0;
        for (int i = 0; i < rows && CurrentPageStart + index < saves.Count; i++)
        {
            for (int j = 0; j < columns && CurrentPageStart + index < saves.Count; j++)
            {
                if (saves[CurrentPageStart + index] == Save.Instance.FileName && index == currentRow * columns + currentColumn) RenderCurrentSelectedSave(batch, saves[CurrentPageStart + index], new Vec2(sizeMin * j * 1.1f, sizeMin * i * 1.1f) + offset, size);
                else if (saves[CurrentPageStart + index] == Save.Instance.FileName) RenderSelectedSave(batch, saves[CurrentPageStart + index], new Vec2(sizeMin * j * 1.1f, sizeMin * i * 1.1f) + offset, size);
                else if (index == currentRow * columns + currentColumn)
                {
                    RenderCurrentSave(batch, saves[CurrentPageStart + index], new Vec2(sizeMin * j * 1.1f, sizeMin * i * 1.1f) + offset, size);
                }
                else
                {
                    RenderSave(batch, saves[CurrentPageStart + index], new Vec2(sizeMin * j * 1.1f, sizeMin * i * 1.1f) + offset, size);
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
                if (((currentPage + 1) * columns * rows) + (currentRow * columns) < saves.Count)
                {
                    currentPage++;
                    currentColumn = 0;
                }
                else if ((currentPage + 1) * columns * rows < saves.Count)
                {
                    currentPage++;
                    currentColumn = 0;
                    currentRow = 0;
                }
            }
            else if (CurrentPageStart + CurrentIndex + 1 < saves.Count)
            {
                currentColumn += 1;
            }
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
        }

        if (Controls.Menu.Vertical.Positive.Pressed && (currentRow + 1) < rows && CurrentPageStart + CurrentIndex + columns < saves.Count)
            currentRow += 1;

        if (Controls.Menu.Vertical.Negative.Pressed && (currentRow - 1) >= 0)
            currentRow -= 1;

        if (Controls.Confirm.Pressed)
        {
            if (!Game.Instance.IsMidTransition)
            {
                SaveManager.Instance.LoadSaveByFileName(saves[CurrentPageStart + CurrentIndex]);
                Game.Instance.Goto(new Transition()
                {
                    Mode = Transition.Modes.Replace,
                    Scene = () => new Overworld(false),
                    ToBlack = new AngledWipe(),
                    ToPause = true
                });
            }
            Controls.Consume();
        }

        if (Controls.CreateFile.Pressed)
        {
            Menu newMenu = new Menu(this);
            newMenu.Title = "Create new file?";
            newMenu.Add(new LegacyOption("Yes", () =>
            {
                if (Game.Instance.IsMidTransition) return;
                SaveManager.Instance.NewSave();
                Game.Instance.Goto(new Transition()
                {
                    Mode = Transition.Modes.Replace,
                    Scene = () => new SelectSaveScene(),
                    ToBlack = new SlideWipe(),
                    ToPause = true
                });
                PopSubMenu();
            }));
            newMenu.Add(new LegacyOption("No", () => PopSubMenu()));
            PushSubMenu(newMenu);

        }

        if (Controls.DeleteFile.Pressed)
        {
            Menu newMenu = new Menu(this);
            newMenu.Title = $"Delete this file? {saves[CurrentPageStart + CurrentIndex]}";
            newMenu.Add(new LegacyOption("Yes", () =>
            {
                if (Game.Instance.IsMidTransition) return;
                SaveManager.Instance.DeleteSave(saves[CurrentPageStart + CurrentIndex]);
                Game.Instance.Goto(new Transition()
                {
                    Mode = Transition.Modes.Replace,
                    Scene = () => new SelectSaveScene(),
                    ToBlack = new SlideWipe(),
                    ToPause = true
                });
                PopSubMenu();
            }));
            newMenu.Add(new LegacyOption("No", () => PopSubMenu()));
            PushSubMenu(newMenu);
        }

        if (Controls.CopyFile.Pressed)
        {
            Menu newMenu = new Menu(this);
            newMenu.Title = $"Copy this file? {saves[CurrentPageStart + CurrentIndex]}";
            newMenu.Add(new LegacyOption("Yes", () =>
            {
                if (Game.Instance.IsMidTransition) return;
                SaveManager.Instance.CopySave(saves[CurrentPageStart + CurrentIndex]);
                Game.Instance.Goto(new Transition()
                {
                    Mode = Transition.Modes.Replace,
                    Scene = () => new SelectSaveScene(),
                    ToBlack = new SlideWipe(),
                    ToPause = true
                });
                PopSubMenu();
            }));
            PushSubMenu(newMenu);
            newMenu.Add(new LegacyOption("No", () => PopSubMenu()));
        }
    }


    protected override void RenderItems(Batcher batch)
    {
        if (postcardImage.Texture != null)
        {
            var bounds = Target.Bounds;
            var scale = MathF.Max(bounds.Width / postcardImage.Width, bounds.Height / postcardImage.Height);
            var size = new Vec2(postcardImage.Width, postcardImage.Height);
            batch.Image(postcardImage, bounds.TopLeft, size / 2, Vec2.One * scale, 0, Color.White);
            batch.Text(Language.Current.SpriteFont, Title, bounds.TopLeft + (-size / 4 + new Vec2(16, 12)) * Game.RelativeScale, Color.Black * 0.7f);
            RenderSaves(batch);
        }
    }
}