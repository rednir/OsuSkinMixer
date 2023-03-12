using System;
using Godot;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Components;

public partial class SkinComponent : HBoxContainer
{
    public OsuSkin Skin { get; set; }

    public Action Pressed { get; set; }

    public Action<bool> Checked { get; set; }

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

		// Compact components don't have these nodes.
        if (AuthorLabel != null && HitcircleIcon != null)
        {
            AuthorLabel.Text = Skin.SkinIni?.TryGetPropertyValue("General", "Author");
            HitcircleIcon.SetSkin(Skin);
        }
    }

    private void OnButtonPressed()
        => Pressed.Invoke();

    private void OnCheckBoxToggled(bool value)
        => Checked.Invoke(value);
}
