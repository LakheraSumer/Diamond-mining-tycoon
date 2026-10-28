// Copied into the isolated QA editor only. No production scene or save changes.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using DiamondMine.Runtime;
using DiamondMine.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DiamondMine.Editor
{
    public static class VisualGalleryCapture
    {
        const string Key="DiamondMine.VisualGalleryCapture";
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static readonly string[] Modes={"","mine","factory","craft","shop","map","tasks","rank","characters","bag","more"};
        static readonly string[] Names={"01-explore","02-mine","03-process","04-craft","05-sell","06-expand","07-progress","08-rankings","09-character-selection","10-inventory","11-more"};
        static readonly List<string> Errors=new List<string>();
        [Serializable] sealed class CaptureImage { public string file,sha256,kind,note,group; }
        [Serializable] sealed class CaptureManifest { public CaptureImage[] images; public string unity,scene,utc,saveRoot; }
        static string output;
        static int step;
        static double next;
        static bool pumping;
        static GameSession session;
        static YardWorld yard;
        static YardControls controls;
        public static void Run()
        {
            double ready=EditorApplication.timeSinceStartup+4;
            EditorApplication.CallbackFunction wait=null;
            wait=()=>{
                if(EditorApplication.timeSinceStartup<ready||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
                EditorApplication.update-=wait;
                try
                {
                    string root=Environment.GetEnvironmentVariable("DIAMOND_QA_EVIDENCE_ROOT")??Directory.GetCurrentDirectory();
                    output=Path.Combine(root,"release/screenshots/gameplay-current");Directory.CreateDirectory(output);
                    SessionState.SetString(Key,output);
                    string save=Path.Combine(Directory.GetCurrentDirectory(),".cache/qa-saves/gallery-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
                    Directory.CreateDirectory(save);Environment.SetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT",save);
                    PortraitReview.SetSize(720,1280);EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
                    EditorApplication.isPlaying=true;Boot();
                }
                catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
            };
            EditorApplication.update+=wait;
        }
        [InitializeOnLoadMethod]
        static void Boot()
        {
            int exit=SessionState.GetInt(Key+".exit",-1);
            if(exit>=0){EditorApplication.delayCall+=()=>{SessionState.EraseInt(Key+".exit");EditorApplication.Exit(exit);};return;}
            output=SessionState.GetString(Key,"");if(output.Length==0)return;
            step=-1;pumping=false;Errors.Clear();next=EditorApplication.timeSinceStartup+3;
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
        }
        static void Log(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Errors.Add(message+"\n"+stack);}
        static void Tick()
        {
            if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
            try
            {
                session=UnityEngine.Object.FindAnyObjectByType<GameSession>();yard=UnityEngine.Object.FindAnyObjectByType<YardWorld>();controls=UnityEngine.Object.FindAnyObjectByType<YardControls>();
                if(session==null||!session.Ready||yard==null||controls==null)return;
                if(!pumping)
                {
                    Type type=Type.GetType("DiamondMine.Runtime.EditorReviewPump, DiamondMine.Runtime",true);
                    Component pump=session.gameObject.AddComponent(type);type.GetField("TickAction").SetValue(pump,(Action)Tick);
                    EditorApplication.update-=Tick;pumping=true;return;
                }
                if(step==-1)
                {
                    if(!session.TryChooseCharacter("tavi"))throw new InvalidOperationException("Cannot select Tavi.");
                    session.State.Settings.ReducedMotion=true;
                    session.State.Adventure.PlayerX=YardLayout.HomeX;session.State.Adventure.PlayerZ=YardLayout.HomeZ;
                    session.State.Adventure.FocusPlotId=0;
                    yard.SetShown(true);QualityPreset.Apply(2);yard.Facing=145;
                    Open(0);step=0;next=EditorApplication.timeSinceStartup+.6;return;
                }
                if(step<Modes.Length*2)
                {
                    int index=step/2;
                    if(step%2==0)
                    {
                        Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(output,Names[index]+".png"));
                        File.WriteAllText(Path.Combine(output,Names[index]+".txt"),"Actual Main Play-mode Game-view ScreenCapture. Unity "+Application.unityVersion+
                            "; 720x1280; UTC "+DateTime.UtcNow.ToString("O")+"\nFresh starter QA save, no progression fixtures. Camera frozen by reduced-motion setting.\nPanel: "+Modes[index]+"\n");
                    }
                    else if(index+1<Modes.Length)Open(index+1);
                    step++;next=EditorApplication.timeSinceStartup+.5;return;
                }
                if(step==Modes.Length*2)
                {
                    typeof(YardControls).GetMethod("ClosePanel",Hidden).Invoke(controls,null);
                    Camera camera=yard.View;Vector3 oldPosition=camera.transform.position;Quaternion oldRotation=camera.transform.rotation;
                    bool ortho=camera.orthographic;float size=camera.orthographicSize;float aspect=camera.aspect;
                    camera.orthographic=true;camera.orthographicSize=11.5f;camera.aspect=1.5f;
                    Vector3 target=new Vector3(0,0,4.5f);camera.transform.position=target+new Vector3(14,18,-23);camera.transform.LookAt(target);
                    var rt=new RenderTexture(1500,1000,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                    var image=new Texture2D(1500,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1500,1000),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(output,"12-entire-yard.png"),image.EncodeToPNG());
                    camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
                    camera.transform.SetPositionAndRotation(oldPosition,oldRotation);camera.orthographic=ortho;camera.orthographicSize=size;camera.aspect=aspect;
                    File.WriteAllText(Path.Combine(output,"12-entire-yard.txt"),"Actual Unity camera render during Main Play mode. Full yard overview, UI omitted. Unfinished surrounding environment remains visible.\n");
                    step++;next=EditorApplication.timeSinceStartup+.5;return;
                }
                File.WriteAllText(Path.Combine(output,"capture-context.txt"),"Unity "+Application.unityVersion+"; actual Main Play mode; 11 current interface screenshots and one world camera overview.\n"+
                    "Fresh starter save: "+Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT")+"\nCurrent working art; no claim of matching the supplied concept.\n");
                Finish();
            }
            catch(Exception ex){Errors.Add(ex.ToString());Finish();}
        }
        static void Open(int index)
        {
            if(index==0)typeof(YardControls).GetMethod("ClosePanel",Hidden).Invoke(controls,null);
            else if(Modes[index]=="mine")typeof(YardControls).GetMethod("OpenMine",Hidden).Invoke(controls,null);
            else typeof(YardControls).GetMethod("Open",Hidden).Invoke(controls,new object[]{Modes[index]});
            foreach(ScrollRect scroll in controls.GetComponentsInChildren<ScrollRect>())scroll.verticalNormalizedPosition=1;
        }
        static void Finish()
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;File.WriteAllLines(Path.Combine(output,"capture-errors.txt"),Errors);
            if(Errors.Count==0)
            {
                var captures=new List<CaptureImage>();
                foreach(string file in Directory.GetFiles(output,"*.png"))
                    using(var hash=SHA256.Create())using(var stream=File.OpenRead(file))captures.Add(new CaptureImage{file="release/screenshots/gameplay-current/"+Path.GetFileName(file),sha256=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant(),kind="Actual Main Play-mode capture",note="Current imported development assets; fresh isolated starter save; illustration match remains unfinished.",group=Path.GetFileName(file).StartsWith("12-",StringComparison.Ordinal)?"Environment":"Gameplay"});
                File.WriteAllText(Path.Combine(output,"evidence-manifest.json"),JsonUtility.ToJson(new CaptureManifest{unity=Application.unityVersion,scene="Assets/_Project/Scenes/Main.unity",utc=DateTime.UtcNow.ToString("O"),saveRoot=Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT"),images=captures.ToArray()},true));
            }
            SessionState.EraseString(Key);SessionState.SetInt(Key+".exit",Errors.Count==0?0:1);EditorApplication.isPlaying=false;
            EditorApplication.delayCall+=()=>{if(EditorApplication.isPlayingOrWillChangePlaymode)return;int code=SessionState.GetInt(Key+".exit",-1);if(code<0)return;SessionState.EraseInt(Key+".exit");EditorApplication.Exit(code);};
        }
    }
}
