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

    public override void _Ready()
    {
        ArrowButton = GetNode<TextureButton>("ArrowButton");
        Label = GetNode<Label>("Label");
        Button = GetNode<Button>("Button");
        ResetButton = GetNode<TextureButton>("Button/ResetButton");
    }

    public void SetSkinOption(SkinOption option, SkinOptionValue defaultValue, int indentLayer)
    {
        SkinOption = option;
        TooltipText = option.ToString();
        Name = option.Name;
        Label.Text = option.Name;
		SetValue(defaultValue);

        if (option is not ParentSkinOption)
        {
            // Option has no children, so hide button to expand option.
            // Don't set visible to false so the button still takes up space. 
            ArrowButton.Disabled = true;
            ArrowButton.Modulate = new Color(0, 0, 0, 0);
        }

        var indent = new Panel()
        {
            CustomMinimumSize = new Vector2(indentLayer * 30, 1),
            Modulate = new Color(0, 0, 0, 0),
        };

        AddChild(indent);
        MoveChild(indent, 0);
    }

	public void SetValue(OsuSkin skin)
		=> SetValue(new SkinOptionValue(SkinOptionValueType.CustomSkin, skin));

    public void SetValue(SkinOptionValue value)
    {
        SkinOption.Value = value;
		Button.TooltipText = null;

        switch (value.Type)
        {
			case SkinOptionValueType.Various:
				Button.Text = "<<VARIOUS>>";
				break;
            case SkinOptionValueType.Unchanged:
                Button.Text = "<<UNCHANGED>>";
                break;
            case SkinOptionValueType.DefaultSkin:
                Button.Text = "<<DEFAULT SKIN>>";
                break;
            case SkinOptionValueType.Blank:
                Button.Text = "<<BLANK>>";
                break;
            case SkinOptionValueType.CustomSkin:
                Button.Text = value.CustomSkin.Name;
                Button.TooltipText = value.CustomSkin.Name;
                break;
        }

        ResetButton.Visible = value != DefaultValue;
    }
}
