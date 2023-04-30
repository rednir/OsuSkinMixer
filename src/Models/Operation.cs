namespace OsuSkinMixer.Models;

using System.Text.Json.Serialization;
using OsuSkinMixer.Statics;

/// <summary>
/// A class that represents an operation to be peformed to a single skin.
/// </summary>
public class Operation
{
    private const int MAX_OPERATION_COUNT = 100;

    private static readonly object _lock = new();

    private static void AddOperationToMemory(Operation operation)
    {
        if (Settings.Content.Operations.Count > MAX_OPERATION_COUNT)
            Settings.Content.Operations.RemoveRange(0, Settings.Content.Operations.Count - MAX_OPERATION_COUNT);

        foreach (var op in Settings.Content.Operations)
        {
            // Only allow the latest operation done to a skin to be undone.
            // e.g. if you create a skin mix, then delete it, you can't undo the creation as that is not relvant anymore.
            if (op.TargetSkinName == operation.TargetSkinName)
                op.UndoAction = null;
        }

        Settings.Content.Operations.Add(operation);
    }

    [JsonPropertyName("type")]
    public OperationType Type { get; set; }

    [JsonPropertyName("target_skin_name")]
    public string TargetSkinName { get; set; }

    [JsonPropertyName("time_started")]
    public DateTime? TimeStarted { get; set; }

    [JsonIgnore]
    public OsuSkin TargetSkin { get; }

    [JsonIgnore]
    public string Description => $"{Type} {TargetSkinName}";

    [JsonIgnore]
    public bool Started => _task != null;

    [JsonIgnore]
    public bool CanUndo => UndoAction != null;

    [JsonIgnore]
    private Action UndoAction { get; set; }

    [JsonIgnore]
    private Action Action { get; }

    [JsonIgnore]
    private Task _task;

    public Operation()
    {
    }

    public Operation(OperationType type, OsuSkin targetSkin, Action action, Action undoAction = null)
    {
        Type = type;
        TargetSkin = targetSkin;
        TargetSkinName = targetSkin.Name;
        Action = action;
        UndoAction = undoAction;
    }

    public Task RunOperation(bool pauseSweep = true)
    {
        if (_task != null)
            return _task;

        if (pauseSweep)
            OsuData.SweepPaused = true;

        TimeStarted = DateTime.Now;

        Settings.Log($"Running operation: {Description}");
        AddOperationToMemory(this);

        _task = Task.Run(() =>
        {
            lock (_lock)
            {
                Action();
            }
        })
        .ContinueWith(t =>
        {
            if (pauseSweep)
                OsuData.SweepPaused = false;

            if (t.IsFaulted)
            {
                if (t.Exception.InnerException is OperationCanceledException)
                {
                    Settings.Log($"Operation canceled: {Description}");
                    return;
                }

                Settings.PushException(t.Exception);
                return;
            }

            Settings.Log($"Operation completed: {Description}");
        });

        return _task;
    }

    public void UndoOperation()
    {
        lock (_lock)
        {
            if (_task?.IsCompleted != true || !CanUndo)
                return;

            Settings.Log($"Undoing operation: {Description}");

            try
            {
                UndoAction();
                UndoAction = null;
                Settings.Content.Operations.Remove(this);
            }
            catch (Exception e)
            {
                Settings.PushException(e);
            }
        }
    }
}