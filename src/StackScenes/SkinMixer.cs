using Godot;
using System;
using OsuSkinMixer.Models.SkinOptions;

namespace OsuSkinMixer.StackScenes;

public partial class SkinMixer : StackScene
{
    public override string Title => "Skin Mixer";

    private SkinOption[] SkinOptions { get; } = SkinOption.Default;

    public override void _Ready()
    {
		CreateOptionButtons();
    }

    private void CreateOptionButtons()
    {
        var rootVbox = GetNode<VBoxContainer>("OptionsContainer");

        foreach (var child in rootVbox.GetChildren())
            child.QueueFree();

        foreach (var option in SkinOptions)
            addOptionButton(option, rootVbox, 0);

        void addOptionButton(SkinOption option, VBoxContainer vbox, int layer)
        {
            // Create new nodes for this option if not already existing.
            var hbox = (HBoxContainer)GetNode("OptionTemplate").Duplicate();
            var arrowButton = hbox.GetChild<TextureButton>(0);
            var label = hbox.GetChild<Label>(1);
            option.OptionButton = hbox.GetChild<OptionButton>(2);

            hbox.Name = option.Name;
            label.Text = option.Name;
            label.Modulate = new Color(1, 1, 1, Math.Max(1f - (layer / 4f), 0.55f));
            hbox.TooltipText = option.ToString();//.Wrap(100);

            var indent = new Panel()
            {
                CustomMinimumSize = new Vector2(layer * 30, 1),
                Modulate = new Color(0, 0, 0, 0),
            };

            // Indent needs to be the first node in the HBoxContainer.
            hbox.AddChild(indent);
            hbox.MoveChild(indent, 0);

            vbox.AddChild(hbox);

            if (option is ParentSkinOption parentOption)
            {
                var newVbox = CreateOptionChildrenVBox();
				vbox.AddChild(newVbox);
                vbox.MoveChild(newVbox, vbox.GetIndex());

                arrowButton.Toggled += t => ArrowButtonPressed(t, newVbox);

                foreach (var child in parentOption.Children)
                    addOptionButton(child, newVbox, layer + 1);
            }
            else
            {
                // No children, so hide arrow button.
                arrowButton.Disabled = true;
                arrowButton.Modulate = new Color(0, 0, 0, 0);
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

	private void ArrowButtonPressed(bool pressed, VBoxContainer vbox)
	{
		vbox.Visible = pressed;
	}
}
