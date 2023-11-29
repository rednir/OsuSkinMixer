namespace OsuSkinMixer;

using OsuSkinMixer.Statics;

public partial class Background : CanvasLayer
{
	public AnimationPlayer AnimationPlayer;

	private bool _snowingEnabled = DateTime.Now.Month == 12 && DateTime.Now.Day > 7 && DateTime.Now.Day < 31;

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

		if (_snowingEnabled)
			AnimationPlayer.Play("snow_in");

		if (Settings.Content.DisableEffects)
			AnimationPlayer.Play("disable");
	}

	public void HideSnow()
	{
		if (!_snowingEnabled)
			return;

		if (AnimationPlayer.AssignedAnimation == "snow_in")
			AnimationPlayer.Play("snow_out");
	}

	public void ShowSnow()
	{
		if (!_snowingEnabled)
			return;

		AnimationPlayer.Play("snow_in");
	}
}
