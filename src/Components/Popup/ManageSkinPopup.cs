namespace OsuSkinMixer.Components;

using System.IO;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using OsuSkinMixer.Storage;

public partial class ManageSkinPopup : Popup
{
    public Action<IEnumerable<OsuSkin>> SkinInfoRequested { get; set; } = OsuData.RequestSkinInfo;
    public ManageSkinOptions Options { get; set; } = ManageSkinOptions.All;
    public bool OverwriteDuplicates { get; set; }
    private OsuSkin[] skins = [];
    private QuestionPopup deleteQuestion;
    private SkinNamePopup namePopup;
    private LoadingPopup loading;
    private bool renaming;
    public override void _Ready()
    {
        base._Ready();
        deleteQuestion = GetNode<QuestionPopup>("%DeleteQuestionPopup");
        namePopup = GetNode<SkinNamePopup>("%SkinNamePopup");
        loading = GetNode<LoadingPopup>("%LoadingPopup");
        Button("OpenInOsu").Pressed += OnOpenInOsuButtonPressed;
        Button("OpenFolder").Pressed += OnOpenFolderButtonPressed;
        Button("Modify").Pressed += OnModifyButtonPressed;
        Button("Hide").Pressed += OnHideButtonPressed;
        Button("Export").Pressed += OnExportButtonPressed;
        Button("Rename").Pressed += OnRenameButtonPressed;
        Button("Duplicate").Pressed += OnDuplicateButtonPressed;
        Button("Delete").Pressed += OnDeleteButtonPressed;
        deleteQuestion.ConfirmAction = () => RunBatch(OperationType.Delete, skin =>
        {
            var snapshot = skin.DeleteFromDisk();
            OsuData.RemoveSkin(skin);
            return () => { skin.Restore(snapshot); OsuData.AddSkin(skin); };
        });
        namePopup.ConfirmAction = NameConfirmed;
    }
    private Button Button(string name) => GetNode<Button>("%" + name + "Button");
    public void SetSkin(OsuSkin skin) => skins = [skin];
    public void SetSkins(IEnumerable<OsuSkin> selected) => skins = selected.ToArray();
    public override void In()
    {
        if (skins.Length == 0) return;
        foreach (var name in new[] { "OpenInOsu", "OpenFolder", "Modify", "Hide", "Export", "Rename", "Duplicate", "Delete" })
        {
            var option = Enum.Parse<ManageSkinOptions>(name);
            Button(name).Visible = Options.HasFlag(option);
            Button(name).Disabled = false;
            Button(name).TooltipText = "";
        }
        Button("OpenInOsu").Visible &= skins.Length == 1;
        Button("OpenFolder").Visible &= skins.Length <= 4;
        var lazer = skins.Any(s => s.IsLazer);
        foreach (var name in new[] { "OpenFolder", "Hide" })
            Button(name).Visible &= !lazer;
        foreach (var name in new[] { "Modify", "Rename", "Duplicate", "Delete" })
        {
            Button(name).Disabled = skins.Any(s => !s.CanEdit);
            Button(name).TooltipText = skins.FirstOrDefault(s => !s.CanEdit)?.Record?.Problem ?? "Only legacy user skins can be changed.";
        }
        Button("Export").Disabled = Button("OpenInOsu").Disabled = skins.Any(s => !s.CanExport);
        Button("Rename").Visible &= skins.Length == 1;
        Button("Hide").Text = skins.Length == 1 && skins[0].Hidden ? "    Unhide from osu!" : "    Hide from osu!";
        GetNode<Label>("%Title").Text = skins.Length == 1 ? skins[0].Name : $"{skins.Length} skins selected";
        base.In();
    }
    public void OnOpenInOsuButtonPressed()
    {
        _ = Run(async () => await Task.Run(() => Tools.TriggerOskImport(skins[0])));
    }
    public void OnOpenFolderButtonPressed()
    {
        if (skins.Any(s => s.IsLazer)) return;
        foreach (var skin in skins) Tools.ShellOpenFile(skin.Directory.FullName);
        Out();
    }
    public void OnModifyButtonPressed() { OsuData.RequestSkinModify(skins); Out(); }
    public void OnHideButtonPressed() => RunBatch(OperationType.Hide, skin =>
    {
        bool hidden = skin.Hidden;
        skin.SetHidden(!hidden); OsuData.InvokeSkinModified(skin);
        return () => { skin.SetHidden(hidden); OsuData.InvokeSkinModified(skin); };
    });
    public void OnExportButtonPressed() => RunBatch(OperationType.Export, skin =>
    {
        var path = LibraryActions.ExportPath(skin.Name);
        skin.Export(path);
        Tools.ShellOpenFile(Path.GetDirectoryName(path));
        return null; // Retained exports are user files, not undoable mutations of the library.
    });
    public void OnRenameButtonPressed() { renaming = true; ShowNamePopup(); }
    public void OnDuplicateButtonPressed() { renaming = false; ShowNamePopup(); }
    private void ShowNamePopup()
    {
        namePopup.RejectNameConflicts = renaming;
        namePopup.OverwriteNameConflicts = !renaming && (OverwriteDuplicates || skins.All(s => !s.IsLazer));
        namePopup.SuffixMode = skins.Length > 1;
        namePopup.SkinNames = skins.Select(s => s.Name).ToArray();
        namePopup.LineEditText = renaming ? skins[0].Name : skins.Length > 1 ? " (copy)" : skins[0].Name + " (copy)";
        namePopup.In();
    }
    private void NameConfirmed(string value)
    {
        var results = new List<OsuSkin>();
        RunBatch(renaming ? OperationType.Other : OperationType.Duplicate, skin =>
        {
            if (renaming)
            {
                var originalName = skin.Name;
                skin.Rename(value); OsuData.InvokeSkinModified(skin);
                results.Add(skin);
                return () => { skin.Rename(originalName); OsuData.InvokeSkinModified(skin); };
            }
            var installation = skin.DuplicateWithResult(namePopup.SuffixMode ? skin.Name + value : value, OverwriteDuplicates);
            var result = OsuData.ApplyInstall(installation);
            results.Add(result);
            return () => { skin.Library.UndoInstall(installation); OsuData.Refresh(); };
        }, () => SkinInfoRequested?.Invoke(results));
    }
    public void OnDeleteButtonPressed() => deleteQuestion.In();
    private void RunBatch(OperationType type, Func<OsuSkin, Action> action, Action completed = null)
    {
        _ = Run(async () =>
        {
            foreach (var skin in skins)
            {
                Operation operation = null;
                operation = new Operation(type, skin, () => operation.SetUndo(action(skin)));
                await operation.RunOperation();
                loading.Progress += 100.0 / skins.Length;
            }
            completed?.Invoke();
        });
    }
    private async Task Run(Func<Task> action)
    {
        loading.In();
        try { await action(); }
        catch (OperationCanceledException) { Settings.PushToast("Operation cancelled; no further skins were changed."); }
        catch (Exception e) { Settings.PushException(e); }
        finally { loading.Out(); namePopup.Out(); Out(); OsuData.RequestRefresh(); }
    }
}
