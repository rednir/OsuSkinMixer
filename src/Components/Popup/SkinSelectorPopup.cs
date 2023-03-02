using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components;

public partial class SkinSelectorPopup : Popup
{
	public Action<OsuSkin> OnSelected { get; set; }

	private PackedScene SkinComponentScene;

	private readonly List<SkinComponent> DisabledSkinComponents = new();

	private TextureButton BackButton;
	private LineEdit SearchLineEdit;
	private VBoxContainer SkinsContainer;

	public override void _Ready()
	{
		base._Ready();

		SkinComponentScene = GD.Load<PackedScene>("res://src/Components/SkinComponent.tscn");

		BackButton = GetNode<TextureButton>("%BackButton");
		SkinsContainer = GetNode<VBoxContainer>("%SkinComponentsContainer");
		SearchLineEdit = GetNode<LineEdit>("%SearchLineEdit");

		BackButton.Pressed += Out;
		SearchLineEdit.TextChanged += OnSearchTextChanged;
		SearchLineEdit.TextSubmitted += OnSearchTextSubmitted;

		OsuData.SkinAdded += OnSkinAdded;
		OsuData.SkinModified += OnSkinModified;
		OsuData.SkinRemoved += OnSkinRemoved;

		TreeExiting += () =>
		{
			OsuData.SkinAdded -= OnSkinAdded;
			OsuData.SkinModified -= OnSkinModified;
			OsuData.SkinRemoved -= OnSkinRemoved;
		};

		AddAllSkinComponents();
	}

	public override void In()
	{
		base.In();
		SearchLineEdit.Clear();
		SearchLineEdit.GrabFocus();
	}

	public void DisableSkinComponent(OsuSkin skin)
	{
		var skinComponent = GetExistingComponentFromSkin(skin);
		skinComponent.Visible = false;
		DisabledSkinComponents.Add(skinComponent);
	}

	public void EnableSkinComponent(OsuSkin skin)
	{
		var skinComponent = GetExistingComponentFromSkin(skin);
		skinComponent.Visible = true;
		DisabledSkinComponents.Remove(skinComponent);
	}

	private void AddAllSkinComponents()
	{
		foreach (var child in SkinsContainer.GetChildren())
			child.QueueFree();

		foreach (OsuSkin skin in OsuData.Skins)
			SkinsContainer.AddChild(CreateSkinComponentFrom(skin));
	}

	private SkinComponent CreateSkinComponentFrom(OsuSkin skin)
	{
		SkinComponent instance = SkinComponentScene.Instantiate<SkinComponent>();
		instance.Skin = skin;
		instance.Name = skin.Name;
		instance.Pressed += () => OnSkinSelected(skin);

		return instance;
	}

	private SkinComponent GetExistingComponentFromSkin(OsuSkin skin)
	{
		return SkinsContainer
			.GetChildren()
			.Cast<SkinComponent>()
			.FirstOrDefault(c => c.Skin == skin);
	}

	private void OnSkinAdded(OsuSkin skin)
	{
		var skinComponent = CreateSkinComponentFrom(skin);
		SkinsContainer.AddChild(skinComponent);
		SkinsContainer.MoveChild(skinComponent, Array.IndexOf(OsuData.Skins, skin));
	}

	private void OnSkinModified(OsuSkin skin)
	{
		var skinComponent = GetExistingComponentFromSkin(skin);
		skinComponent.Skin = skin;
		skinComponent.SetValues();
	}

	private void OnSkinRemoved(OsuSkin skin)
	{
		GetExistingComponentFromSkin(skin).QueueFree();
	}

	private void OnSkinSelected(OsuSkin skin)
	{
		OnSelected(skin);
		Out();
	}

	private void OnSearchTextChanged(string text)
	{
		foreach (var component in SkinsContainer.GetChildren().Cast<SkinComponent>())
        {
            component.Visible = component.Name.ToString().Contains(text, StringComparison.OrdinalIgnoreCase)
				&& !DisabledSkinComponents.Contains(component);
        }
    }

	private void OnSearchTextSubmitted(string text)
	{
		SkinComponent selectedComponent = SkinsContainer.GetChildren().Cast<SkinComponent>().FirstOrDefault(c => c.Visible);
		if (selectedComponent != null)
			OnSkinSelected(selectedComponent.Skin);
	}
}
