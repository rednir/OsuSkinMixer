using Godot;
using OsuSkinMixer.Components;

namespace OsuSkinMixer.StackScenes;

public partial class Menu : StackScene
{
	public override string Title => "Menu";

	private PackedScene SkinMixerScene;
	private PackedScene SkinModifierSkinSelectScene;
	private PackedScene SkinManagerScene;

	private Button SkinMixerButton;
	private Button SkinModifierButton;
	private Button SkinManagerButton;
	private Button GetMoreSkinsButton;
	private GetMoreSkinsPopup GetMoreSkinsPopup;

	public override void _Ready()
	{
		SkinMixerScene = GD.Load<PackedScene>("res://src/StackScenes/SkinMixer.tscn");
		SkinModifierSkinSelectScene = GD.Load<PackedScene>("res://src/StackScenes/SkinModifierSkinSelect.tscn");
		SkinManagerScene = GD.Load<PackedScene>("res://src/StackScenes/SkinManager.tscn");

		SkinMixerButton = GetNode<Button>("%SkinMixerButton");
		SkinModifierButton = GetNode<Button>("%SkinModifierButton");
		SkinManagerButton = GetNode<Button>("%SkinManagerButton");
		GetMoreSkinsButton = GetNode<Button>("%GetMoreSkinsButton");
		GetMoreSkinsPopup = GetNode<GetMoreSkinsPopup>("%GetMoreSkinsPopup");

		SkinMixerButton.Pressed += () => EmitSignal(SignalName.ScenePushed, SkinMixerScene.Instantiate<StackScene>());
		SkinModifierButton.Pressed += () => EmitSignal(SignalName.ScenePushed, SkinModifierSkinSelectScene.Instantiate<StackScene>());
		SkinManagerButton.Pressed += () => EmitSignal(SignalName.ScenePushed, SkinManagerScene.Instantiate<StackScene>());
		GetMoreSkinsButton.Pressed += GetMoreSkinsPopup.In;
	}
}
