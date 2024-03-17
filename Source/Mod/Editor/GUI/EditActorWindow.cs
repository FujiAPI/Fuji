using ImGuiNET;
using System.Reflection;

namespace Celeste64.Mod.Editor;

public class EditActorWindow() : EditorWindow("EditActor")
{
	// TODO: Properly display selected name
	protected override string Title => EditorWorld.Current.Selected is { } selected
		? $"Edit Actor - {selected}"
		: "Edit Actor - Nothing selected";

	protected override void RenderWindow(EditorWorld editor)
	{
		// TODO: Add some actor picker
		if (ImGui.Button("DEBUG: Add Spikes"))
		{
			editor.AddDefinition(new SpikeBlock.Definition());
		}
		if (ImGui.Button("DEBUG: Add Solid"))
		{
			editor.AddDefinition(new Solid.Definition());
		}

		if (editor.Selected is { } selected)
		{
			var props = selected.GetType()
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(prop => !prop.HasAttr<IgnorePropertyAttribute>());

			foreach (var prop in props)
			{
				if (prop.GetCustomAttribute<CustomPropertyAttribute>() is { } custom)
				{
					object obj = prop.GetValue(selected)!;
					if (custom.RenderGui(ref obj))
					{
						prop.SetValue(selected, obj);
						selected.Dirty = true;
					}

					continue;
				}

				switch (prop.GetValue(selected))
				{
					case Vec3 v:
						if (ImGui.DragFloat3(prop.Name, ref v))
						{
							prop.SetValue(selected, v);
							selected.Dirty = true;
						}
						break;

					default:
						ImGui.Text($" - {prop.Name}: {prop.GetValue(selected)}");
						break;
				}
			}

			ImGui.NewLine();
			if (ImGui.Button("Remove Actor") || Input.Keyboard.Pressed(Keys.Delete))
			{
				editor.RemoveDefinition(selected);
			}
		}
		else
		{
			ImGui.Text("Nothing selected");
		}
	}
}
