using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DiamondMine.Runtime;
using DiamondMine.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace DiamondMine.Editor
{
    /// <summary>Real Main/Play-mode art and interaction evidence, using an isolated save.</summary>
    public static class TimberVisualReview
    {
        const string Key="DiamondMine.TimberReview";
        static int stage;
        static double next;
        static bool pumping;
        static string output;
        static GameSession session;
        static YardWorld yard;
        static YardControls controls;
        static Vector3 movementStart,cartStart;
        static double initialInventory,load;
        static string item;
        static readonly List<string> checks=new List<string>();
        static readonly List<string> errors=new List<string>();
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;

        public static void Run()
        {
            double ready=EditorApplication.timeSinceStartup+8;
            EditorApplication.CallbackFunction wait=null;
            wait=()=>{
                if(EditorApplication.timeSinceStartup<ready || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                EditorApplication.update-=wait;
                try
                {
                    TimberAssetPipeline.Install();
                    string root=Environment.GetEnvironmentVariable("DIAMOND_QA_EVIDENCE_ROOT");
                    if(string.IsNullOrEmpty(root)) root=Directory.GetCurrentDirectory();
                    string destination=Path.Combine(root,"release","screenshots","timber-bunker");
                    Directory.CreateDirectory(destination);
                    SessionState.SetString(Key,destination);
                    string save=Path.Combine(Directory.GetCurrentDirectory(),".cache","qa-saves","timber-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
                    Directory.CreateDirectory(save);
                    Environment.SetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT",save);
                    PortraitReview.SetSize(720,1280);
                    EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
                    EditorApplication.isPlaying=true;
                    Boot();
                }
                catch(Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
            };
            EditorApplication.update+=wait;
        }

        [InitializeOnLoadMethod]
        static void Boot()
        {
            int exit=SessionState.GetInt(Key+".exit",-1);
            if(exit>=0)
            {
                EditorApplication.delayCall+=()=>{SessionState.EraseInt(Key+".exit");EditorApplication.Exit(exit);};
                return;
            }
            output=SessionState.GetString(Key,"");
            if(output.Length==0) return;
            stage=0; pumping=false; next=EditorApplication.timeSinceStartup+4;
            checks.Clear();errors.Clear();
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
        }

        static void Log(string text,string stack,LogType kind)
        { if(kind==LogType.Exception || kind==LogType.Error || kind==LogType.Assert) errors.Add(text+"\n"+stack); }

        public static void Tick()
        {
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup<next) return;
            try
            {
                session=UnityEngine.Object.FindAnyObjectByType<GameSession>();
                yard=UnityEngine.Object.FindAnyObjectByType<YardWorld>();
                controls=UnityEngine.Object.FindAnyObjectByType<YardControls>();
                if(session==null || !session.Ready || yard==null || controls==null) return;
                if(!pumping)
                {
                    Type type=Type.GetType("DiamondMine.Runtime.EditorReviewPump, DiamondMine.Runtime",true);
                    Component pump=session.gameObject.AddComponent(type);
                    type.GetField("TickAction").SetValue(pump,(Action)Tick);
                    EditorApplication.update-=Tick;pumping=true;return;
                }
                switch(stage++)
                {
                    case 0:
                        Require("Main scene in Play mode",EditorApplication.isPlaying && UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.EndsWith("Main.unity",StringComparison.Ordinal));
                        Require("Save isolated",Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT").Contains("qa-saves"));
                        Require("Imported bunker prefab instantiated",yard.TimberArea!=null);
                        Require("Metal reflection environment is authored",yard.TimberArea.GetComponentsInChildren<ReflectionProbe>()
                            .Any(p=>p.mode==ReflectionProbeMode.Custom && p.customBakedTexture!=null));
                        Require("Imported cart prefab instantiated",yard.TransportCart!=null);
                        Transform[] wheels=yard.TransportCart.GetComponentsInChildren<Transform>(true)
                            .Where(t=>t.name.StartsWith("Wheel_",StringComparison.Ordinal)).ToArray();
                        Require("Four authored wheels retain axle pivots",wheels.Length==4 &&
                            wheels.All(w=>Mathf.Abs(yard.TransportCart.InverseTransformPoint(w.position).y-.335f)<.01f));
                        foreach(MeshFilter filter in yard.TimberArea.GetComponentsInChildren<MeshFilter>())
                        {
                            Mesh mesh=filter.sharedMesh;
                            if(!AssetDatabase.GetAssetPath(mesh).StartsWith("Assets/_Project/Art/TimberBunker/Models/",StringComparison.Ordinal))
                                throw new InvalidOperationException("Runtime primitive inside authored area: "+filter.name);
                            if(!mesh.HasVertexAttribute(VertexAttribute.TexCoord0) || !mesh.HasVertexAttribute(VertexAttribute.Color))
                                throw new InvalidOperationException("Missing exported UV or contact AO: "+filter.name);
                        }
                        Require("Authored area uses FBX meshes with UV0 and vertex AO",true);
                        Renderer glow=yard.TimberArea.GetComponentsInChildren<Renderer>().First(r=>r.name=="TimberBunker_LanternGlow");
                        Require("FBX portal faces the playable path",(glow.bounds.center-yard.TimberArea.transform.position).z<-.30f);
                        Require("Character selection",session.TryChooseCharacter("tavi"));
                        ClosePanel();
                        QualityPreset.Apply(2);QualitySettings.shadowResolution=ShadowResolution.High;
                        QualitySettings.shadowDistance=24;QualitySettings.antiAliasing=4;
                        yard.SetShown(true);
                        YardPoint door=YardLayout.Entrance(0);
                        session.State.Adventure.PlayerX=door.X-.83;
                        session.State.Adventure.PlayerZ=door.Z-2.55;
                        movementStart=new Vector3((float)door.X-.83f,0,(float)door.Z-2.55f);
                        yard.FrameTimberArea(true);
                        yard.SetMoveTarget((float)door.X-.83f,(float)door.Z-1.16f);
                        next=EditorApplication.timeSinceStartup+2.5;return;
                    case 1:
                        Require("Tap-to-move controls move player",Vector3.Distance(movementStart,yard.Player.position)>.9f && !yard.HasMove);
                        Bounds walker=new Bounds(yard.Player.position,Vector3.zero);
                        foreach(Renderer r in yard.Player.GetComponentsInChildren<Renderer>())walker.Encapsulate(r.bounds);
                        Require("Walking actor retains full height",walker.size.y>1.6f);
                        Require("Bunker interaction radius unchanged",YardLayout.Nearest(session.State.Adventure).PlotId==0);
                        Shot("play-mode-main.png");
                        next=EditorApplication.timeSinceStartup+.6;return;
                    case 2:
                        Click("Bunker 1",false);
                        next=EditorApplication.timeSinceStartup+.7;return;
                    case 3:
                        Require("Interact opens bunker 1",session.State.Adventure.FocusPlotId==0 && Panel().gameObject.activeInHierarchy);
                        Panel().GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;
                        next=EditorApplication.timeSinceStartup+.7;return;
                    case 4:
                        Click("Mine",true);
                        next=EditorApplication.timeSinceStartup+.7;return;
                    case 5:
                        Require("Mine button grants real carried ore",session.State.Adventure.HandsAmount>0);
                        Panel().GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;
                        Shot("play-mode-mine-interaction.png");
                        next=EditorApplication.timeSinceStartup+.6;return;
                    case 6:
                        item=session.State.Adventure.HandsItem;
                        initialInventory=Inventory(item);
                        load=session.State.Adventure.HandsAmount;
                        Click("Send cart to mill",true);
                        Require("Cart UI starts transaction",session.State.Adventure.CartLoad==load && session.State.Adventure.HandsAmount==0);
                        ClosePanel();
                        yard.Present(session);
                        cartStart=yard.TransportCart.position;
                        next=EditorApplication.timeSinceStartup+.25;return;
                    case 7:
                        Require("Imported cart moves along haul route",yard.TransportCart.gameObject.activeSelf && Vector3.Distance(cartStart,yard.TransportCart.position)>.1f);
                        Require("Parked cart hidden during haul",!yard.TimberArea.ParkedCart.gameObject.activeSelf);
                        Shot("play-mode-cart-moving.png");
                        next=EditorApplication.timeSinceStartup+JourneyCatalog.CartTripSeconds+1;return;
                    case 8:
                        Require("Cart delivers exact load to warehouse",Math.Abs(Inventory(item)-initialInventory-load)<.001);
                        Require("Moving cart stops after delivery",!yard.TransportCart.gameObject.activeSelf);
                        Require("Parked cart returns",yard.TimberArea.ParkedCart.gameObject.activeSelf);
                        RenderCloseup();
                        Compare();
                        File.WriteAllText(Path.Combine(output,"interaction-results.txt"),
                            "Unity "+Application.unityVersion+"\nScene: Main.unity\nActual Play mode: true\n"+
                            "Input: synthetic Unity UI pointer events and existing tap-to-move control; not a physical device test.\n"+
                            "Save: "+Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT")+"\n"+
                            "Cart item: "+item+"; carried/delivered: "+load+"\n"+string.Join("\n",checks));
                        next=EditorApplication.timeSinceStartup+2;return;
                    default: Finish();return;
                }
            }
            catch(Exception ex) { errors.Add(ex.ToString());Finish(); }
        }

        static double Inventory(string key)
        { return session.State.Adventure.Inventory.TryGetValue(key,out double value)?value:0; }
        static Transform Panel()
        { return controls.GetComponentsInChildren<RectTransform>(true).First(t=>t.name=="YardPanel"); }
        static void ClosePanel() { typeof(YardControls).GetMethod("ClosePanel",Hidden).Invoke(controls,null); }
        static void Click(string caption,bool panelOnly)
        {
            IEnumerable<Button> candidates=controls.GetComponentsInChildren<Button>().Where(b=>b.gameObject.activeInHierarchy && b.interactable);
            if(panelOnly) candidates=candidates.Where(b=>b.transform.IsChildOf(Panel()));
            Button button=candidates.FirstOrDefault(b=>UiFactory.Caption(b).text==caption);
            if(button==null) throw new InvalidOperationException("Active UI button missing: "+caption);
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        static void Require(string text,bool passed)
        { if(!passed) throw new InvalidOperationException("FAILED: "+text);checks.Add("PASS: "+text); }
        static void Shot(string name)
        {
            Canvas.ForceUpdateCanvases();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,name));
            File.WriteAllText(Path.Combine(output,Path.ChangeExtension(name,"txt")),
                "Actual Main Play-mode ScreenCapture; "+Screen.width+"x"+Screen.height+"; frame "+Time.frameCount+
                "; UTC "+DateTime.UtcNow.ToString("O")+"; no image generation/compositing.\n");
        }
        static void RenderCloseup()
        {
            Camera camera=yard.View;
            int oldMask=camera.cullingMask;
            Color oldBackground=camera.backgroundColor;
            float oldAspect=camera.aspect;
            var layers=new Dictionary<GameObject,int>();
            foreach(Transform t in yard.TimberArea.GetComponentsInChildren<Transform>(true)) { layers.Add(t.gameObject,t.gameObject.layer);t.gameObject.layer=29; }
            camera.cullingMask=1<<29;
            camera.backgroundColor=new Color(.86f,.84f,.77f);
            camera.aspect=1000f/1120f;
            camera.orthographic=true;camera.orthographicSize=3.12f;
            YardPoint door=YardLayout.Entrance(0);
            Vector3 target=new Vector3((float)door.X,1.23f,(float)door.Z-.35f);
            camera.transform.position=target+new Vector3(4.8f,3.7f,-7.2f);
            camera.transform.LookAt(target);
            SaveCamera(camera,Path.Combine(output,"unity-closeup.png"),1000,1120);
            foreach(var entry in layers) entry.Key.layer=entry.Value;
            camera.cullingMask=oldMask;camera.backgroundColor=oldBackground;camera.aspect=oldAspect;
            File.WriteAllText(Path.Combine(output,"unity-closeup.txt"),
                "Unity camera render while Main is playing. Same instantiated FBX meshes/materials as main screenshot.\n"+
                "Camera: orthographic, size 3.12, aspect 1000/1120, offset (4.8,3.7,-7.2), target entrance+(0,1.23,-0.35).\n"+
                "Only authored area rendered for crop comparison; UI/other unfinished plots excluded by camera mask.\n");
        }
        static void SaveCamera(Camera camera,string path,int width,int height)
        {
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.antiAliasing=4;
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;
            UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
        }
        static void Compare()
        {
            string root=Environment.GetEnvironmentVariable("DIAMOND_QA_EVIDENCE_ROOT");
            if(string.IsNullOrEmpty(root))root=Directory.GetCurrentDirectory();
            var reference=new Texture2D(2,2);reference.LoadImage(File.ReadAllBytes(Path.Combine(root,"Docs/design-boards/yard-board.jpg")));
            // Original user board: one top-row mine and cart, no unrelated factory crop.
            var crop=new Texture2D(120,133);crop.SetPixels(reference.GetPixels(10,reference.height-111-133,120,133));crop.Apply();
            var actual=new Texture2D(2,2);actual.LoadImage(File.ReadAllBytes(Path.Combine(output,"unity-closeup.png")));
            var board=new Texture2D(2024,1120,TextureFormat.RGB24,false);
            for(int y=0;y<1120;y++)for(int x=0;x<2024;x++)
            {
                Color pixel=x<1000?crop.GetPixelBilinear((x+.5f)/1000,(y+.5f)/1120):
                    x>=1024?actual.GetPixel(x-1024,y):new Color(.12f,.10f,.08f);
                board.SetPixel(x,y,pixel);
            }
            board.Apply();File.WriteAllBytes(Path.Combine(output,"reference-vs-unity.png"),board.EncodeToPNG());
            File.WriteAllText(Path.Combine(output,"comparison.txt"),
                "LEFT: supplied yard-board.jpg, mine 1 crop x=10,y=111,width=120,height=133 (top-left coordinates), enlarged.\n"+
                "RIGHT: Unity Play-mode camera render. Same camera angle family and single-entrance composition; not a pixel-identical reproduction.\n");
            foreach(var t in new[]{reference,crop,actual,board})UnityEngine.Object.DestroyImmediate(t);
        }
        static void Finish()
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
            File.WriteAllLines(Path.Combine(output,"review-errors.txt"),errors);
            File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);
            Debug.Log("Timber Play review: "+checks.Count+" passed checks; "+errors.Count+" errors.");
            SessionState.EraseString(Key);
            SessionState.SetInt(Key+".exit",errors.Count==0?0:1);
            EditorApplication.isPlaying=false;
            EditorApplication.delayCall+=()=>{
                if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                int code=SessionState.GetInt(Key+".exit",-1);
                if(code<0)return;SessionState.EraseInt(Key+".exit");EditorApplication.Exit(code);
            };
        }
    }
}
