using Godot;
using OsuSkinMixer.Components;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.StackScenes;

public partial class SkinManager : StackScene
{
    public override string Title => "Skin Manager";

    private PackedScene SkinInfoScene;
	private PackedScene SkinComponentScene;

	private LineEdit SearchLineEdit;
	private Button SelectAllButton;
	private VBoxContainer SkinComponentContainer;

    public override void _Ready()
    {
        SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");
		SkinComponentScene = GD.Load<PackedScene>("res://src/Components/SkinComponentSkinManager.tscn");

		SearchLineEdit = GetNode<LineEdit>("%SearchLineEdit");
		SelectAllButton = GetNode<Button>("%SelectAllButton");
		SkinComponentContainer = GetNode<VBoxContainer>("%SkinComponentContainer");
    }
}
