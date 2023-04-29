namespace OsuSkinMixer.Components;

using System.IO;
using OsuSkinMixer.Statics;

public partial class SkinNamePopup : Popup
{
    public Action<string> ConfirmAction { get; set; }

    public bool SuffixMode { get; set; }

    public string[] SkinNames { get; set; }

    public string LineEditText
    {
        get => LineEdit.Text;
        set => LineEdit.Text = value;
    }

    private Label TitleLabel;
    private Label WarningLabel;
    private LineEdit LineEdit;
    private Button ConfirmButton;

    public override void _Ready()
    {
        base._Ready();

        TitleLabel = GetNode<Label>("%TitleLabel");
        WarningLabel = GetNode<Label>("%WarningLabel");
        LineEdit = GetNode<LineEdit>("%LineEdit");
        ConfirmButton = GetNode<Button>("%ConfirmButton");

        ConfirmButton.Pressed += OnConfirm;
        LineEdit.TextSubmitted += t =>
        {
            if (ConfirmButton.Disabled)
                return;

            Out();
            ConfirmAction?.Invoke(t);
        };
        LineEdit.TextChanged += OnTextChanged;
    }

    public override void In()
    {
        base.In();
        TitleLabel.Text = SuffixMode ? "Choose a suffix for the new skins" : "Name your new skin";
        LineEdit.GrabFocus();
        LineEdit.SelectAll();
        OnTextChanged(LineEdit.Text);
    }

    private void OnConfirm()
    {
        if (string.IsNullOrWhiteSpace(LineEdit.Text))
            return;

        Out();
        ConfirmAction?.Invoke(LineEdit.Text);
    }

    private void OnTextChanged(string text)
    {
        if (text.Any(c => Path.GetInvalidFileNameChars().Contains(c)) || text == "." || text == "..")
        {
            ConfirmButton.Disabled = true;
            WarningLabel.Text = "Invalid characters in skin name.";
        }
        else if (string.IsNullOrWhiteSpace(text))
        {
            ConfirmButton.Disabled = true;
            WarningLabel.Text = !SuffixMode ? "Skin name cannot be empty." : "Skin name suffix cannot be empty.";
        }
        else if (!SuffixMode && SkinNames != null && SkinNames.FirstOrDefault() == text)
        {
            ConfirmButton.Disabled = true;
            WarningLabel.Text = "New skin name cannot be the same as the original skin.";
        }
        else if (!SuffixMode && OsuData.Skins.Any(s => s.Name == text))
        {
            ConfirmButton.Disabled = false;
            WarningLabel.Text = "Skin with this name already exists and will be replaced.";
        }
        else if (CheckForSuffixConflicts(text))
        {
            ConfirmButton.Disabled = false;
            WarningLabel.Text = "Some skins will be replaced due to conflicting skin names.";
        }
        else
        {
            ConfirmButton.Disabled = false;
            WarningLabel.Text = string.Empty;
        }
    }

    private bool CheckForSuffixConflicts(string suffix)
    {
        if (SkinNames == null)
            return false;

        foreach (string skinName in OsuData.Skins.Select(s => s.Name))
        {
            if (SkinNames.Any(s => s + suffix == skinName))
                return true;
        }

        return false;
    }
}
