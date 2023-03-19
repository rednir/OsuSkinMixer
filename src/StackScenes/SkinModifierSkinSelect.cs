using System.Collections.Generic;
using System.Linq;
using Godot;
using OsuSkinMixer.Components;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.StackScenes;

public partial class SkinModifierSkinSelect : StackScene
{
    public override string Title => "Skin Modifier";

    public List<SkinComponent> SkinsToModifyComponents { get; set; } = new();

    private PackedScene SkinModifierModificationSelectScene;
    private PackedScene SkinComponentScene;

    private VBoxContainer SkinsToModifyContainer;
    private Button AddSkinToModifyButton;
    private CheckButton MakeCopyButton;
    private Button ContinueButton;
    private ManageSkinPopup ManageSkinPopup;
    private SkinSelectorPopup SkinSelectorPopup;

    public override void _Ready()
    {
        SkinModifierModificationSelectScene = GD.Load<PackedScene>("res://src/StackScenes/SkinModifierModificationSelect.tscn");
        SkinComponentScene = GD.Load<PackedScene>("res://src/Components/SkinComponentSkinManager.tscn");

        SkinsToModifyContainer = GetNode<VBoxContainer>("%SkinsToModifyContainer");
        AddSkinToModifyButton = GetNode<Button>("%AddSkinToModifyButton");
        MakeCopyButton = GetNode<CheckButton>("%MakeCopyButton");
        ContinueButton = GetNode<Button>("%ContinueButton");
        SkinSelectorPopup = GetNode<SkinSelectorPopup>("%SkinSelectorPopup");
        ManageSkinPopup = GetNode<ManageSkinPopup>("%ManageSkinPopup");

        ContinueButton.Pressed += OnContinueButtonPressed;
        AddSkinToModifyButton.Pressed += AddSkinToModifyButtonPressed;
        ManageSkinPopup.SkinInfoRequested = OnSkinInfoRequest;
        ManageSkinPopup.Options = ManageSkinOptions.All & ~ManageSkinOptions.Modify & ~ManageSkinOptions.Duplicate & ~ManageSkinOptions.Delete;
        SkinSelectorPopup.OnSelected = OnSkinSelected;

        OsuData.SkinRemoved += OnSkinRemoved;
    }

    public override void _ExitTree()
    {
        OsuData.SkinRemoved -= OnSkinRemoved;
    }

    private void OnSkinRemoved(OsuSkin skin)
    {
        var component = SkinsToModifyComponents.Find(c => c.Skin.Equals(skin));

        if (component == null)
            return;

        component.Checked(false);
    }

    private void AddSkinToModifyButtonPressed()
    {
        SkinSelectorPopup.In();
    }

    private void OnSkinSelected(OsuSkin skin)
    {
        SkinSelectorPopup.Out();
        AddSkinComponent(skin);
    }

    private void AddSkinComponent(OsuSkin skin)
    {
        // Don't show already selected skins in the selector.
        SkinSelectorPopup.DisableSkinComponent(skin);

        // TODO: use SkinComponentsContainer.
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

            SkinsToModifyComponents.Remove(component);
            component.QueueFree();

            if (SkinsToModifyComponents.Count == 0)
                ContinueButton.Disabled = true;
        };

        component.RightClicked += () =>
        {
            ManageSkinPopup.SetSkins(new OsuSkin[] { skin });
            ManageSkinPopup.In();
        };

        SkinsToModifyComponents.Add(component);

        ContinueButton.Disabled = false;
    }

    private void OnContinueButtonPressed()
    {
        if (!MakeCopyButton.ButtonPressed)
        {
            PushNextScene();
            return;
        }

        ManageSkinPopup.SetSkins(SkinsToModifyComponents.Select(c => c.Skin));
        ManageSkinPopup.OnDuplicateButtonPressed();
    }

    private void OnSkinInfoRequest(IEnumerable<OsuSkin> skins)
    {
        foreach (var component in SkinsToModifyContainer.GetChildren().Cast<SkinComponent>())
            component.Checked(false);

        foreach (var skin in skins)
            AddSkinComponent(skin);

        MakeCopyButton.ButtonPressed = false;
        PushNextScene();
    }

    private void PushNextScene()
    {
        var instance = SkinModifierModificationSelectScene.Instantiate<SkinModifierModificationSelect>();
        instance.SkinsToModify = SkinsToModifyComponents.ConvertAll(c => c.Skin);
        EmitSignal(SignalName.ScenePushed, instance);
    }
}
