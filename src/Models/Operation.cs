using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;
using OsuSkinMixer.Statics;

namespace OsuSkinMixer.Models;

/// <summary>
/// A class that represents an operation to be peformed on the user's osu! folder.
/// </summary>
public class Operation
{
    [JsonPropertyName("type")]
    public OperationType Type { get; set; }

    [JsonPropertyName("name")]
    public string Description { get; set; }

    [JsonPropertyName("time_started")]
    public DateTime? TimeStarted { get; set; }

    [JsonIgnore]
    public Operation UndoOperation { get; }

    [JsonIgnore]
    private Action Action { get; }

    private Task _task;

    public Operation()
    {
    }

    public Operation(OperationType type, string description, Action action, Action undoAction = null)
    {
        Type = type;
        Description = description;
        Action = action;

        if (undoAction != null)
            UndoOperation = new Operation(type, $"Undo: {description}", undoAction);
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