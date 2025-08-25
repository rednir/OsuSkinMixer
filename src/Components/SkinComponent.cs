namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

public partial class SkinComponent : HBoxContainer
{
    public OsuSkin Skin { get; set; }

    public Action LeftClicked { get; set; }

    public Action RightClicked { get; set; }

    public Action<bool> Checked { get; set; }

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

    public int CreditPercentage
    {
        get => _creditPercentage;
        set
        {
            _creditPercentage = value;
            SetCreditPercentageLabelText();
        }
    }

    private int _creditPercentage;

    private bool visibleCheckBox;

    private Button Button;
    private Label NameLabel;
    private Label AuthorLabel;
    private HitcircleIcon HitcircleIcon;
    private CheckBox CheckBox;
    private TextureRect HiddenIcon;
    private Label CreditPercentageLabel;

    public override void _Ready()
    {
        Button = GetNode<Button>("%Button");
        NameLabel = GetNode<Label>("%Name");
        AuthorLabel = GetNodeOrNull<Label>("%Author");
        HitcircleIcon = GetNodeOrNull<HitcircleIcon>("%HitcircleIcon");
        CheckBox = GetNodeOrNull<CheckBox>("%CheckBox");
        HiddenIcon = GetNodeOrNull<TextureRect>("%HiddenIcon");
        CreditPercentageLabel = GetNodeOrNull<Label>("%CreditPercentage");

        Button.Pressed += OnButtonPressed;
        Button.GuiInput += OnGuiInput;

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
            if (Skin?.Directory is null)
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
            if (Skin?.Directory is null)
            {
                Settings.PushToast("You don't seem to have that skin downloaded.");
                return;
            }

            RightClicked?.Invoke();
        }
    }

    private void OnCheckBoxToggled(bool value)
    {
        Checked?.Invoke(value);
    }

    private void SetCreditPercentageLabelText()
    {
        if (CreditPercentageLabel is not null)
            CreditPercentageLabel.SetDeferred(Label.PropertyName.Text, CreditPercentage < 1 ? "<1%" : $"{CreditPercentage}%");
    }
}
