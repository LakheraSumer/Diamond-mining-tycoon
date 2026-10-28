using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using DiamondMine.Runtime;
using DiamondMine.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiamondMine.Editor
{
    /// <summary>Captures actual Main Game view at each source aspect; runs only with a private QA save.</summary>
    public static class ReferenceUiReview
    {
        [Serializable] sealed class FileRecord { public string file,sha256,kind,note,group; }
        [Serializable] sealed class CaptureManifest { public string unity,scene,utc,saveRoot;public FileRecord[] images,inputs; }
        const string Key="DiamondMine.ReferenceUiReview";
        static readonly string[] Screens={"explore","mine","factory","craft","shop","map","tasks","rank"};
        static readonly List<string> errors=new List<string>(),checks=new List<string>();
        static string output;
        static int step;
        static double next,deadline;
        static bool pumping;
        static GameSession session;
        static YardControls controls;

        [MenuItem("Diamond Mine/Reference Review/Capture all eight screens")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start this capture with Play mode stopped.");
            double ready=EditorApplication.timeSinceStartup+3,waitDeadline=EditorApplication.timeSinceStartup+300;
            EditorApplication.CallbackFunction wait=null;
            wait=()=>{
                if(EditorApplication.timeSinceStartup>waitDeadline)
                {
                    EditorApplication.update-=wait;
                    Debug.LogError("Reference UI review startup timed out after 300 seconds.");
                    if(Application.isBatchMode)EditorApplication.Exit(1);
                    return;
                }
                if(EditorApplication.timeSinceStartup<ready||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
                EditorApplication.update-=wait;
                try
                {
                string evidence=Environment.GetEnvironmentVariable("DIAMOND_QA_EVIDENCE_ROOT")??Directory.GetCurrentDirectory();
                output=Path.Combine(evidence,"release/screenshots/ui-reference");Directory.CreateDirectory(output);
                SessionState.SetString(Key,output);
                string save=Path.Combine(Directory.GetCurrentDirectory(),".cache/qa-saves/ui-reference-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(save);
                Environment.SetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT",save);
                PortraitReview.SetSize(988,1136);EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
                EditorApplication.isPlaying=true;Boot();
                }
                catch(Exception ex)
                {
                    errors.Clear();checks.Clear();errors.Add("Reference UI review startup failed: "+ex);
                    Finish();
                }
            };
            EditorApplication.update+=wait;
        }

        [InitializeOnLoadMethod]
        static void Boot()
        {
            int code=SessionState.GetInt(Key+".exit",-1);
            if(code>=0){EditorApplication.delayCall+=()=>{SessionState.EraseInt(Key+".exit");if(Application.isBatchMode)EditorApplication.Exit(code);};return;}
            output=SessionState.GetString(Key,"");if(output.Length==0)return;
            step=-1;pumping=false;next=EditorApplication.timeSinceStartup+4;deadline=EditorApplication.timeSinceStartup+300;errors.Clear();checks.Clear();
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
        }
        static void Log(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);}
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup>deadline)
            {
                errors.Add("Reference UI review timed out after 300 seconds; session readiness or capture completion was not reached.");
                Finish();return;
            }
            if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
            try
            {
                session=UnityEngine.Object.FindAnyObjectByType<GameSession>();controls=UnityEngine.Object.FindAnyObjectByType<YardControls>();
                if(session==null||!session.Ready||controls==null)return;
                if(!pumping)
                {
                    Type pumpType=Type.GetType("DiamondMine.Runtime.EditorReviewPump, DiamondMine.Runtime",true);
                    Component pump=session.gameObject.AddComponent(pumpType);pumpType.GetField("TickAction").SetValue(pump,(Action)Tick);
                    EditorApplication.update-=Tick;pumping=true;return;
                }
                if(step==-1)
                {
                    if(!Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT").Contains("qa-saves"))throw new InvalidOperationException("Capture requires private save.");
                    if(!session.TryChooseCharacter("tavi"))throw new InvalidOperationException("Cannot select starter Tavi.");
                    session.DismissOfflineReport();PortraitReview.ClosePanels();
                    session.State.Settings.ReducedMotion=true;QualityPreset.Apply(2);
                    session.State.Adventure.PlayerX=YardLayout.HomeX;session.State.Adventure.PlayerZ=YardLayout.HomeZ;
                    checks.Add("Actual Main Play mode, isolated save, real imported world and uGUI.");
                    Open(0,true);step=0;next=EditorApplication.timeSinceStartup+1;return;
                }
                int index=step/2;
                if(index<16)
                {
                    bool review=index<8;int screen=index%8;
                    if(step%2==0)Capture(Screens[screen],review);
                    else if(index+1<16)Open((index+1)%8,index+1<8);
                    step++;next=EditorApplication.timeSinceStartup+.6;return;
                }
                if(step==32)
                {
                    controls.EndReferenceReview();controls.OpenScreen("explore");PortraitReview.SetSize(2160,3840);
                    step++;next=EditorApplication.timeSinceStartup+1;return;
                }
                if(step==33)
                {
                    Capture("explore-native-2160x3840",false);checks.Add("Native Game-view portrait capture requested at 2160x3840; exact dimensions recorded beside image.");
                    step++;next=EditorApplication.timeSinceStartup+.6;return;
                }
                if(step==34)
                {
                    controls.OpenScreen("map");PortraitReview.SetSize(1536,2048);step++;next=EditorApplication.timeSinceStartup+1;return;
                }
                if(step==35)
                {
                    Capture("map-tablet-1536x2048",false);checks.Add("Tablet portrait layout captured at 1536x2048; no cross-aspect pixel identity claim.");
                    step++;next=EditorApplication.timeSinceStartup+.6;return;
                }
                Finish();
            }
            catch(Exception ex){errors.Add(ex.ToString());Finish();}
        }
        static void Open(int index,bool review)
        {
            if(review)
            {
                controls.BeginReferenceReview(Screens[index]);Vector2Int viewport=controls.ReferenceViewport;
                PortraitReview.SetSize(viewport.x,viewport.y);
            }
            else
            {
                controls.EndReferenceReview();PortraitReview.SetSize(720,1280);controls.OpenScreen(Screens[index]);
            }
        }
        static void Capture(string screen,bool review)
        {
            Canvas.ForceUpdateCanvases();ReferenceUiReport report=controls.MeasureReferenceLayout();
            string folder=Path.Combine(output,review?"reference":"live");Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,screen+".png"));
            File.WriteAllText(Path.Combine(folder,screen+"-bounds.json"),JsonUtility.ToJson(report,true));
            if(review)
            {
                if(report==null||report.elements.Any(e=>!e.present||!e.withinTarget))
                    throw new InvalidOperationException(screen+" measured UI rectangle regression. See bounds JSON.");
                checks.Add(screen+": "+report.elements.Length+" measured elements within 0.5% of recorded JPEG rectangle bounds. Measurement uncertainty remains separate; visual identity is unscored.");
            }
            File.WriteAllText(Path.Combine(folder,screen+".txt"),"Actual Main Play-mode Game-view ScreenCapture. Unity "+Application.unityVersion+"; "+Screen.width+"x"+Screen.height+"; frame "+Time.frameCount+".\n"+
                (review?"UI-only deterministic demonstration values; actions disabled; visible DEMO label. Exact individual source crop aspect.\n":"Live authoritative game values in a fresh isolated starter save. Source anchors adapted to the native viewport, safe area and live navigation dock.\n")+
                "No raster reference background, UI text baked into backdrop or image-generation substitution.\n");
        }
        static void Finish()
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
            try
            {
            if(string.IsNullOrEmpty(output))
                output=Path.Combine(Environment.GetEnvironmentVariable("DIAMOND_QA_EVIDENCE_ROOT")??Directory.GetCurrentDirectory(),"release/screenshots/ui-reference");
            Directory.CreateDirectory(output);
            File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);File.WriteAllLines(Path.Combine(output,"errors.txt"),errors);
            File.WriteAllText(Path.Combine(output,"capture-context.txt"),"Actual Main Play-mode uGUI + imported 3D.\nPrivate QA save: "+Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT")+
                "\nReference images: source per-panel aspects at 4x native crop. Live images: 720x1280 adaptive portrait.\nBounds equality checks layout implementation against manually measured source rectangles. It does not certify total visual identity.\n");
            var images=new List<FileRecord>();
            foreach(string folder in new[]{"reference","live"})foreach(string path in Directory.Exists(Path.Combine(output,folder))?Directory.GetFiles(Path.Combine(output,folder),"*.png"):Array.Empty<string>())
            {
                images.Add(new FileRecord{file="release/screenshots/ui-reference/"+folder+"/"+Path.GetFileName(path),sha256=Hash(path),kind="Actual Main Play-mode Game-view screenshot",group=folder=="reference"?"Reference review screens":"Live gameplay screens",note=folder=="reference"?"Deterministic UI-only demonstration values; source crop aspect; actions disabled; visible DEMO label.":"Live authoritative values in isolated starter save; native dimensions recorded in adjacent text file."});
            }
            var inputs=new List<FileRecord>();
            foreach(string relative in new[]{"Assets/_Project/Scripts/Runtime/YardControls.cs","Assets/_Project/Scripts/Runtime/YardReferenceUi.cs","Assets/_Project/Scripts/Runtime/ReferenceUiGlyph.cs","Assets/_Project/Scripts/Runtime/UiFactory.cs","Assets/_Project/Scripts/Runtime/YardWorld.cs","Assets/_Project/Resources/Ui/reference-layout-runtime.json","Assets/_Project/Resources/Fonts/Nunito.ttf","Assets/_Project/Resources/Fonts/DiamondMineRounded-Bold.ttf"})
            {if(File.Exists(relative))inputs.Add(new FileRecord{file=relative,sha256=Hash(relative),kind="Capture input"});}
            foreach(string relative in new[]{"Assets/_Project/Art/Crew/Models","Assets/_Project/Art/GameplayEnvironments/Models"})
                if(Directory.Exists(relative))foreach(string path in Directory.GetFiles(relative,"*.fbx"))inputs.Add(new FileRecord{file=path.Replace('\\','/'),sha256=Hash(path),kind="Imported model input"});
            if(Directory.Exists("Assets/_Project/Resources/Ui"))
                foreach(string path in Directory.GetFiles("Assets/_Project/Resources/Ui","*.png"))inputs.Add(new FileRecord{file=path.Replace('\\','/'),sha256=Hash(path),kind="UI illustration input"});
            if(errors.Count==0)
                File.WriteAllText(Path.Combine(output,"capture-manifest.json"),JsonUtility.ToJson(new CaptureManifest{unity=Application.unityVersion,scene="Assets/_Project/Scenes/Main.unity",utc=DateTime.UtcNow.ToString("O"),saveRoot=Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT"),images=images.ToArray(),inputs=inputs.ToArray()},true));
            }
            catch(Exception ex)
            {
                errors.Add("Failed to write reference UI review evidence: "+ex);
                Debug.LogException(ex);
                try{if(!string.IsNullOrEmpty(output)&&Directory.Exists(output))File.WriteAllLines(Path.Combine(output,"errors.txt"),errors);}catch(Exception writeError){Debug.LogException(writeError);}
            }
            Debug.Log("Reference UI capture: "+checks.Count+" checks, "+errors.Count+" errors.");
            SessionState.EraseString(Key);SessionState.SetInt(Key+".exit",errors.Count==0?0:1);
            EditorApplication.isPlaying=false;
            EditorApplication.delayCall+=()=>{if(EditorApplication.isPlayingOrWillChangePlaymode)return;int code=SessionState.GetInt(Key+".exit",-1);if(code<0)return;SessionState.EraseInt(Key+".exit");if(Application.isBatchMode)EditorApplication.Exit(code);};
        }
        static string Hash(string path)
        {using(SHA256 hash=SHA256.Create())using(FileStream stream=File.OpenRead(path))return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
    }
}
