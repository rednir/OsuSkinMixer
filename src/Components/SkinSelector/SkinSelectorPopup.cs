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

		BackButton.Pressed += Out;
		SearchLineEdit.TextChanged += OnSearchTextChanged;
		SearchLineEdit.TextSubmitted += OnSearchTextSubmitted;

		OsuData.SkinAdded += OnSkinAdded;
		OsuData.SkinModified += OnSkinModified;
		OsuData.SkinRemoved += OnSkinRemoved;

		AddAllSkinComponents();
	}

	public override void In()
	{
		base.In();
		SearchLineEdit.Clear();
		SearchLineEdit.GrabFocus();
	}

	private void AddAllSkinComponents()
	{
		foreach (var child in SkinsContainer.GetChildren())
			child.QueueFree();

		foreach (OsuSkin skin in OsuData.Skins)
			SkinsContainer.AddChild(SkinComponentFrom(skin));
	}

	private SkinComponent SkinComponentFrom(OsuSkin skin)
	{
		SkinComponent instance = SkinComponentScene.Instantiate<SkinComponent>();
		instance.Skin = skin;
		instance.Name = skin.Name;
		instance.Pressed += () => OnSkinSelected(skin);

		return instance;
	}

	private void OnSkinAdded(OsuSkin skin)
	{
		var skinComponent = SkinComponentFrom(skin);
		SkinsContainer.AddChild(skinComponent);
		SkinsContainer.MoveChild(skinComponent, Array.IndexOf(OsuData.Skins, skin));
	}

	private void OnSkinModified(OsuSkin skin)
	{
		var skinComponent = SkinsContainer
			.GetChildren()
			.Cast<SkinComponent>()
			.FirstOrDefault(c => c.Skin.Name == skin.Name);

		skinComponent.Skin = skin;
		skinComponent.SetValues();
	}

	private void OnSkinRemoved(OsuSkin skin)
	{
		SkinsContainer
			.GetChildren()
			.Cast<SkinComponent>()
			.FirstOrDefault(c => c.Skin.Name == skin.Name)?
			.QueueFree();
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
