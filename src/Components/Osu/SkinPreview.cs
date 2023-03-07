using Godot;
using System;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Components;

public partial class SkinPreview : PanelContainer
{
	private AnimationPlayer AnimationPlayer;
	private TextureRect MenuBackground;
	private Sprite2D Cursor;
	private Sprite2D Cursormiddle;
	private CpuParticles2D Cursortrail;
	private Hitcircle Hitcircle;

	private bool _paused;

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
		MenuBackground = GetNode<TextureRect>("%MenuBackground");
		Cursor = GetNode<Sprite2D>("%Cursor");
		Cursormiddle = GetNode<Sprite2D>("%Cursormiddle");
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
		MenuBackground.Texture = skin.GetTexture("menu-background.jpg") ?? skin.GetTexture("menu-background.png");
		Cursor.Texture = skin.GetTexture("cursor.png");
		Cursormiddle.Texture = skin.GetTexture("cursormiddle.png");
		Cursortrail.Texture = skin.GetTexture("cursortrail.png");

		Hitcircle.SetSkin(skin);
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
