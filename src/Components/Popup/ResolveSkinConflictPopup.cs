using System.IO;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Components;

public partial class ResolveSkinConflictPopup : Popup
{
    protected override bool IsImportant => true;

    private SkinComponent VisibleSkinComponent;
    private SkinComponent HiddenSkinComponent;
    private Button DoneButton;
    private OkPopup ResolveFailedPopup;

    public ResolveSkinConflictPopup()
    {
        OsuData.SkinConflictDetected += (v, h) =>
        {
            if (VisibleSkinComponent.Skin != null && HiddenSkinComponent.Skin != null)
                return;

            In();
            VisibleSkinComponent.Skin = v;
            HiddenSkinComponent.Skin = h;
            VisibleSkinComponent.SetValues();
            HiddenSkinComponent.SetValues();
        };
    }

    public override void _Ready()
    {
        base._Ready();

        VisibleSkinComponent = GetNode<SkinComponent>("%VisibleSkinComponent");
        HiddenSkinComponent = GetNode<SkinComponent>("%HiddenSkinComponent");
        DoneButton = GetNode<Button>("%DoneButton");
        ResolveFailedPopup = GetNode<OkPopup>("%ResolveFailedPopup");

        VisibleSkinComponent.LeftClicked += () => Tools.ShellOpenFile(VisibleSkinComponent.Skin.Directory.FullName);
        HiddenSkinComponent.LeftClicked += () => Tools.ShellOpenFile(HiddenSkinComponent.Skin.Directory.FullName);
        DoneButton.Pressed += OnDoneButtonPressed;
    }

    private void OnDoneButtonPressed()
    {
        if (Directory.Exists(VisibleSkinComponent.Skin.Directory.FullName) && Directory.Exists(HiddenSkinComponent.Skin.Directory.FullName))
        {
            ResolveFailedPopup.In();
            return;
        }

        VisibleSkinComponent.Skin = null;
        HiddenSkinComponent.Skin = null;
        OsuData.SweepPaused = false;
        Out();
    }
}
