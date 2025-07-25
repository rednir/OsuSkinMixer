namespace OsuSkinMixer.Components;

public partial class ExpandablePanelContainer : PanelContainer
{
	private AnimationPlayer AnimationPlayer;
	private CpuParticles2D CpuParticles2D;
	private Button ExpandButton;
	private VBoxContainer ContentContainer;
	private Texture2D MoreIconTexture;
	private Texture2D LessIconTexture;

	private bool Active;

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		CpuParticles2D = GetNode<CpuParticles2D>("%CPUParticles2D");
		ExpandButton = GetNode<Button>("%ExpandButton");
		ContentContainer = GetNode<VBoxContainer>("%ContentContainer");
		MoreIconTexture = GD.Load<Texture2D>("res://assets/materialicons/expand_more.png");
		LessIconTexture = GD.Load<Texture2D>("res://assets/materialicons/expand_less.png");
		ExpandButton.Icon = ContentContainer.Visible ? LessIconTexture : MoreIconTexture;

		ExpandButton.Pressed += ExpandButtonPressed;

		VisibilityChanged += () => CallDeferred(MethodName.OnVisiblilityChanged);
	}

	public void Activate()
	{
		if (Active)
			return;

		Active = true;
		AnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Play, "activated");
	}

	public void Deactivate()
	{
		if (!Active)
			return;

		Active = false;
		AnimationPlayer.CallDeferred(AnimationPlayer.MethodName.Play, "deactivated");
	}

	private void OnVisiblilityChanged()  
	{
		if (Active)
		{
			if (IsVisibleInTree())
			{
				CpuParticles2D.Emitting = true;
			}
			else
			{
				CpuParticles2D.Emitting = false;
			}
		}
	}

	private void ExpandButtonPressed()
	{
		ContentContainer.Visible = !ContentContainer.Visible;
		ExpandButton.Icon = ContentContainer.Visible ? LessIconTexture : MoreIconTexture;
	}
}
