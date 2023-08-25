namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;

public partial class SkinOptionComponent : HBoxContainer
{
    public TextureButton ArrowButton { get; private set; }

    public TextureButton ResetButton { get; private set; }

    public Button Button { get; private set; }

    public SkinOptionValue DefaultValue { get; private set; }

    public SkinOption SkinOption { get; private set; }

    private Label Label;
    private Label SpecialTextLabel;

    public override void _Ready()
    {
        ArrowButton = GetNode<TextureButton>("ArrowButton");
        Label = GetNode<Label>("Label");
        Button = GetNode<Button>("Button");
        ResetButton = GetNode<TextureButton>("Button/ResetButton");
        SpecialTextLabel = GetNode<Label>("Button/SpecialText");
    }

    public void SetSkinOption(SkinOption option, SkinOptionValue defaultValue, int indentLayer)
    {
        SkinOption = option;
        ArrowButton.TooltipText = option.ToString();
        Name = option.Name;
        Label.Text = option.Name;
        DefaultValue = defaultValue;
        SetValue(defaultValue);

        // Disable button to expand option if the option has no children;
        if (option is not ParentSkinOption)
            ArrowButton.Disabled = true;

        var indent = new Panel()
        {
            CustomMinimumSize = new Vector2(indentLayer * 30, 1),
            Modulate = new Color(0, 0, 0, 0),
        };

        AddChild(indent);
        MoveChild(indent, 0);
    }

    public void SetValue(SkinOptionValue value)
    {
        SkinOption.Value = value;
        Button.SetDeferred(Button.PropertyName.TooltipText, string.Empty);
        Button.SetDeferred(Button.PropertyName.Text, string.Empty);

        switch (value.Type)
        {
            case SkinOptionValueType.Various:
                SpecialTextLabel.SetDeferred(Label.PropertyName.Text, "<<VARIOUS>>");
                break;
            case SkinOptionValueType.Unchanged:
                SpecialTextLabel.SetDeferred(Label.PropertyName.Text, "<<UNCHANGED>>");
                break;
            case SkinOptionValueType.DefaultSkin:
                SpecialTextLabel.SetDeferred(Label.PropertyName.Text, "<<DEFAULT SKIN>>");
                break;
            case SkinOptionValueType.Blank:
                SpecialTextLabel.SetDeferred(Label.PropertyName.Text, "<<BLANK>>");
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
