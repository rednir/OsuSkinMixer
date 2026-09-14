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
    private static int activeCount;
    public static bool IsBusy => Volatile.Read(ref activeCount) > 0;

    private static void AddOperationToMemory(Operation operation)
    {
        if (Settings.Content.Operations.Count > MAX_OPERATION_COUNT)
            Settings.Content.Operations.RemoveRange(0, Settings.Content.Operations.Count - MAX_OPERATION_COUNT);

        foreach (var op in Settings.Content.Operations)
        {
            // Only allow the latest operation done to a skin to be undone.
            // e.g. if you create a skin mix, then delete it, you can't undo the creation as that is not relvant anymore.
            if (op.TargetSkin != null && op.TargetSkin.Equals(operation.TargetSkin))
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
    public OsuSkin TargetSkin { get; private set; }

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

    public void SetTarget(OsuSkin skin) { TargetSkin = skin; TargetSkinName = skin.Name; }
    public void SetUndo(Action undo) => UndoAction = undo;

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
        if (_task != null) return _task;
        Interlocked.Increment(ref activeCount);
        TimeStarted = DateTime.Now;
        _task = Task.Run(() =>
        {
            GodotThread.SetThreadSafetyChecksEnabled(false);
            if (pauseSweep) OsuData.SweepPaused = true;
            try
            {
                lock (_lock)
                {
                    Action();
                    AddOperationToMemory(this);
                }
            }
            catch { UndoAction = null; throw; }
            finally
            {
                if (pauseSweep) OsuData.SweepPaused = false;
                OsuData.RequestRefresh();
                Interlocked.Decrement(ref activeCount);
            }
        });
        return _task;
    }

    public async void UndoOperation()
    {
        Interlocked.Increment(ref activeCount);
        try
        {
            await Task.Run(() =>
            {
                GodotThread.SetThreadSafetyChecksEnabled(false);
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
            });
        }
        finally { Interlocked.Decrement(ref activeCount); OsuData.RequestRefresh(); }
    }
}
