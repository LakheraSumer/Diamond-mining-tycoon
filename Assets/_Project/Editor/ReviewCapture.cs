using System;
using System.IO;
using DiamondMine.Runtime;
using UnityEditor;
using UnityEngine;

namespace DiamondMine.Editor
{
    // Run with graphics enabled and WITHOUT -quit. This captures an actual
    // running game, then exits. It never alters the player's normal save.
    public static class ReviewCapture
    {
        static double deadline;
        static double started;
        static string destination;
        static bool requested;
        static double nextTap;

        public static void CapturePhone()
        {
            destination = Path.Combine(Directory.GetCurrentDirectory(), "release", "screenshots", "phone-gameplay.png");
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            Environment.SetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT", Path.Combine(Directory.GetCurrentDirectory(), ".cache", "review-save"));
            SessionState.SetString("DiamondMine.ReviewCapture", destination);
            EditorApplication.isPlaying = true;
            OnLoad();
        }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            destination = SessionState.GetString("DiamondMine.ReviewCapture", "");
            if (string.IsNullOrEmpty(destination)) return;
            started = EditorApplication.timeSinceStartup;
            deadline = started + 45;
            requested = false;
            nextTap = started + 2;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                Finish(1);
                return;
            }
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - started < 5) return;
            GameSession session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            if (session == null || !session.Ready) return;
            if (EditorApplication.timeSinceStartup >= nextTap && !requested)
            {
                session.TryTap();
                nextTap = EditorApplication.timeSinceStartup + 0.5;
            }
            if (EditorApplication.timeSinceStartup - started < 20) return;
            if (!requested)
            {
                ScreenCapture.CaptureScreenshot(destination);
                requested = true;
            }
            else if (File.Exists(destination)) Finish(0);
        }

        static void Finish(int code)
        {
            SessionState.EraseString("DiamondMine.ReviewCapture");
            EditorApplication.update -= Tick;
            EditorApplication.Exit(code);
        }
    }
}
