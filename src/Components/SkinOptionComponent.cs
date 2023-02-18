using Godot;
using System;

namespace OsuSkinMixer;

public partial class SkinOptionComponent : HBoxContainer
{
	private TextureButton ArrowButton;
	private Label Label;
	private Button Button;
	private TextureButton ResetButton;

    public override void _Ready()
    {
		ArrowButton = GetNode<TextureButton>("ArrowButton");
		Label = GetNode<Label>("Label");
		Button = GetNode<Button>("Button");
		ResetButton = GetNode<TextureButton>("Button/ResetButton");
    }
}
