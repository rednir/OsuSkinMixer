using Godot;
using System;

namespace OsuSkinMixer
{
    public class TopBar : Panel
    {
        public Main Main { get; set; }

        private PopupMenu SkinPopup;
        private PopupMenu OptionsPopup;
        private PopupMenu HelpPopup;

        public override void _Ready()
        {
            SkinPopup = GetNode<MenuButton>("HBoxContainer/SkinButton").GetPopup();
            SkinPopup.Connect("id_pressed", this, nameof(_SkinButtonPressed));

            OptionsPopup = GetNode<MenuButton>("HBoxContainer/OptionsButton").GetPopup();
            OptionsPopup.Connect("id_pressed", this, nameof(_OptionsButtonPressed));
            OptionsPopup.SetItemChecked(1, Settings.Content.LogToFile);

            HelpPopup = GetNode<MenuButton>("HBoxContainer/HelpButton").GetPopup();
            HelpPopup.Connect("id_pressed", this, nameof(_HelpButtonPressed));
        }

        public void _SkinButtonPressed(int id)
        {
            switch (id)
            {
                case 0:
                    Main.CreateSkin();
                    break;

                case 1:
                    Main.RefreshSkins();
                    break;

                case 3:
                    Main.UseExistingSkin();
                    break;

                case 4:
                    Main.RandomizeOptions(false);
                    break;

                case 5:
                    Main.RandomizeOptions(true);
                    break;

                case 6:
                    Main.ResetSelections();
                    break;
            }
        }

        public void _OptionsButtonPressed(int id)
        {
            switch (id)
            {
                case 0:
                    Main.PromptForSkinsFolder();
                    break;

                case 1:
                    Settings.Content.LogToFile = !Settings.Content.LogToFile;
                    Settings.Save();
                    OptionsPopup.SetItemChecked(1, Settings.Content.LogToFile);
                    break;
            }
        }

        public void _HelpButtonPressed(int id)
        {
            switch (id)
            {
                case 0:
                case 1:
                    OS.ShellOpen("https://github.com/rednir/OsuSkinMixer/issues/new/choose");
                    break;
            }
        }
    }
}
