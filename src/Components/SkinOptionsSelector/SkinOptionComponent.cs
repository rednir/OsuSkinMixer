using Godot;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Components;

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
        Button.TooltipText = null;
        Button.Text = string.Empty;

        switch (value.Type)
        {
            case SkinOptionValueType.Various:
                SpecialTextLabel.Text = "<<VARIOUS>>";
                break;
            case SkinOptionValueType.Unchanged:
                SpecialTextLabel.Text = "<<UNCHANGED>>";
                break;
            case SkinOptionValueType.DefaultSkin:
                SpecialTextLabel.Text = "<<DEFAULT SKIN>>";
                break;
            case SkinOptionValueType.Blank:
                SpecialTextLabel.Text = "<<BLANK>>";
                break;
            case SkinOptionValueType.CustomSkin:
                SpecialTextLabel.Text = string.Empty;
                Button.Text = value.CustomSkin.Name;
                Button.TooltipText = value.CustomSkin.Name;
                break;
        }

        ResetButton.Visible = value != DefaultValue;
    }
}
