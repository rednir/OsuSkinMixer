using Godot;
using System;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Components;

public partial class SkinPreview : PanelContainer
{
	private AnimationPlayer AnimationPlayer;
	private Sprite2D Cursor;
	private CpuParticles2D Cursortrail;
	private Hitcircle Hitcircle;

	private bool _paused;

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
		Cursor = GetNode<Sprite2D>("%Cursor");
		Cursortrail = GetNode<CpuParticles2D>("%Cursortrail");
		Hitcircle = GetNode<Hitcircle>("%Hitcircle");
	}

	public override void _Process(double delta)
	{
		Vector2 mousePosition = GetGlobalMousePosition();

		Cursor.GlobalPosition = mousePosition;

		// The MouseEntered and MouseExited control node don't work as the hitcircle handles mouse input.
		if (GetGlobalRect().HasPoint(mousePosition))
			OnMouseEntered();
		else
			OnMouseExited();
	}

	public void SetSkin(OsuSkin skin)
	{
		Cursor.Texture = skin.GetTexture("cursor.png");
		Cursortrail.Texture = skin.GetTexture("cursortrail.png");

		Hitcircle.SetSkin(skin, 1);
	}

	private void OnMouseEntered()
	{
		if (!_paused)
			return;

		_paused = false;
		AnimationPlayer.Play("enter");
		Hitcircle.Resume();
	}

	private void OnMouseExited()
	{
		if (_paused)
			return;

		_paused = true;
		AnimationPlayer.Play("exit");
		Hitcircle.Pause();
	}
}
