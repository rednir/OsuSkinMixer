using System.Collections.Generic;
using Godot;
using OsuSkinMixer.Components;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.StackScenes;

public partial class SkinInfo : StackScene
{
    public override string Title => "Skin info";

    public IEnumerable<OsuSkin> Skins { get; set; }

	private PackedScene SkinModiferModificationSelectScene;
    private PackedScene SkinInfoPanelScene;

    public override void _Ready()
    {
		SkinModiferModificationSelectScene = GD.Load<PackedScene>("res://src/StackScenes/SkinModifierModificationSelect.tscn");
        SkinInfoPanelScene = GD.Load<PackedScene>("res://src/Components/SkinInfoPanel.tscn");

        foreach (var skin in Skins)
        {
            var instance = SkinInfoPanelScene.Instantiate<SkinInfoPanel>();
            instance.Skin = skin;
            instance.ModifySkin = () => PushModifierScene(skin);
            AddChild(instance);
        }
    }

    private void PushModifierScene(OsuSkin skin)
    {
        var scene = SkinModiferModificationSelectScene.Instantiate<SkinModifierModificationSelect>();
        scene.SkinsToModify = new List<OsuSkin> { skin };
        EmitSignal(SignalName.ScenePushed, scene);
    }
}
