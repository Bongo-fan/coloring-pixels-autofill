using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using UnityEngine;

[BepInPlugin("local.coloringpixels.autofill", "Coloring Pixels Autofill", "1.0.0")]
public sealed class PixelAutofill : BaseUnityPlugin
{
    ClickTest board;
    ChannelChange palette;
    GridLayout grid;
    Camera cam;
    Action<Vector3> cast;
    int[,] answers, progress;
    readonly List<int> pending = new List<int>();
    int cursor, filled, width, height;
    bool running, grouped = true, panel = true;
    string status = "Open a picture. F8 start / stop.";
    float speed = 64;

    void Stop(string reason) { running = false; status = reason; }

    void Begin()
    {
        board = FindObjectOfType<ClickTest>();
        palette = FindObjectOfType<ChannelChange>();
        cam = Camera.main;
        if (!board || !board.initialised || board.gameOver || board.inputDisabled || !palette || !cam)
        { Stop("Open an unfinished picture first."); return; }
        grid = board.transform.parent.GetComponent<GridLayout>();
        var method = typeof(ClickTest).GetMethod("CastRay", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
            null, new Type[] { typeof(Vector3) }, null);
        if (!grid || method == null) { Stop("Unsupported game version."); return; }
        cast = (Action<Vector3>)Delegate.CreateDelegate(typeof(Action<Vector3>), board, method);
        answers = board.mainGridValues;
        progress = board.savedGridValues;
        width = answers.GetLength(0); height = answers.GetLength(1);
        pending.Clear(); cursor = filled = 0;
        var buckets = new SortedDictionary<int,List<int>>();
        for (int y=height-1; y>=0; --y)
            for (int x=0; x<width; ++x)
            {
                int color = answers[x,y], index = y*width+x;
                if (color <= 0 || progress[x,y] == color) continue;
                if (!grouped) pending.Add(index);
                else {
                    List<int> bucket;
                    if (!buckets.TryGetValue(color,out bucket)) buckets[color]=bucket=new List<int>();
                    bucket.Add(index);
                }
            }
        foreach (var bucket in buckets.Values) pending.AddRange(bucket);
        running = pending.Count > 0;
        status = running ? "Painting" : "Already complete.";
    }

    void Fill(int x,int y)
    {
        int color = answers[x,y];
        if (progress[x,y] == color) return;
        if (board.selectedColourID != color) palette.SetColNumber(color);
        if (board.selectedColourID != color) throw new InvalidOperationException("Color selection failed.");
        // CastRay maps the argument through Camera, board scale/translation, then the parent Grid.
        // Place this cell at the virtual click for the duration of the game's own handler.
        Vector3 center = (grid.CellToWorld(new Vector3Int(x,y,0)) + grid.CellToWorld(new Vector3Int(x+1,y+1,0))) * 0.5f;
        Vector3 screen = new Vector3(Screen.width/2, Screen.height/2, 0);
        Vector3 world = cam.ScreenToWorldPoint(screen);
        Transform t = board.transform;
        Vector3 old = t.position, scale = t.localScale;
        try {
            t.position = new Vector3(world.x/scale.x-center.x,world.y/scale.y-center.y,old.z);
            Vector3 actual = t.position;
            Vector3Int cell = grid.WorldToCell(new Vector3(world.x/scale.x-actual.x,world.y/scale.y-actual.y,0));
            if (cell.x != x || cell.y != y) throw new InvalidOperationException("Grid mapping failed.");
            cast(screen);
        } finally { t.position = old; }
        if (progress[x,y] != color) throw new InvalidOperationException("Game did not accept cell.");
        filled++;
    }

    void Update()
    {
        try {
            if (Input.GetKeyDown(KeyCode.F7)) panel = !panel;
            if (Input.GetKeyDown(KeyCode.F8)) { if (running) Stop("Stopped."); else Begin(); }
            if (!running) return;
            if (Input.GetKeyDown(KeyCode.Escape) || !board || !palette || !cam || board.inputDisabled ||
                board.gameOver || board.mainGridValues != answers || board.savedGridValues != progress)
            { Stop("Stopped."); return; }
            float until = Time.realtimeSinceStartup + 0.004f;
            for (int n=0; n<(int)speed && cursor<pending.Count; ++n)
            {
                int index = pending[cursor];
                Fill(index%width,index/width); cursor++;
                if (Time.realtimeSinceStartup >= until) break;
            }
            if (cursor == pending.Count) Stop("Finished. Verified cells: "+filled);
        } catch (Exception e) { Stop(e.Message); Logger.LogError(e); }
    }

    void OnGUI()
    {
        if (!panel) return;
        GUILayout.BeginArea(new Rect(8,8,285,190),GUI.skin.box);
        GUILayout.Label("Coloring Pixels Autofill   [F7] hide");
        GUILayout.Label(running ? "Verified "+filled+" / "+pending.Count : status);
        if (!running) grouped=GUILayout.Toggle(grouped,"Group by color (off: row order)");
        GUILayout.Label("Cells per frame: "+(int)speed);
        speed=GUILayout.HorizontalSlider(speed,1,256);
        if (GUILayout.Button(running ? "Stop [F8]" : "Start / resume [F8]"))
        {
            if (running) Stop("Stopped.");
            else try { Begin(); } catch(Exception e) { Stop(e.Message); Logger.LogError(e); }
        }
        GUILayout.EndArea();
    }
}
