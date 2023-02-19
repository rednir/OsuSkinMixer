using Godot;
using System;
using OsuSkinMixer.Models.Osu;
using Skin = OsuSkinMixer.Models.Osu.Skin;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components;

public partial class SkinSelectorPopup : Control
{
	public Action<Skin> SkinSelected { get; set; }

	private PackedScene SkinComponentScene;

	private AnimationPlayer AnimationPlayer;
	private VBoxContainer SkinsContainer;

	public override void _Ready()
	{
		SkinComponentScene = GD.Load<PackedScene>("res://src/Components/SkinComponent.tscn");

		AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");
		SkinsContainer = GetNode<VBoxContainer>("Popup/ScrollContainer/VBoxContainer/ContentPanelContainer/SkinComponentsContainer");

		SetSkins(OsuData.Skins);
	}

	public void SetSkins(Skin[] skins)
	{
		foreach (var child in SkinsContainer.GetChildren())
			child.QueueFree();

		foreach (Skin skin in skins)
		{
			SkinComponent instance = SkinComponentScene.Instantiate<SkinComponent>();
			SkinsContainer.AddChild(instance);
			instance.SetValues(skin);
			instance.Button.Pressed += () => SkinSelected(skin);
		}
	}

	public void In()
	{
		AnimationPlayer.Play("in");
	}
}
