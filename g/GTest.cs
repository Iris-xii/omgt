using Godot;
using System;
using System.Collections.Generic;

public partial class GTest : Node
{

    static String SAVE_PATH = "res://playback/";
    [Export(PropertyHint.File)] String loadFrom = "res://playback/null.cfg"; 

    static StringName RECORD = "d_record";
    static StringName PLAYBACK = "d_playback";
    static StringName STOP = "d_stop";
    static StringName SAVE = "d_save";
    static StringName LOAD = "d_load";

    [ExportGroup("Debug")]
    [Export] double ForceSpeedScaleDebug { get => Engine.TimeScale; set {
        if (this.IsNodeReady()) { Engine.TimeScale = value; }
    }}

    List<SerializedInput> recorded = new();
    enum State {NONE,RECORDING,PLAYING}
    State currently = State.NONE;
    ulong playbackStartTime;
    ulong recordingOffsetTime;
    int playbackHead = 0;
 

    public override void _Input(InputEvent e)
    {
        if(OS.IsDebugBuild()) {
            if (e.IsActionPressed(RECORD)) {StartRecording(); return;}
            else if (e.IsActionPressed(PLAYBACK)) {StartPlayback(); return;}
            else if (e.IsActionPressed(STOP)) {GD.Print("Stopped playback/record");currently = State.NONE; return;}
            else if (e.IsActionPressed(SAVE)) {GD.Print("Saving playback");currently = State.NONE;SaveCurrentRecording(); return;}
            else if (e.IsActionPressed(LOAD)) {GD.Print("Loading playback");LoadRecording(loadFrom);return;}
        }
        //
        if (currently != State.RECORDING) {return;}
        if (!OS.IsDebugBuild()) {currently = State.NONE; return;};
        
        recorded.Add(new SerializedInput(
            GD.VarToBytesWithObjects(e),
            Time.GetTicksUsec() - recordingOffsetTime
        ));
    }

    public override void _Process(double _)
    {
        if(currently != State.PLAYING) {SetProcess(false); return;}
        var timeCurrent = (Time.GetTicksUsec() - playbackStartTime) * Engine.TimeScale;
        for (int iter = 0; iter < 100; iter++) // iteration limit
        {
            if(playbackHead >= recorded.Count) {currently = State.NONE; return;}
            var serIn = recorded[playbackHead];
            if(serIn.time > timeCurrent) {return;}

            Input.ParseInputEvent((InputEvent) GD.BytesToVarWithObjects(serIn.obj).AsGodotObject());
            playbackHead++;
        }
    }

    public void LoadRecording(string path) {
        recorded.Clear();

        using var file = FileAccess.Open(path,FileAccess.ModeFlags.Read);
        if(file == null) {GD.PushWarning($"Couldn't open playback file {path}");return;}
        while (file.GetPosition() < file.GetLength()) {
            var time = file.Get64();
            var bufLen = file.Get32();
            var obj = file.GetBuffer(bufLen);
            if (file.GetError() == Error.Ok ) {
                recorded.Add(new SerializedInput(
                    obj,
                    time
                ));
            } else {
                var err = file.GetError();
                GD.PushWarning($"Playback file errored with {err}");
                recorded.Clear();
            }
        }
        file.Close();
    }

    public void SaveCurrentRecording() {
        DirAccess.MakeDirRecursiveAbsolute(SAVE_PATH);
        var datetime = Time.GetDatetimeStringFromSystem()
            .Replace("-","x")
            .Replace(":","x");
        using var file = FileAccess.Open($"{SAVE_PATH}/{datetime}.cfg", FileAccess.ModeFlags.Write);
        foreach (SerializedInput val in recorded)
        {
            file.Store64(val.time);
            file.Store32((uint) val.obj.Length);
            file.StoreBuffer(val.obj);
        }
        file.Close();
    }

    public void StartRecording() {
        if(currently != State.NONE) return;
        GD.Print("Recording Started");

        recorded.Clear();
        currently = State.RECORDING;
        playbackHead = 0;
        recordingOffsetTime = Time.GetTicksUsec();
    }

    public void StartPlayback() {
        if(currently == State.PLAYING) return;
        GD.Print("Playback Started");

        currently = State.PLAYING;
        playbackHead = 0;
        playbackStartTime = Time.GetTicksUsec();
        SetProcess(true);
    }
}

public struct SerializedInput {
    public byte[] obj;
    public ulong time; 

    public SerializedInput(byte[] o,ulong t) {
        this.obj = o;
        this.time = t; 
    }
} 