namespace OsuSkinMixer;

using OsuSkinMixer.Statics;

public partial class Background : CanvasLayer
{
	public AnimationPlayer AnimationPlayer;

	private bool _snowing = DateTime.Now.Month == 12 && DateTime.Now.Day > 10 && DateTime.Now.Day < 31;

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

		if (_snowing)
			AnimationPlayer.Play("snow_in");

		if (Settings.Content.DisableEffects)
			AnimationPlayer.Play("disable");
	}

	public void HideSnow()
	{
		if (!_snowing)
			return;

		AnimationPlayer.Play("snow_out");
	}

	public void ShowSnow()
	{
		if (!_snowing)
			return;

		AnimationPlayer.Play("snow_in");
	}

	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusIn)
		{
			GetTree().Paused = false;
		}
		else if (what == NotificationApplicationFocusOut)
		{
			GetTree().Paused = true;
		}
	}
}
