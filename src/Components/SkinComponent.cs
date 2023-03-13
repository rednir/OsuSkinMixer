using System;
using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components;

public partial class SkinComponent : HBoxContainer
{
    public OsuSkin Skin { get; set; }

    public Action Pressed { get; set; }

    public Action<bool> Checked { get; set; }

    public bool IsChecked
    {
        get => CheckBox?.ButtonPressed ?? false;
        set
        {
            if (CheckBox != null)
                CheckBox.ButtonPressed = value;
        }
    }

    public bool CheckBoxVisible
    {
        get => visibleCheckBox;
        set
        {
            visibleCheckBox = value;
            if (CheckBox != null)
                CheckBox.Visible = value;
        }
    }

    private bool visibleCheckBox;

    private Button Button;
    private Label NameLabel;
    private Label AuthorLabel;
    private HitcircleIcon HitcircleIcon;
    private CheckBox CheckBox;
    private TextureRect HiddenIcon;

    public override void _Ready()
    {
        Button = GetNode<Button>("%Button");
        NameLabel = GetNode<Label>("%Name");
        AuthorLabel = GetNodeOrNull<Label>("%Author");
        HitcircleIcon = GetNodeOrNull<HitcircleIcon>("%HitcircleIcon");
        CheckBox = GetNodeOrNull<CheckBox>("%CheckBox");
        HiddenIcon = GetNode<TextureRect>("%HiddenIcon");

        Button.Pressed += OnButtonPressed;

        if (CheckBox != null)
            CheckBox.Toggled += OnCheckBoxToggled;

        if (Skin != null)
            SetValues();
    }

    public void SetValues()
    {
        NameLabel.Text = Skin.Name;
        Button.TooltipText = Skin.Name;
        HiddenIcon.Visible = Skin.Hidden;
        CheckBox.Visible = CheckBoxVisible;

        // Compact components don't have these nodes.
        if (AuthorLabel != null && HitcircleIcon != null)
        {
            AuthorLabel.Text = Skin.SkinIni?.TryGetPropertyValue("General", "Author");
            HitcircleIcon.SetSkin(Skin);
        }
    }

    private void OnButtonPressed()
    {
        if (Pressed == null)
        {
            OsuData.RequestSkinInfo(new OsuSkin[] { Skin });
            return;
        }

        Pressed();
    }

    private void OnCheckBoxToggled(bool value)
    {
        Checked?.Invoke(value);
    }
}
