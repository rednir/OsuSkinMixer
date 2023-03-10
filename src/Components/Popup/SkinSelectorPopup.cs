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

	private bool _isCompact;

	private Button BackButton;
	private LineEdit SearchLineEdit;
	private VBoxContainer SkinOptionsContainer;
	private VBoxContainer SkinsContainer;
	private Button GetMoreSkinsButton;
	private GetMoreSkinsPopup GetMoreSkinsPopup;

	public override void _Ready()
	{
		base._Ready();
		SetCompactFlag();

		BackButton = GetNode<Button>("%BackButton");
		SkinsContainer = GetNode<VBoxContainer>("%SkinComponentsContainer");
		SkinOptionsContainer = GetNode<VBoxContainer>("%SkinOptionsContainer");
		SearchLineEdit = GetNode<LineEdit>("%SearchLineEdit");
		GetMoreSkinsButton = GetNode<Button>("%GetMoreSkinsButton");
		GetMoreSkinsPopup = GetNode<GetMoreSkinsPopup>("%GetMoreSkinsPopup");

		BackButton.Pressed += Out;
		SearchLineEdit.TextChanged += OnSearchTextChanged;
		SearchLineEdit.TextSubmitted += OnSearchTextSubmitted;
		GetMoreSkinsButton.Pressed += GetMoreSkinsPopup.In;

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

		if (Settings.Content.UseCompactSkinSelector != _isCompact)
		{
			SetCompactFlag();
			AddAllSkinComponents();
		}

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

	private void SetCompactFlag()
	{
		_isCompact = Settings.Content.UseCompactSkinSelector;
		SkinComponentScene = _isCompact
			? GD.Load<PackedScene>("res://src/Components/SkinComponentCompact.tscn")
			: GD.Load<PackedScene>("res://src/Components/SkinComponent.tscn");
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
			.FirstOrDefault(c => c.Skin.Name == skin.Name);
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
	}

	private void OnSearchTextChanged(string text)
	{
		SkinOptionsContainer.Visible = string.IsNullOrWhiteSpace(text);

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
