using System;
using System.Collections.Generic;
using Godot;
using OsuSkinMixer.Components;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.StackScenes;

public partial class SkinManager : StackScene
{
    public override string Title => "Skin Manager";

    private PackedScene SkinInfoScene;

	private LineEdit SearchLineEdit;
	private Button SelectAllButton;
	private Button DeselectAllButton;
	private Button ManageSkinButton;
	private SkinComponentsContainer SkinComponentsContainer;

	private readonly List<OsuSkin> _checkedSkins = new();

    public override void _Ready()
    {
        SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");

		SearchLineEdit = GetNode<LineEdit>("%SearchLineEdit");
		SelectAllButton = GetNode<Button>("%SelectAllButton");
		DeselectAllButton = GetNode<Button>("%DeselectAllButton");
		ManageSkinButton = GetNode<Button>("%ManageSkinButton");
		SkinComponentsContainer = GetNode<SkinComponentsContainer>("%SkinComponentsContainer");

		SkinComponentsContainer.SkinComponentScene = GD.Load<PackedScene>("res://src/Components/SkinComponentSkinManager.tscn");
		SkinComponentsContainer.SkinSelected += OnSkinSelected;
		SkinComponentsContainer.SkinChecked += OnSkinChecked;
		SkinComponentsContainer.InitialiseSkinComponents();

		SearchLineEdit.TextChanged += SkinComponentsContainer.FilterSkins;
		SelectAllButton.Pressed += () => SkinComponentsContainer.SelectAll(true);
		DeselectAllButton.Pressed += () => SkinComponentsContainer.SelectAll(false);
    }

    private void OnSkinSelected(OsuSkin skin)
    {
		SkinInfo instance = SkinInfoScene.Instantiate<SkinInfo>();
		instance.Skins = new OsuSkin[] { skin };
		EmitSignal(SignalName.ScenePushed, instance);
    }

	private void OnSkinChecked(OsuSkin skin, bool isChecked)
	{
		if (isChecked)
			_checkedSkins.Add(skin);
		else
			_checkedSkins.Remove(skin);

		ManageSkinButton.Disabled = _checkedSkins.Count == 0;

		bool allChecked = _checkedSkins.Count == SkinComponentsContainer.GetChildCount();
		SelectAllButton.Visible = !allChecked;
		DeselectAllButton.Visible = allChecked;
	}
}
