using Godot;
using System;
using System.Linq;
using OsuSkinMixer.Statics;
using OsuSkinMixer.Models.Osu;

namespace OsuSkinMixer.Components.SkinSelector;

public partial class SkinSelectorPopup : Popup
{
	public Action<OsuSkin> OnSelected { get; set; }

	private PackedScene SkinComponentScene;

	private TextureButton BackButton;
	private LineEdit SearchLineEdit;
	private VBoxContainer SkinsContainer;

	public override void _Ready()
	{
		base._Ready();

		SkinComponentScene = GD.Load<PackedScene>("res://src/Components/SkinSelector/SkinComponent.tscn");

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

		foreach (OsuSkin skin in OsuData.Skins)
		{
			SkinComponent instance = SkinComponentScene.Instantiate<SkinComponent>();
			SkinsContainer.AddChild(instance);
			instance.Name = skin.Name;
			instance.SetValues(skin);
			instance.Button.Pressed += () => OnSkinSelected(skin);
		}
	}

	public override void In()
	{
		base.In();
		SearchLineEdit.Clear();
		SearchLineEdit.GrabFocus();
	}

	private void OnSkinSelected(OsuSkin skin)
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
