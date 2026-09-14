namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;
using OsuSkinMixer.src.Models.Osu;
using OsuSkinMixer.Statics;

public partial class SkinComponent : HBoxContainer
{
    public OsuSkin Skin { get; set; }

    public Action LeftClicked { get; set; }

    public Action RightClicked { get; set; }

    public Action<bool> Checked { get; set; }

    public Action<bool, Action> PreviewRequested { get; set; }

    public bool IsChecked
    {
        get => CheckBox?.ButtonPressed ?? false;
        set
        {
            if (CheckBox is not null)
                CheckBox.ButtonPressed = value;
        }
    }

    public bool CheckBoxVisible
    {
        get => visibleCheckBox;
        set
        {
            visibleCheckBox = value;
            if (CheckBox is not null)
                CheckBox.Visible = value;
        }
    }

    public bool PreviewButtonVisible
    {
        get => visiblePreviewButton;
        set
        {
            visiblePreviewButton = value;
            if (PreviewButton is not null)
                PreviewButton.Visible = value;
        }
    }

    public int CreditPercentage
    {
        get => _creditPercentage;
        set
        {
            _creditPercentage = value;
            SetCreditPercentageLabelText();
        }
    }

    // TODO: move this to a subclass (and all the other credits stuff)    
    public OsuSkinCreditsElement[] CreditsElements { get; set; }

    private int _creditPercentage;

    private bool visibleCheckBox;

    private bool visiblePreviewButton;

    private Button Button;
    private Label NameLabel;
    private Label AuthorLabel;
    private HitcircleIcon HitcircleIcon;
    private CheckBox CheckBox;
    private TextureRect HiddenIcon;
    private Label CreditPercentageLabel;
    private Button PreviewButton;
    private AnimationPlayer PreviewAnimationPlayer;

    public override void _Ready()
    {
        Button = GetNode<Button>("%Button");
        NameLabel = GetNode<Label>("%Name");
        AuthorLabel = GetNodeOrNull<Label>("%Author");
        HitcircleIcon = GetNodeOrNull<HitcircleIcon>("%HitcircleIcon");
        CheckBox = GetNodeOrNull<CheckBox>("%CheckBox");
        HiddenIcon = GetNodeOrNull<TextureRect>("%HiddenIcon");
        CreditPercentageLabel = GetNodeOrNull<Label>("%CreditPercentage");
        PreviewButton = GetNodeOrNull<Button>("%PreviewButton");
        PreviewAnimationPlayer = GetNodeOrNull<AnimationPlayer>("%PreviewAnimationPlayer");

        Button.Pressed += OnButtonPressed;
        Button.GuiInput += OnGuiInput;

        if (PreviewButton != null)
            PreviewButton.Pressed += OnPreviewButtonPressed;

        if (CheckBox != null)
            CheckBox.Toggled += OnCheckBoxToggled;

        if (Skin != null)
            SetValues();
    }

    public void SetValues()
    {
        NameLabel.SetDeferred(Label.PropertyName.Text, Skin.Name);
        Button.SetDeferred(Button.PropertyName.TooltipText, $"{Skin.Name}\nRight click for options...");
        CheckBox.SetDeferred(CheckBox.PropertyName.Visible, CheckBoxVisible);
        PreviewButton?.SetDeferred(Button.PropertyName.Visible, PreviewButtonVisible);
        SetCreditPercentageLabelText();

        // Compact components don't have these nodes.
        if (AuthorLabel != null && HitcircleIcon != null)
        {
            AuthorLabel.Text = Skin.SkinIni?.TryGetPropertyValue("General", "Author");
            HitcircleIcon.SetSkin(Skin);

            AuthorLabel.SetDeferred(Label.PropertyName.Text, Skin.SkinIni?.TryGetPropertyValue("General", "Author"));
        }

        // Only compact components have this node, otherwise it is found in HitcircleIcon.
        HiddenIcon?.SetDeferred(TextureRect.PropertyName.Visible, Skin.Hidden);
    }

    private void OnButtonPressed()
    {
        if (LeftClicked == null)
        {
            if (Skin?.Record == null && Skin?.Directory is null)
            {
                Settings.PushToast("You don't seem to have that skin downloaded.");
                return;
            }

            OsuData.RequestSkinInfo(new OsuSkin[] { Skin });
            return;
        }

        LeftClicked();
    }

    private void OnGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseButton
            && mouseButton.ButtonIndex == MouseButton.Right
            && mouseButton.Pressed)
        {
            if (Skin?.Record == null && Skin?.Directory is null)
            {
                Settings.PushToast("You don't seem to have that skin downloaded.");
                return;
            }

            RightClicked?.Invoke();
        }
    }

    private void OnPreviewButtonPressed()
    {
        PreviewRequested?.Invoke(PreviewButton.ButtonPressed, OnPreviewFinished);
        PreviewAnimationPlayer?.Play(PreviewButton.ButtonPressed ? "in" : "out");
    }

    private void OnCheckBoxToggled(bool value)
    {
        Checked?.Invoke(value);
    }

    private void OnPreviewFinished()
    {
        PreviewButton?.SetDeferred(Button.PropertyName.ButtonPressed, false);
        PreviewAnimationPlayer?.CallDeferred(AnimationPlayer.MethodName.Play, "out");
    }

    private void SetCreditPercentageLabelText()
    {
        if (CreditPercentageLabel is not null)
            CreditPercentageLabel.SetDeferred(Label.PropertyName.Text, CreditPercentage < 1 ? "<1%" : $"{CreditPercentage}%");
    }
}
