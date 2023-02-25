using Godot;
using System;
using Skin = OsuSkinMixer.Models.Osu.Skin;
using OsuSkinMixer.Statics;
using System.Linq;

namespace OsuSkinMixer.Components.SkinSelector;

public partial class SkinSelectorPopup : Control
{
	private PackedScene SkinComponentScene;

	private AnimationPlayer AnimationPlayer;
	private LineEdit SearchLineEdit;
	private VBoxContainer SkinsContainer;

	public override void _Ready()
	{
		SkinComponentScene = GD.Load<PackedScene>("res://src/Components/SkinSelector/SkinComponent.tscn");

		AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");
		SkinsContainer = GetNode<VBoxContainer>("%SkinComponentsContainer");
		SearchLineEdit = GetNode<LineEdit>("%SearchLineEdit");

		SearchLineEdit.TextChanged += OnSearchTextChanged;
	}

	public void CreateSkinComponents(Action<Skin> onSelected)
	{
		foreach (var child in SkinsContainer.GetChildren())
			child.QueueFree();

		foreach (Skin skin in OsuData.Skins)
		{
			SkinComponent instance = SkinComponentScene.Instantiate<SkinComponent>();
			SkinsContainer.AddChild(instance);
			instance.Name = skin.Name;
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

	private void OnSearchTextChanged(string text)
	{
		foreach (var component in SkinsContainer.GetChildren().Cast<SkinComponent>())
			component.Visible = component.Name.ToString().Contains(text, StringComparison.OrdinalIgnoreCase);
	}
}
