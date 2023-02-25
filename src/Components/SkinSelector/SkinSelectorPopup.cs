using Godot;
using System;
using System.Linq;
using Skin = OsuSkinMixer.Models.Osu.Skin;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components.SkinSelector;

public partial class SkinSelectorPopup : Control
{
	public Action<Skin> OnSelected { get; set; }

	private PackedScene SkinComponentScene;

	private AnimationPlayer AnimationPlayer;
	private TextureButton BackButton;
	private LineEdit SearchLineEdit;
	private VBoxContainer SkinsContainer;

	public override void _Ready()
	{
		SkinComponentScene = GD.Load<PackedScene>("res://src/Components/SkinSelector/SkinComponent.tscn");

		AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");
		BackButton = GetNode<TextureButton>("%BackButton");
		SkinsContainer = GetNode<VBoxContainer>("%SkinComponentsContainer");
		SearchLineEdit = GetNode<LineEdit>("%SearchLineEdit");

		BackButton.Pressed += () => Out();
		SearchLineEdit.TextChanged += OnSearchTextChanged;
		SearchLineEdit.TextSubmitted += OnSearchTextSubmitted;

		CreateSkinComponents();
	}

	public void CreateSkinComponents()
	{
		foreach (var child in SkinsContainer.GetChildren())
			child.QueueFree();

		foreach (Skin skin in OsuData.Skins)
		{
			SkinComponent instance = SkinComponentScene.Instantiate<SkinComponent>();
			SkinsContainer.AddChild(instance);
			instance.Name = skin.Name;
			instance.SetValues(skin);
			instance.Button.Pressed += () => OnSkinSelected(skin);
		}
	}

	public void In()
	{
		AnimationPlayer.Play("in");
		SearchLineEdit.GrabFocus();
	}

	private void Out()
	{
		AnimationPlayer.Play("out");
	}

	private void OnSkinSelected(Skin skin)
	{
		OnSelected(skin);
		Out();
	}

	private void OnSearchTextChanged(string text)
	{
		foreach (var component in SkinsContainer.GetChildren().Cast<SkinComponent>())
			component.Visible = component.Name.ToString().Contains(text, StringComparison.OrdinalIgnoreCase);
	}

	private void OnSearchTextSubmitted(string text)
	{
		SkinComponent selectedComponent = SkinsContainer.GetChildren().Cast<SkinComponent>().FirstOrDefault(c => c.Visible);
		if (selectedComponent != null)
			OnSkinSelected(selectedComponent.Skin);
	}
}
