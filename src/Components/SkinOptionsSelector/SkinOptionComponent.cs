namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;

public partial class SkinOptionComponent : HBoxContainer
{
    public event Action OnButtonPressed;

    public event Action OnResetButtonPressed;

    public event Action<bool> OnArrowButtonToggled;

    public SkinOptionValue DefaultValue { get; private set; }

    public SkinOption SkinOption { get; private set; }

    public VBoxContainer ParentContainer { get; set; }

    public VBoxContainer ChildrenContainer { get; set; }

    private int _indentLayer;

    private Label Label;
    private Label SpecialTextLabel;
    private Button Button;
    private TextureButton ArrowButton;
    private TextureButton ResetButton;

    public override void _Ready()
    {
        Button = GetNode<Button>("Button");
        ResetButton = GetNode<TextureButton>("Button/ResetButton");
        ArrowButton = GetNode<TextureButton>("ArrowButton");
        Label = GetNode<Label>("Label");
        SpecialTextLabel = GetNode<Label>("Button/SpecialText");

        Button.Pressed += () => OnButtonPressed?.Invoke();
        ResetButton.Pressed += () => OnResetButtonPressed?.Invoke();
        ArrowButton.Toggled += p => OnArrowButtonToggled?.Invoke(p);
        ArrowButton.TooltipText = SkinOption.ToString();
        Label.Text = SkinOption.Name;

        // Disable button to expand option if the option has no children;
        if (SkinOption is not ParentSkinOption)
            ArrowButton.Disabled = true;

        Panel indent = new()
        {
            CustomMinimumSize = new Vector2(_indentLayer * 30, 1),
            Modulate = new Color(0, 0, 0, 0),
        };

        AddChild(indent);
        MoveChild(indent, 0);

        UpdateNodeValuesToOptionValue();
    }

    public void SetSkinOption(SkinOption option, SkinOptionValue defaultValue, int indentLayer)
    {
        SkinOption = option;
        DefaultValue = defaultValue;
        _indentLayer = indentLayer;

        Name = option.Name;

        SetOptionValue(defaultValue);
    }

    public bool CreateChildrenContainer()
    {
        if (ChildrenContainer is null)
        {
            ChildrenContainer = new VBoxContainer()
            {
                CustomMinimumSize = new Vector2(10, 0),
                Visible = false,
            };
            ChildrenContainer.AddThemeConstantOverride("separation", 8);
            ParentContainer.AddChild(ChildrenContainer);
            ParentContainer.MoveChild(ChildrenContainer, GetIndex() + 1);

            return true;
        }

        return false;
    }

    public void FocusButton()
    {
        Button?.GrabFocus();
    }

    public void SetOptionValue(SkinOptionValue value)
    {
        SkinOption.Value = value;
        UpdateNodeValuesToOptionValue();
    }

    private void UpdateNodeValuesToOptionValue()
    {
        SkinOptionValue value = SkinOption.Value;

        // Return if we aren't in the scene tree yet (due to lazy init).
        if (Button is null)
            return;

        Button.SetDeferred(Button.PropertyName.TooltipText, string.Empty);
        Button.SetDeferred(Button.PropertyName.Text, string.Empty);

        switch (value.Type)
        {
            case SkinOptionValueType.Various:
                SpecialTextLabel.SetDeferred(Label.PropertyName.Text, "Various skins");
                break;
            case SkinOptionValueType.Unchanged:
                SpecialTextLabel.SetDeferred(Label.PropertyName.Text, "Unchanged");
                break;
            case SkinOptionValueType.DefaultSkin:
                SpecialTextLabel.SetDeferred(Label.PropertyName.Text, "Default skin");
                break;
            case SkinOptionValueType.Blank:
                SpecialTextLabel.SetDeferred(Label.PropertyName.Text, "Blank file");
                break;
            case SkinOptionValueType.CustomSkin:
                SpecialTextLabel.SetDeferred(Label.PropertyName.Text, string.Empty);
                Button.SetDeferred(Button.PropertyName.TooltipText, value.CustomSkin.Name);
                Button.SetDeferred(Button.PropertyName.Text, value.CustomSkin.Name);
                break;
        }

        ResetButton.SetDeferred(Button.PropertyName.Visible, value != DefaultValue);
    }
}
