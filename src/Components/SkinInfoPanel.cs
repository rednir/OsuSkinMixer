using System;
using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using OsuSkinMixer.Utils;

namespace OsuSkinMixer.Components;

public partial class SkinInfoPanel : PanelContainer
{
	public OsuSkin Skin { get; set; }

	public Action ModifySkin { get; set; }

	private VBoxContainer DeletedContainer;
	private VBoxContainer MainContentContainer;
	private SkinPreview SkinPreview;
	private HitcircleIcon HitcircleIcon;
	private Label SkinNameLabel;
	private Label SkinAuthorLabel;
	private Button MoreButton;
	private Label DetailsLabel;
	private Button OpenFolderButton;
	private Button OpenInOsuButton;
	private ManageSkinPopup ManageSkinPopup;

	public override void _Ready()
	{
		DeletedContainer = GetNode<VBoxContainer>("%DeletedContainer");
		MainContentContainer = GetNode<VBoxContainer>("%MainContentContainer");
		SkinPreview = GetNode<SkinPreview>("%SkinPreview");
		HitcircleIcon = GetNode<HitcircleIcon>("%HitcircleIcon");
		SkinNameLabel = GetNode<Label>("%SkinName");
		SkinAuthorLabel = GetNode<Label>("%SkinAuthor");
		MoreButton = GetNode<Button>("%MoreButton");
		DetailsLabel = GetNode<Label>("%Details");
		OpenFolderButton = GetNode<Button>("%OpenFolderButton");
		OpenInOsuButton = GetNode<Button>("%OpenInOsuButton");
		ManageSkinPopup = GetNode<ManageSkinPopup>("%ManageSkinPopup");

		SkinPreview.SetSkin(Skin);
		HitcircleIcon.SetSkin(Skin);
		SkinNameLabel.Text = Skin.Name;
		SkinAuthorLabel.Text = Skin.SkinIni?.TryGetPropertyValue("General", "Author");
		MoreButton.Pressed += OnMoreButtonPressed;
		DetailsLabel.Text = $"Last modified: {Skin.Directory.LastWriteTime}";
		OpenFolderButton.Pressed += OnOpenFolderButtonPressed;
		OpenInOsuButton.Pressed += OnOpenInOsuButtonPressed;
		ManageSkinPopup.SetSkin(Skin);
		ManageSkinPopup.ModifySkin += OnModifySkin;

		OsuData.SkinRemoved += OnSkinRemoved;
	}

	private void OnOpenFolderButtonPressed()
	{
		OS.ShellOpen(Skin.Directory.FullName);
	}

	private void OnOpenInOsuButtonPressed()
	{
        SkinMachine.TriggerOskImport(Skin);
	}

	private void OnMoreButtonPressed()
	{
		ManageSkinPopup.In();
	}

	private void OnModifySkin()
	{
		ModifySkin();
	}

	private void OnSkinRemoved(OsuSkin skin)
	{
		if (skin != Skin)
			return;

		MainContentContainer.Visible = false;
		DeletedContainer.Visible = true;
	}
}
