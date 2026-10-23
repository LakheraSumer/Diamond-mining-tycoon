using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DiamondMine.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DiamondMine.Editor
{
    // Editor-only rendered review. All progress is isolated from the normal player save.
    public static class PortraitReview
    {
        const string Key = "DiamondMine.PortraitReview";
        static readonly Queue<Action> steps = new Queue<Action>();
        static double next;
        static double timeout;
        static string output;
        static string sizeName;
        static bool prepared;
        static bool pumping;
        static readonly List<string> errors = new List<string>();
        static readonly List<string> captures = new List<string>();

        public static void Before() { Schedule("before"); }
        public static void After() { Schedule("after"); }
        public static void Focused() { Schedule("focused"); }

        static void Schedule(string phase)
        {
            double ready = EditorApplication.timeSinceStartup + 8;
            EditorApplication.CallbackFunction wait = null;
            wait = () => {
                if (EditorApplication.timeSinceStartup < ready || EditorApplication.isUpdating || EditorApplication.isCompiling) return;
                EditorApplication.update -= wait;
                Start(phase);
            };
            EditorApplication.update += wait;
        }

        [MenuItem("Diamond Mine/QA/Play isolated portrait")]
        public static void PlayIsolated()
        {
            PrepareSave("interactive");
            SetSize(720, 1280);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            EditorApplication.isPlaying = true;
        }

        static void PrepareSave(string phase)
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), ".cache", "qa-saves", phase + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(root);
            Environment.SetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT", root);
            Debug.Log("QA isolated save: " + root);
        }

        static void Start(string phase)
        {
            PrepareSave(phase);
            string evidenceRoot = Environment.GetEnvironmentVariable("DIAMOND_QA_EVIDENCE_ROOT");
            if (string.IsNullOrEmpty(evidenceRoot)) evidenceRoot = Directory.GetCurrentDirectory();
            output = Path.Combine(evidenceRoot, "release", "screenshots", "portrait-" + phase);
            Directory.CreateDirectory(output);
            SessionState.SetString(Key, output);
            SetSize(720, 1280);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            EditorApplication.isPlaying = true;
            OnLoad();
        }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            int exitCode=SessionState.GetInt(Key+".exit",-1);
            if(exitCode>=0)
            {
                EditorApplication.delayCall += () => { SessionState.EraseInt(Key+".exit"); EditorApplication.Exit(exitCode); };
                return;
            }
            EditorApplication.update -= ReadRequest;
            EditorApplication.update += ReadRequest;
            output = SessionState.GetString(Key, "");
            if (string.IsNullOrEmpty(output)) return;
            prepared = false;
            pumping = false;
            errors.Clear();
            captures.Clear();
            next = EditorApplication.timeSinceStartup + 5;
            timeout = EditorApplication.timeSinceStartup + 360;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= Log;
            Application.logMessageReceived += Log;
        }

        static void ReadRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            string request = Path.Combine(Directory.GetCurrentDirectory(), ".cache", "portrait-review-request.txt");
            string phase = SessionState.GetString(Key + ".pending", "");
            if (File.Exists(request))
            {
                phase = File.ReadAllText(request).Trim();
                File.Delete(request);
                if (phase != "before" && phase != "after") throw new InvalidOperationException("Unknown QA capture request.");
                SessionState.SetString(Key + ".pending", phase);
            }
            if (string.IsNullOrEmpty(phase)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
            SessionState.EraseString(Key + ".pending");
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.IsValid() && active.isDirty)
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(active, ".cache/scene-before-qa.unity", true);
            Start(phase);
        }

        static void Log(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors.Add(message + "\n" + trace);
        }

        static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > timeout) throw new TimeoutException("Portrait review timed out.");
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
                GameSession session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
                if (session == null || !session.Ready) return;
                if (!pumping)
                {
                    pumping = true;
                    Type pumpType = typeof(GameSession).Assembly.GetType("DiamondMine.Runtime.EditorReviewPump",true);
                    Component pump = session.gameObject.AddComponent(pumpType);
                    pumpType.GetField("TickAction").SetValue(pump,(Action)Tick);
                    EditorApplication.update -= Tick;
                    return;
                }
                if (!prepared)
                {
                    prepared = true;
                    steps.Clear();
                    if (output.EndsWith("portrait-focused",StringComparison.Ordinal))
                    {
                        steps.Enqueue(() => { SetSize(720,1280); sizeName="reset-regression"; });
                        steps.Enqueue(() => GameplayReview.StartResetReview(Path.Combine(output,"reset-integration.json")));
                        steps.Enqueue(GameplayReview.ScrollSettingsBottom);
                        steps.Enqueue(() => Capture("settings-bottom"));
                        steps.Enqueue(GameplayReview.OpenReset);
                        steps.Enqueue(() => Capture("reset-first-confirmation"));
                        steps.Enqueue(GameplayReview.ArmReset);
                        steps.Enqueue(() => Capture("reset-second-confirmation"));
                        steps.Enqueue(GameplayReview.BackFromReset);
                        steps.Enqueue(GameplayReview.ScrollSettingsBottom);
                        steps.Enqueue(GameplayReview.OpenReset);
                        steps.Enqueue(() => Capture("reset-reopened-after-back"));
                        steps.Enqueue(GameplayReview.CheckResetReopened);
                        steps.Enqueue(CloseAll);
                        foreach(Vector2Int size in new[] {new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1080,2400),new Vector2Int(1536,2048)})
                        {
                            Vector2Int current=size;
                            steps.Enqueue(() => { SetSize(current.x,current.y);sizeName=current.x+"x"+current.y;Open("Offline"); });
                            steps.Enqueue(() => Capture("offline-layout-empty-report"));
                            steps.Enqueue(CloseAll);
                        }
                    }
                    else
                    {
                    foreach (Vector2Int size in new[] { new Vector2Int(720,1280), new Vector2Int(1080,1920), new Vector2Int(1080,2400), new Vector2Int(1536,2048) })
                    {
                        Vector2Int current = size;
                        steps.Enqueue(() => { SetSize(current.x,current.y); sizeName = current.x + "x" + current.y; });
                        steps.Enqueue(() => Capture("main"));
                        foreach (string panel in new[] { "Upgrades", "Goals", "Orders", "Mines", "Prestige", "Settings", "Credits", "Privacy", "Reset", "Quit" })
                        {
                            string currentPanel = panel;
                            steps.Enqueue(() => { CloseAll(); Open(currentPanel); });
                            steps.Enqueue(() => Capture(currentPanel.ToLowerInvariant()));
                        }
                        steps.Enqueue(CloseAll);
                    }
                    }
                    if (output.EndsWith("portrait-after",StringComparison.Ordinal))
                    {
                        steps.Enqueue(() => { SetSize(1080,2400); sizeName="1080x2400-safe-insets"; typeof(GameUi).GetField("ReviewSafeAreaOverride",BindingFlags.Public|BindingFlags.Static).SetValue(null,(Rect?)new Rect(0,90,1080,2190)); });
                        steps.Enqueue(() => Capture("simulated-safe-area"));
                        steps.Enqueue(() => { typeof(GameUi).GetField("ReviewSafeAreaOverride",BindingFlags.Public|BindingFlags.Static).SetValue(null,null); SetSize(720,1280); sizeName="gameplay-fixtures"; CloseAll(); });
                        steps.Enqueue(() => GameplayReview.Run(Path.Combine(output,"editor-integration.json")));
                        steps.Enqueue(GameplayReview.BeginModalChecks);
                        steps.Enqueue(GameplayReview.CheckModalAndClose);
                        steps.Enqueue(GameplayReview.CheckClosedAndOpenPrestige);
                        steps.Enqueue(() => Capture("prestige-unarmed-fixture"));
                        steps.Enqueue(GameplayReview.CheckPrestigeAndCancel);
                        steps.Enqueue(GameplayReview.CheckCancelAndReopen);
                        steps.Enqueue(GameplayReview.ArmPrestige);
                        steps.Enqueue(() => Capture("prestige-armed-fixture"));
                        steps.Enqueue(GameplayReview.ConfirmPrestige);
                        steps.Enqueue(GameplayReview.CheckPrestigeSaved);
                        steps.Enqueue(() => Capture("prestige-completed"));
                        steps.Enqueue(GameplayReview.PrepareLateGame);
                        foreach(string mine in new[] {"warmstone","crystal","deepcore"})
                        {
                            string currentMine=mine;
                            steps.Enqueue(() => session.TrySelectMine(currentMine));
                            steps.Enqueue(() => Capture(currentMine + "-automated-fixture"));
                        }
                        steps.Enqueue(() => Open("Prestige"));
                        steps.Enqueue(() => Capture("prestige-available-fixture"));
                        steps.Enqueue(CloseAll);
                        steps.Enqueue(GameplayReview.PrepareOffline);
                        steps.Enqueue(() => Capture("offline-committed-fixture"));
                        steps.Enqueue(CloseAll);
                    }
                }
                if (steps.Count == 0) { Finish(); return; }
                steps.Dequeue()();
                next = EditorApplication.timeSinceStartup + 1.2;
            }
            catch (Exception ex) { errors.Add(ex.ToString()); Finish(); }
        }

        public static void SetSize(int width, int height)
        {
            Assembly assembly = typeof(EditorWindow).Assembly;
            Type sizesType = assembly.GetType("UnityEditor.GameViewSizes", true);
            object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            object group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            Type groupType = group.GetType();
            Type sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
            Type kind = assembly.GetType("UnityEditor.GameViewSizeType", true);
            string label = "Diamond QA " + width + "x" + height;
            object entry = Activator.CreateInstance(sizeType, new object[] { Enum.Parse(kind,"FixedResolution"), width, height, label });
            int index = (int)groupType.GetMethod("GetTotalCount").Invoke(group,null);
            groupType.GetMethod("AddCustomSize").Invoke(group,new[] { entry });
            Type viewType = assembly.GetType("UnityEditor.GameView",true);
            EditorWindow window = EditorWindow.GetWindow(viewType);
            viewType.GetMethod("SizeSelectionCallback").Invoke(window,new object[] { index,null });
            window.Show();
            window.Focus();
            window.Repaint();
        }

        static GameUi Ui { get { return UnityEngine.Object.FindAnyObjectByType<GameUi>(); } }
        public static void FrameTick() { Tick(); }
        public static void ClosePanels() { CloseAll(); }
        public static void CloseTopPanel() { typeof(GameUi).GetMethod("Pop",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Ui,null); }
        static void Open(string name)
        {
            Type modal = typeof(GameUi).GetNestedType("Modal",BindingFlags.NonPublic);
            typeof(GameUi).GetMethod("Push",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Ui,new object[] { Enum.Parse(modal,name),false });
        }
        static void CloseAll()
        {
            object stack = typeof(GameUi).GetField("stack",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Ui);
            while ((int)stack.GetType().GetProperty("Count").GetValue(stack) > 0)
                typeof(GameUi).GetMethod("Pop",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Ui,null);
        }

        static void Capture(string scenario)
        {
            Canvas.ForceUpdateCanvases();
            string directory = Path.Combine(output,sizeName);
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory,scenario + ".png");
            ScreenCapture.CaptureScreenshot(path);
            captures.Add(path);
            var lines = new List<string> { "unity=" + Application.unityVersion, "version=" + Branding.Version, "build=" + Branding.BuildNumber, "scenario=" + (sizeName == "gameplay-fixtures" ? "accelerated QA fixture / " : "fresh isolated QA / ") + scenario, "requested=" + sizeName, "actual=" + Screen.width + "x" + Screen.height, "safeArea=" + Screen.safeArea, "editorSafeAreaOverride=" + typeof(GameUi).GetField("ReviewSafeAreaOverride",BindingFlags.Public|BindingFlags.Static)?.GetValue(null), "saveRoot=" + Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT"), "deviceTest=false" };
            foreach (Text label in Ui.GetComponentsInChildren<Text>())
                lines.Add(label.rectTransform.rect.size + " | preferred=" + label.preferredHeight + " | " + label.text.Replace("\n"," / "));
            File.WriteAllLines(Path.ChangeExtension(path,"txt"),lines);
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= Log;
            GameplayReview.CompleteReport();
            SessionState.EraseString(Key);
            foreach (string capture in captures) if (!File.Exists(capture)) errors.Add("Missing capture: " + capture);
            Directory.CreateDirectory(output);
            File.WriteAllLines(Path.Combine(output,"review-errors.txt"),errors);
            Debug.Log("Portrait review captured " + captures.Count + " views. Errors: " + errors.Count);
            SessionState.SetInt(Key+".exit",errors.Count == 0 ? 0 : 1);
            EditorApplication.isPlaying=false;
            EditorApplication.delayCall += () => {
                if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                int code=SessionState.GetInt(Key+".exit",-1);
                if(code<0)return;
                SessionState.EraseInt(Key+".exit");
                EditorApplication.Exit(code);
            };
        }
    }
}
