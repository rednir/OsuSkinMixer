using System.Collections.Generic;
using Godot;
using OsuSkinMixer.Components;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.StackScenes;

public partial class SkinModifierSkinSelect : StackScene
{
    public override string Title => "Skin Modifier";

    public List<OsuSkin> SkinsToModify { get; set; } = new();

    private PackedScene SkinModifierModificationSelectScene;
    private PackedScene SkinComponentScene;

    private VBoxContainer SkinsToModifyContainer;
    private Button AddSkinToModifyButton;
    private Button ContinueButton;
    private SkinSelectorPopup SkinSelectorPopup;

    public override void _Ready()
    {
        SkinModifierModificationSelectScene = GD.Load<PackedScene>("res://src/StackScenes/SkinModifierModificationSelect.tscn");
        SkinComponentScene = GD.Load<PackedScene>("res://src/Components/SkinComponentSkinManager.tscn");

        SkinsToModifyContainer = GetNode<VBoxContainer>("%SkinsToModifyContainer");
        AddSkinToModifyButton = GetNode<Button>("%AddSkinToModifyButton");
        ContinueButton = GetNode<Button>("%ContinueButton");
        SkinSelectorPopup = GetNode<SkinSelectorPopup>("%SkinSelectorPopup");

        ContinueButton.Pressed += OnContinueButtonPressed;
        AddSkinToModifyButton.Pressed += AddSkinToModifyButtonPressed;

        SkinSelectorPopup.OnSelected = OnSkinSelected;

        // Add components if the scene has been initalised with skins to modify.
        foreach (var skin in SkinsToModify)
            AddSkinComponent(skin);
    }

    private void AddSkinToModifyButtonPressed()
    {
        SkinSelectorPopup.In();
    }

    private void OnSkinSelected(OsuSkin skin)
    {
        SkinSelectorPopup.Out();
        AddSkinComponent(skin);
        SkinsToModify.Add(skin);
    }

    private void AddSkinComponent(OsuSkin skin)
    {
        // Don't show already selected skins in the selector.
        SkinSelectorPopup.DisableSkinComponent(skin);

        var component = SkinComponentScene.Instantiate<SkinComponent>();
        component.Skin = skin;
        component.Visible = true;
        component.CheckBoxVisible = true;
        SkinsToModifyContainer.AddChild(component);
        component.IsChecked = true;

        component.Checked += c =>
        {
            if (c)
                return;

            SkinSelectorPopup.EnableSkinComponent(skin);

            SkinsToModify.Remove(skin);
            component.QueueFree();

            if (SkinsToModify.Count == 0)
                ContinueButton.Disabled = true;
        };

        ContinueButton.Disabled = false;
    }

    private void OnContinueButtonPressed()
    {
        var instance = SkinModifierModificationSelectScene.Instantiate<SkinModifierModificationSelect>();
        instance.SkinsToModify = SkinsToModify;
        EmitSignal(SignalName.ScenePushed, instance);
    }
}
