using Godot;
using System;

namespace OsuSkinMixer.Components;

public partial class CursorColourIcon : CenterContainer
{
	public Action Pressed;

	private TextureRect CursorTexture;
	private TextureRect CursorMiddleTexture;
	private Button Button;

	public override void _Ready()
	{
		CursorTexture = GetNode<TextureRect>("CursorTexture");
		CursorMiddleTexture = GetNode<TextureRect>("CursorMiddleTexture");
		Button = GetNode<Button>("Button");

		Button.Pressed += OnButtonPressed;
	}

	public void SetValues(Texture2D cursorTexture, Texture2D cursorMiddleTexture)
	{
		CursorTexture.Texture = cursorTexture;
		CursorMiddleTexture.Texture = cursorMiddleTexture;
	}

	private void OnButtonPressed()
	{
		Pressed?.Invoke();
	}
}
