// Diag.cs
using UnityEngine;
using System;
using System.Diagnostics;                 // for Conditional, StackTrace, Stopwatch
using UDebug = UnityEngine.Debug;          // alias Unity logger
using SDebug = System.Diagnostics.Debug;   // (optional) alias .NET debug if you ever want it
using Stopwatch = System.Diagnostics.Stopwatch;

public enum DiagLevel { Quiet = 0, Errors = 1, Warnings = 2, Info = 3, Verbose = 4 }

public static class Diag
{
    public static bool Enabled = true;
    public static DiagLevel Level = DiagLevel.Info;

    const string CTag  = "#8888FF";  // tag
    const string CInfo = "#A0FFA0";
    const string CWarn = "#FFD26E";
    const string CErr  = "#FF6E6E";
    const string CStep = "#AEC6FF";

    static string T() => DateTime.Now.ToString("HH:mm:ss.fff");

    public static void ApplySettings(DiagSettings s)
    {
        if (s == null) return;
        Enabled = s.enabled;
        Level   = s.level;
    }

    [Conditional("UNITY_EDITOR")]
    public static void Info(string tag, string msg, UnityEngine.Object ctx = null)
    {
        if (!Enabled || Level < DiagLevel.Info) return;
        UDebug.Log($"<color={CTag}>[{tag}]</color> <color={CInfo}>{T()} {msg}</color>", ctx);
    }

    [Conditional("UNITY_EDITOR")]
    public static void Warn(string tag, string msg, UnityEngine.Object ctx = null)
    {
        if (!Enabled || Level < DiagLevel.Warnings) return;
        UDebug.LogWarning($"<color={CTag}>[{tag}]</color> <color={CWarn}>{T()} {msg}</color>", ctx);
    }

    [Conditional("UNITY_EDITOR")]
    public static void Error(string tag, string msg, UnityEngine.Object ctx = null)
    {
        if (!Enabled || Level < DiagLevel.Errors) return;
        UDebug.LogError($"<color={CTag}>[{tag}]</color> <color={CErr}>{T()} {msg}</color>", ctx);
    }

    public static Scope Begin(string tag, string what, UnityEngine.Object ctx = null)
        => new Scope(tag, what, ctx);

    public sealed class Scope : IDisposable
    {
        readonly string tag, what;
        readonly UnityEngine.Object ctx;
        readonly Stopwatch sw = new Stopwatch();
        int stepIdx = 0;
        bool disposed;

        public Scope(string tag, string what, UnityEngine.Object ctx = null)
        {
            this.tag = tag; this.what = what; this.ctx = ctx;
            if (Enabled && Level >= DiagLevel.Info)
                UDebug.Log($"<color={CTag}>[{tag}]</color> <b>START</b> {what}  <i>{T()}</i>", ctx);
            sw.Start();
        }

        [Conditional("UNITY_EDITOR")]
        public void Step(string msg)
        {
            if (!Enabled || Level < DiagLevel.Verbose) return;
            stepIdx++;
            UDebug.Log($"<color={CTag}>[{tag}]</color> <color={CStep}>Step {stepIdx:00}:</color> {msg}  <i>{T()}</i>", ctx);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            sw.Stop();
            if (Enabled && Level >= DiagLevel.Info)
                UDebug.Log($"<color={CTag}>[{tag}]</color> <b>END</b> {what}  ⏱ {sw.ElapsedMilliseconds} ms  <i>{T()}</i>", ctx);
        }
    }
}
