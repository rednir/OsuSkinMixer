using Godot;
using OsuSkinMixer.Models.SkinOptions;
using OsuSkinMixer.Components.SkinOptionsSelector;
using OsuSkinMixer.Components;
using System.Collections.Generic;

namespace OsuSkinMixer.StackScenes;

public partial class SkinMixer : StackScene
{
    public override string Title => "Skin Mixer";

    private IEnumerable<SkinOption> SkinOptions { get; } = SkinOption.Default;

    private SkinCreatorPopup SkinCreatorPopup;
    private SkinOptionsSelector SkinOptionsSelector;
    private Button CreateSkinButton;

    public override void _Ready()
    {
        SkinCreatorPopup = GetNode<SkinCreatorPopup>("SkinCreatorPopup");
        SkinOptionsSelector = GetNode<SkinOptionsSelector>("SkinOptionsSelector");
        CreateSkinButton = GetNode<Button>("CreateSkinButton");

        CreateSkinButton.Pressed += CreateSkinButtonPressed;

        SkinOptionsSelector.CreateOptionComponents(SkinOptions);
    }

    public void CreateSkinButtonPressed()
    {
        SkinCreatorPopup.CreateSkin("test", SkinOptions);
    }
}
