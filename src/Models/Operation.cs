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
    public Operation UndoOperation { get; }

    [JsonIgnore]
    private Action Action { get; }

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

        if (undoAction != null)
            UndoOperation = new Operation(type, targetSkin, undoAction);
    }

    public Task RunOperation()
    {
        if (_task != null)
            return _task;

        OsuData.SweepPaused = true;
        TimeStarted = DateTime.Now;

        Settings.Log($"Running operation: {Description}");

        _task = Task.Run(() => Action())
            .ContinueWith(t =>
            {
                OsuData.SweepPaused = false;

                if (t.IsFaulted)
                {
                    GD.PrintErr(t.Exception);
                    OS.Alert($"{t.Exception.Message}\n\nPlease report this error with logs.", "Error");
                    return;
                }

                Settings.Log($"Operation completed: {Description}");
                Settings.Content.Operations.Push(this);
            });

        return _task;
    }
}