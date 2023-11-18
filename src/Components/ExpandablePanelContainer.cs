namespace OsuSkinMixer.Components;

public partial class ExpandablePanelContainer : PanelContainer
{
	private Button ExpandButton;
	private VBoxContainer ContentContainer;
	
	private Texture2D MoreIconTexture;
	private Texture2D LessIconTexture;

	public override void _Ready()
	{
		ExpandButton = GetNode<Button>("%ExpandButton");
		ContentContainer = GetNode<VBoxContainer>("%ContentContainer");

		MoreIconTexture = GD.Load<Texture2D>("res://assets/materialicons/expand_more.png");
		LessIconTexture = GD.Load<Texture2D>("res://assets/materialicons/expand_less.png");

		ExpandButton.Pressed += ExpandButtonPressed;
	}

	private void ExpandButtonPressed()
	{
		ContentContainer.Visible = !ContentContainer.Visible;
		ExpandButton.Icon = ContentContainer.Visible ? LessIconTexture : MoreIconTexture;
	}
}
