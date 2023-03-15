using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Models;

/// <summary>
/// A class that represents an operation to be peformed to a single skin.
/// </summary>
public class Operation
{
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
    public bool CanUndo => UndoAction != null && !_isUndone;

    [JsonIgnore]
    private Action UndoAction { get; }

    [JsonIgnore]
    private Action Action { get; }

    [JsonIgnore]
    private Task _task;

    [JsonIgnore]
    private bool _isUndone;

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

    public Task RunOperation()
    {
        if (_task != null)
            return _task;

        OsuData.SweepPaused = true;
        TimeStarted = DateTime.Now;

        Settings.Log($"Running operation: {Description}");
        Settings.Content.Operations.Add(this);

        _task = Task.Run(() => Action())
            .ContinueWith(t =>
            {
                OsuData.SweepPaused = false;

                if (t.IsFaulted)
                {
                    if (t.Exception.InnerException is OperationCanceledException)
                    {
                        Settings.Log($"Operation canceled: {Description}");
                        return;
                    }

                    GD.PrintErr(t.Exception);
                    OS.Alert($"{t.Exception.Message}\n\nPlease report this error with logs.", "Error");
                    return;
                }

                Settings.Log($"Operation completed: {Description}");
            });

        return _task;
    }

    public void UndoOperation()
    {
        if (_task?.IsCompleted != true)
            return;

        Settings.Log($"Undoing operation: {Description}");

        try
        {
            UndoAction();
            _isUndone = true;
            Settings.Content.Operations.Remove(this);
        }
        catch (Exception e)
        {
            GD.PrintErr(e);
            OS.Alert($"{e.Message}\n\nPlease report this error with logs.", "Error");
        }
    }
}