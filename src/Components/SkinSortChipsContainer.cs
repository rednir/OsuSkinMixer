namespace OsuSkinMixer;

using OsuSkinMixer.Models;

public partial class SkinSortChipsContainer : HBoxContainer
{
	public event Action<SkinSort> SortSelected;

	private Button[] _sortButtons;

	public override void _Ready()
	{
		_sortButtons = GetChildren().Cast<Button>().ToArray();

        for (int i = 0; i < _sortButtons.Length; i++)
		{
			int index = i;
            _sortButtons[i].Pressed += () => OnSortButtonPressed(index);
		}
    }

	public void SetSortVisible(SkinSort sort, bool visible)
	{
		Button button = _sortButtons[(int)sort];
		button.Visible = visible;

		if (!visible && button.Disabled)
			OnSortButtonPressed((int)SkinSort.Name);
	}

	private void OnSortButtonPressed(int index)
	{
		for (int i = 0; i < _sortButtons.Length; i++)
		{
			if (i == index)
			{
				_sortButtons[i].Disabled = true;
				SortSelected?.Invoke((SkinSort)index);
				continue;
			}

			_sortButtons[i].Disabled = false;
			_sortButtons[i].ButtonPressed = false;
		}
	}
}
