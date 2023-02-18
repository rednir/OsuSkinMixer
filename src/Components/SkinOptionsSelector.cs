using Godot;
using System;
using OsuSkinMixer.Models.SkinOptions;

namespace OsuSkinMixer.Components;

public partial class SkinOptionsSelector : VBoxContainer
{
    private PackedScene SkinOptionComponentScene;

    public override void _Ready()
    {
        SkinOptionComponentScene = GD.Load<PackedScene>("res://src/Components/SkinOptionComponent.tscn");
    }

	public void CreateOptionComponents(SkinOption[] skinOptions)
    {
        foreach (var child in GetChildren())
            child.QueueFree();

        foreach (var option in skinOptions)
            addOptionComponent(option, this, 0);

        void addOptionComponent(SkinOption option, VBoxContainer vbox, int layer)
        {
            // Create new nodes for this option if not already existing.
            SkinOptionComponent component = SkinOptionComponentScene.Instantiate<SkinOptionComponent>();
            vbox.AddChild(component);
            component.SetValues(option, layer);

            if (option is ParentSkinOption parentOption)
            {
                var newVbox = CreateOptionChildrenVBox();
				vbox.AddChild(newVbox);
                vbox.MoveChild(newVbox, vbox.GetIndex());

                component.ArrowButton.Toggled += p => vbox.Visible = p;

                foreach (var child in parentOption.Children)
                    addOptionComponent(child, newVbox, layer + 1);
            }
        }
    }

    private VBoxContainer CreateOptionChildrenVBox()
    {
        var vbox = new VBoxContainer()
        {
            CustomMinimumSize = new Vector2(10, 0),
            Visible = false,
        };

        return vbox;
    }
}
