using Godot;
using System;
using OsuSkinMixer.Models.SkinOptions;

namespace OsuSkinMixer.Components;

public partial class SkinOptionsSelector : VBoxContainer
{
    private PackedScene SkinOptionComponentScene;

    private SkinSelectorPopup SkinSelectorPopup;

    public override void _Ready()
    {
        SkinOptionComponentScene = GD.Load<PackedScene>("res://src/Components/SkinOptionComponent.tscn");

        SkinSelectorPopup = GetNode<SkinSelectorPopup>("SkinSelectorPopup");
    }

	public void CreateOptionComponents(SkinOption[] skinOptions)
    {
        foreach (Node child in GetChildren())
            (child as SkinOptionComponent)?.QueueFree();

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
                var newVbox = new VBoxContainer()
                {
                    CustomMinimumSize = new Vector2(10, 0),
                    Visible = false,
                };
                newVbox.AddThemeConstantOverride("separation", 8);

				vbox.AddChild(newVbox);
                vbox.MoveChild(newVbox, component.GetIndex() + 1);

                component.ArrowButton.Toggled += p => newVbox.Visible = p;
                component.Button.Pressed += () => SkinSelectorPopup.In();

                foreach (var child in parentOption.Children)
                    addOptionComponent(child, newVbox, layer + 1);
            }
        }
    }
}
