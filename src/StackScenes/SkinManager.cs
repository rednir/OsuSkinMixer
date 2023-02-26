using Godot;
using System;
using OsuSkinMixer.Components.SkinSelector;
using OsuSkinMixer.Models.Osu;

namespace OsuSkinMixer.StackScenes;

public partial class SkinManager : StackScene
{
    public override string Title => "Skin Manager";

	private PackedScene SkinInfoScene;

	private SkinSelectorPopup SkinSelectorPopup;
	private Button SelectSkinButton;

    public override void _Ready()
	{
		SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");

		VisibilityChanged += OnVisibilityChanged;

		SelectSkinButton = GetNode<Button>("%SelectSkinButton");
		SelectSkinButton.Pressed += () => SkinSelectorPopup.In();

		SkinSelectorPopup = GetNode<SkinSelectorPopup>("%SkinSelectorPopup");
		SkinSelectorPopup.In();
		SkinSelectorPopup.OnSelected = OnSkinSelected;
	}

	private void OnSkinSelected(OsuSkin skin)
	{
		SkinInfo instance = SkinInfoScene.Instantiate<SkinInfo>();
		instance.SetSkin(skin);
		EmitSignal(SignalName.ScenePushed, instance);
	}

	private void OnVisibilityChanged()
	{
		if (Visible)
			SkinSelectorPopup.In();
	}
}
