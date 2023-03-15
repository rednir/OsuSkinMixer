using Godot;
using System;

namespace OsuSkinMixer.Components;

public partial class OkPopup : Popup
{
    private Label Title;
    private Label Text;
    private Button OkButton;

    public override void _Ready()
    {
        base._Ready();

        Title = GetNode<Label>("%Title");
        Text = GetNode<Label>("%Text");
        OkButton = GetNode<Button>("%OkButton");
        OkButton.Pressed += Out;
    }

    public void SetValues(string text, string title)
    {
        Title.Text = title;
        Text.Text = text;
    }
}
