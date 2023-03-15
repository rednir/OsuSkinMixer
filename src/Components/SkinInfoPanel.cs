using System;
using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using OsuSkinMixer.Utils;

namespace OsuSkinMixer.Components;

public partial class SkinInfoPanel : PanelContainer
{
	public OsuSkin Skin { get; set; }

	private VBoxContainer DeletedContainer;
	private VBoxContainer MainContentContainer;
	private SkinPreview SkinPreview;
	private HitcircleIcon HitcircleIcon;
	private Label SkinNameLabel;
	private Label SkinAuthorLabel;
	private Button MoreButton;
	private Label DetailsLabel;
	private TextureRect HiddenIcon;
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
		HiddenIcon = GetNode<TextureRect>("%HiddenIcon");
		OpenFolderButton = GetNode<Button>("%OpenFolderButton");
		OpenInOsuButton = GetNode<Button>("%OpenInOsuButton");
		ManageSkinPopup = GetNode<ManageSkinPopup>("%ManageSkinPopup");

		MoreButton.Pressed += OnMoreButtonPressed;
		OpenFolderButton.Pressed += OnOpenFolderButtonPressed;
		OpenInOsuButton.Pressed += OnOpenInOsuButtonPressed;

		OsuData.SkinAdded += OnSkinAdded;
		OsuData.SkinModified += OnSkinModified;
		OsuData.SkinRemoved += OnSkinRemoved;

		SetValues();
	}

	public override void _ExitTree()
	{
		OsuData.SkinAdded -= OnSkinAdded;
		OsuData.SkinModified -= OnSkinModified;
		OsuData.SkinRemoved -= OnSkinRemoved;
	}

	private void SetValues()
	{
		SkinPreview.SetSkin(Skin);
		HitcircleIcon.SetSkin(Skin);
		SkinNameLabel.Text = Skin.Name;
		SkinAuthorLabel.Text = Skin.SkinIni?.TryGetPropertyValue("General", "Author");
		DetailsLabel.Text = $"Last modified: {Skin.Directory.LastWriteTime}";
		HiddenIcon.Visible = Skin.Hidden;
		OpenInOsuButton.Disabled = Skin.Hidden;
		ManageSkinPopup.SetSkin(Skin);
	}

	private void OnSkinAdded(OsuSkin skin)
	{
		if (skin != Skin)
			return;

		MainContentContainer.Visible = true;
		DeletedContainer.Visible = false;
		SetValues();
	}

	private void OnSkinModified(OsuSkin skin)
	{
		if (skin != Skin)
			return;

		SetValues();
	}

	private void OnSkinRemoved(OsuSkin skin)
	{
		if (skin != Skin)
			return;

		MainContentContainer.Visible = false;
		DeletedContainer.Visible = true;
	}

	private void OnOpenFolderButtonPressed()
	{
		Tools.ShellOpenFile(Skin.Directory.FullName);
	}

	private void OnOpenInOsuButtonPressed()
	{
        Tools.TriggerOskImport(Skin);
	}

	private void OnMoreButtonPressed()
	{
		ManageSkinPopup.In();
	}
}
