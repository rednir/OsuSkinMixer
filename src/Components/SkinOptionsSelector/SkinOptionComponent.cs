using Godot;
using System;
using OsuSkinMixer.Models.SkinOptions;
using OsuSkinMixer.Models.Osu;
using Skin = OsuSkinMixer.Models.Osu.Skin;

namespace OsuSkinMixer.Components.SkinOptionsSelector;

public partial class SkinOptionComponent : HBoxContainer
{
	public TextureButton ArrowButton { get; private set; }

	public TextureButton ResetButton { get; private set; }

	public Button Button { get; private set; }

	public string DefaultValue { get; set; }

	public SkinOption SkinOption { get; private set; }

	private Label Label;

    public override void _Ready()
    {
		ArrowButton = GetNode<TextureButton>("ArrowButton");
		Label = GetNode<Label>("Label");
		Button = GetNode<Button>("Button");
		ResetButton = GetNode<TextureButton>("Button/ResetButton");
    }

	public void SetSkinOption(SkinOption option, int indentLayer)
	{
		SkinOption = option;
		TooltipText = option.ToString();
		Name = option.Name;
		Label.Text = option.Name;
		Button.Text = DefaultValue;

		if (option is not ParentSkinOption)
		{
			// Option has no children, so hide button to expand option.
			// Don't set visible to false so the button still takes up space. 
			ArrowButton.Disabled = true;
			ArrowButton.Modulate = new Color(0, 0, 0, 0);
		}

		var indent = new Panel()
		{
			CustomMinimumSize = new Vector2(indentLayer * 30, 1),
			Modulate = new Color(0, 0, 0, 0),
		};

		AddChild(indent);
		MoveChild(indent, 0);
	}

	public void SetSelectedSkin(Skin skin)
	{
		if (skin == null)
		{
			Button.Text = DefaultValue;
			SkinOption.Skin = null;
			return;
		}

		Button.Text = skin.Name;
		SkinOption.Skin = skin;
	}

	public void SetToVarious()
	{
		Button.Text = "<<VARIOUS>>";
		SkinOption.Skin = null;
	}
}
