namespace OsuSkinMixer.StackScenes;

using OsuSkinMixer.Components;
using OsuSkinMixer.Models;
using OsuSkinMixer.Models.Osu;
using OsuSkinMixer.Statics;

public partial class SkinModifierSkinSelect : StackScene
{
    public override string Title => "Skin modifier";

    public List<SkinComponent> SkinsToModifyComponents { get; set; } = new();

    private PackedScene SkinModifierModificationSelectScene;
    private PackedScene SkinComponentScene;

    private VBoxContainer SkinsToModifyContainer;
    private Button AddSkinToModifyButton;
    private CheckBox MakeCopyCheckBox;
    private Button ContinueButton;
    private ManageSkinPopup ManageSkinPopup;
    private SkinSelectorPopup SkinSelectorPopup;

    public override void _Ready()
    {
        SkinModifierModificationSelectScene = GD.Load<PackedScene>("res://src/StackScenes/SkinModifierModificationSelect.tscn");
        SkinComponentScene = GD.Load<PackedScene>("res://src/Components/SkinComponentSkinManager.tscn");

        SkinsToModifyContainer = GetNode<VBoxContainer>("%SkinsToModifyContainer");
        AddSkinToModifyButton = GetNode<Button>("%AddSkinToModifyButton");
        MakeCopyCheckBox = GetNode<CheckBox>("%MakeCopyCheckBox");
        ContinueButton = GetNode<Button>("%ContinueButton");
        SkinSelectorPopup = GetNode<SkinSelectorPopup>("%SkinSelectorPopup");
        ManageSkinPopup = GetNode<ManageSkinPopup>("%ManageSkinPopup");

        ContinueButton.Pressed += OnContinueButtonPressed;
        AddSkinToModifyButton.Pressed += AddSkinToModifyButtonPressed;
        ManageSkinPopup.SkinInfoRequested = OnSkinInfoRequest;
        ManageSkinPopup.Options = ManageSkinOptions.All & ~ManageSkinOptions.Modify & ~ManageSkinOptions.Duplicate & ~ManageSkinOptions.Delete;
        SkinSelectorPopup.OnSelected = OnSkinSelected;

        OsuData.SkinRemoved += OnSkinRemoved;

        SkinSelectorPopup.In();
    }

    public override void _ExitTree()
    {
        OsuData.SkinRemoved -= OnSkinRemoved;
    }

    private void OnSkinRemoved(OsuSkinBase skin)
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

    private void OnSkinSelected(OsuSkinBase skin)
    {
        SkinSelectorPopup.Out();
        AddSkinComponent(skin);
    }

    private void AddSkinComponent(OsuSkinBase skin)
    {
        // Don't show already selected skins in the selector.
        SkinSelectorPopup.DisableSkinComponent(skin);

        // TODO: use SkinComponentsContainer.
        var component = SkinComponentScene.Instantiate<SkinComponent>();
        component.Skin = skin;
        component.Visible = true;
        component.CheckBoxVisible = true;
        SkinsToModifyContainer.CallDeferred(MethodName.AddChild, component);
        component.Ready += () => component.IsChecked = true;

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
            ManageSkinPopup.SetSkins(new OsuSkinBase[] { skin });
            ManageSkinPopup.In();
        };

        SkinsToModifyComponents.Add(component);

        ContinueButton.Disabled = false;
    }

    private void OnContinueButtonPressed()
    {
        if (!MakeCopyCheckBox.ButtonPressed)
        {
            PushNextScene();
            return;
        }

        ManageSkinPopup.SetSkins(SkinsToModifyComponents.Select(c => c.Skin));
        ManageSkinPopup.OnDuplicateButtonPressed();
    }

    private void OnSkinInfoRequest(IEnumerable<OsuSkinBase> skins)
    {
        foreach (var component in SkinsToModifyContainer.GetChildren().Cast<SkinComponent>())
            component.Checked(false);

        foreach (var skin in skins)
            AddSkinComponent(skin);

        MakeCopyCheckBox.ButtonPressed = false;
        PushNextScene();
    }

    private void PushNextScene()
    {
        var instance = SkinModifierModificationSelectScene.Instantiate<SkinModifierModificationSelect>();
        instance.SkinsToModify = SkinsToModifyComponents.ConvertAll(c => c.Skin);
        EmitSignal(SignalName.ScenePushed, instance);
    }
}
