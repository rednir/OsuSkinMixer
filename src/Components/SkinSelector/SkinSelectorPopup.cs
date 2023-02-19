using Godot;
using System;
using Skin = OsuSkinMixer.Models.Osu.Skin;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components.SkinSelector;

public partial class SkinSelectorPopup : Control
{
	private PackedScene SkinComponentScene;

	private AnimationPlayer AnimationPlayer;
	private VBoxContainer SkinsContainer;

	public override void _Ready()
	{
		SkinComponentScene = GD.Load<PackedScene>("res://src/Components/SkinSelector/SkinComponent.tscn");

		AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");
		SkinsContainer = GetNode<VBoxContainer>("Popup/CanvasLayer/ScrollContainer/VBoxContainer/ContentPanelContainer/SkinComponentsContainer");
	}

	public void CreateSkinComponents(Action<Skin> onSelected)
	{
		foreach (var child in SkinsContainer.GetChildren())
			child.QueueFree();

		foreach (Skin skin in OsuData.Skins)
		{
			SkinComponent instance = SkinComponentScene.Instantiate<SkinComponent>();
			SkinsContainer.AddChild(instance);
			instance.SetValues(skin);
			instance.Button.Pressed += () =>
			{
				onSelected(skin);
				AnimationPlayer.Play("out");
			};
		}
	}

	public void In()
	{
		AnimationPlayer.Play("in");
	}
}
