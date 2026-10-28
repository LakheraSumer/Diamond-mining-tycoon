using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
    /// <summary>Checks the imported rig in actual Main Play mode, with a private QA save.</summary>
    public static class CrewVisualReview
    {
        const string Key="DiamondMine.CrewReview";
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string[] Poses={"idle","walk","run","mine","carry","collect","wave","celebrate","machine","craft"};
        static readonly List<string> checks=new List<string>(),errors=new List<string>();
        [Serializable] sealed class EvidenceFile { public string file,sha256,kind,note; }
        [Serializable] sealed class StateSample { public string character_id,state; public bool passed; public EvidenceFile[] outputs; }
        [Serializable] sealed class StateReport { public StateSample[] states; public string method="Actual Unity Generic Animator state sampled at normalized time 0.35 in Main Play mode. Native 400x550 per pose; continuous-motion quality remains a separate review."; }
        [Serializable] sealed class CaptureManifest { public EvidenceFile[] images,inputs; public string unity,method,scene,utc,saveRoot; public string[] checks; }
        static readonly List<StateSample> stateSamples=new List<StateSample>();
        static string output;
        static int stage;
        static double next,inventory,load,deadline,cartDeadline;
        static string item;
        static bool pumping;
        static Vector3 start,cartStart;
        static GameSession session;
        static YardWorld yard;
        static YardControls controls;

        public static void Run()
        {
            double ready=EditorApplication.timeSinceStartup+5;
            EditorApplication.CallbackFunction wait=null;
            wait=()=>{
                if(EditorApplication.timeSinceStartup<ready||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
                EditorApplication.update-=wait;
                try
                {
                    if(Environment.GetEnvironmentVariable("DMT_REVIEW_SKIP_IMPORT")!="1")ReferenceAssetImport.Install();
                    string root=Environment.GetEnvironmentVariable("DIAMOND_QA_EVIDENCE_ROOT");
                    if(string.IsNullOrEmpty(root))root=Directory.GetCurrentDirectory();
                    output=Path.Combine(root,"release/screenshots/characters");Directory.CreateDirectory(output);
                    SessionState.SetString(Key,output);
                    string save=Path.Combine(Directory.GetCurrentDirectory(),".cache/qa-saves/crew-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
                    Directory.CreateDirectory(save);Environment.SetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT",save);
                    PortraitReview.SetSize(720,1280);
                    EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
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
            stage=0;pumping=false;next=EditorApplication.timeSinceStartup+4;deadline=EditorApplication.timeSinceStartup+300;checks.Clear();errors.Clear();stateSamples.Clear();
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
        }
        static void Log(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);}
        static void Require(string text,bool condition)
        {if(!condition)throw new InvalidOperationException("FAILED: "+text);checks.Add("PASS: "+text);}
        static void Tick()
        {
            if(deadline>0&&EditorApplication.timeSinceStartup>deadline){errors.Add("Crew review exceeded five minutes.");Finish();return;}
            if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Crew review exceeded five minutes; inspect session startup and the Unity log.");
                session=UnityEngine.Object.FindAnyObjectByType<GameSession>();
                yard=UnityEngine.Object.FindAnyObjectByType<YardWorld>();controls=UnityEngine.Object.FindAnyObjectByType<YardControls>();
                if(session==null||!session.Ready||yard==null||controls==null)return;
                if(!pumping)
                {
                    Type type=Type.GetType("DiamondMine.Runtime.EditorReviewPump, DiamondMine.Runtime",true);
                    Component pump=session.gameObject.AddComponent(type);type.GetField("TickAction").SetValue(pump,(Action)Tick);
                    EditorApplication.update-=Tick;pumping=true;return;
                }
                switch(stage++)
                {
                    case 0:
                        Require("Actual Main scene Play mode",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.EndsWith("Main.unity",StringComparison.Ordinal));
                        Require("Yard camera excludes legacy Works scenery",(yard.View.cullingMask & (1 << MinePresenter.WorldLayer))==0);
                        Require("Private review save",Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT").Contains("qa-saves"));
                        QualityPreset.Apply(2);QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.antiAliasing=4;
                        yard.SetShown(true);ValidateAndRender();
                        Require("Select Tavi through existing game rule",session.TryChooseCharacter("tavi"));ClosePanel();
                        YardPoint door=YardLayout.Entrance(0);
                        session.State.Adventure.PlayerX=door.X-.83;session.State.Adventure.PlayerZ=door.Z-2.55;
                        start=new Vector3((float)door.X-.83f,0,(float)door.Z-2.55f);
                        // Leave room for the controller's 0.18m arrival tolerance inside interaction range.
                        yard.FrameTimberArea(true);yard.SetMoveTarget((float)door.X-.95f,(float)door.Z-.78f);
                        next=EditorApplication.timeSinceStartup+2.5;return;
                    case 1:
                        Require("Tap-to-move reaches target",Vector3.Distance(start,yard.Player.position)>.9f&&!yard.HasMove);
                        Require("Walk pose ends at destination",session.State.Adventure.PoseHint!="move"&&session.State.Adventure.PoseHint!="run");
                        Require("Destination is inside bunker interaction range",YardLayout.Nearest(session.State.Adventure).PlotId==0);
                        Require("Main player uses skinned Tavi",yard.Player.GetComponent<CrewActor>()!=null&&yard.Player.GetComponent<CrewActor>().CharacterId=="tavi");
                        Require("No primitive MeshFilters in imported player",yard.Player.GetComponentsInChildren<MeshFilter>(true).Length==0);
                        Shot("play-mode-character.png");next=EditorApplication.timeSinceStartup+.6;return;
                    case 2:UiGameplayAssertions.Click(controls,"Action.interact");Require("Interact opens the approached starter bunker",session.State.Adventure.FocusPlotId==0);next=EditorApplication.timeSinceStartup+.7;return;
                    case 3:next=EditorApplication.timeSinceStartup+.2;return;
                    case 4:Click("Mine",false);next=EditorApplication.timeSinceStartup+.15;return;
                    case 5:
                        Require("Mine grants carried ore",session.State.Adventure.HandsAmount>0);
                        Require("Mining tool appears in actual mine gameplay",yard.GetComponentsInChildren<CrewActor>().Any(a=>a.CharacterId=="tavi" && a.GetComponentsInChildren<Renderer>().Any(r=>r.name=="MiningTool"&&r.enabled)));
                        Shot("play-mode-character-mining.png");next=EditorApplication.timeSinceStartup+.6;return;
                    case 6:
                        item=session.State.Adventure.HandsItem;inventory=Inventory(item);load=session.State.Adventure.HandsAmount;
                        Click("Send cart to mill",false);ClosePanel();yard.Present(session);cartStart=yard.TransportCart.position;
                        Require("Cart takes real carried load",load>0&&session.State.Adventure.CartLoad==load&&session.State.Adventure.HandsAmount==0);
                        cartDeadline=EditorApplication.timeSinceStartup+2;
                        next=EditorApplication.timeSinceStartup+.3;return;
                    case 7:
                        yard.Present(session);
                        float moved=Vector3.Distance(cartStart,yard.TransportCart.position);
                        Debug.Log("CART-PLAY active="+yard.TransportCart.gameObject.activeSelf+" seconds="+session.State.Adventure.CartSeconds+" moved="+moved+" screen="+controls.CurrentReferenceScreen+" granted="+session.State.Adventure.CartGranted);
                        if(moved<=.1f&&session.State.Adventure.CartSeconds>0&&EditorApplication.timeSinceStartup<cartDeadline)
                        {stage=7;next=EditorApplication.timeSinceStartup+.15;return;}
                        Require("Authored cart still travels (distance="+moved.ToString("F3")+", active="+yard.TransportCart.gameObject.activeSelf+")",yard.TransportCart.gameObject.activeSelf&&moved>.1f);
                        Shot("play-mode-character-cart.png");next=EditorApplication.timeSinceStartup+JourneyCatalog.CartTripSeconds+1;return;
                    case 8:
                        Require("Cart delivers exact quantity",Math.Abs(Inventory(item)-inventory-load)<.001);
                        File.WriteAllText(Path.Combine(output,"review-context.txt"),"Unity "+Application.unityVersion+"; Main.unity; actual Play mode.\n"+
                            "Automated existing movement target and Unity UI pointer events. Not a physical device test.\nSave: "+Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT")+
                            "\nCart item: "+item+"; quantity: "+load+"\nCharacter views are Unity camera renders of the imported FBX, captured during Play mode.\n"+
                            "GPU: "+SystemInfo.graphicsDeviceName+"; API: "+SystemInfo.graphicsDeviceType+"; memory MB: "+SystemInfo.graphicsMemorySize+"\n"+
                            "Desktop evidence only; no physical Android or iOS frame-time certification.\n");
                        session.State.Adventure.PoseHint="";yard.Facing=145;yard.Present(session);
                        next=EditorApplication.timeSinceStartup+.5;return;
                    case 9:
                        RenderTimber();
                        Shot("play-mode-character-front.png");
                        next=EditorApplication.timeSinceStartup+.5;return;
                    case 10:
                        UiGameplayAssertions.Run(session,controls,Require);
                        File.AppendAllText(Path.Combine(output,"review-context.txt"),"\nAdditional UI callback assertions use four diamond ore, two silver ore and task-progress fixtures in this private save only. Machine/job time is advanced through the production rules. Live screenshots use a separate fresh save.\n");
                        next=EditorApplication.timeSinceStartup+.5;return;
                    default:Finish();return;
                }
            }
            catch(Exception ex){errors.Add(ex.ToString());Finish();}
        }

        static void ValidateAndRender()
        {
            string ids=Environment.GetEnvironmentVariable("DMT_CHARACTER_IDS");
            string[] wanted=string.IsNullOrEmpty(ids)?JourneyCatalog.AllCharacters.Select(c=>c.Id).ToArray():ids.Split(',');
            Vector3 studio=new Vector3(100,0,100);
            var cameraObject=new GameObject("Crew evidence camera");Camera camera=cameraObject.AddComponent<Camera>();
            camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.88f,.85f,.79f);
            camera.cullingMask=1<<30;camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=20;
            Light sun=yard.GetComponentsInChildren<Light>().First(l=>l.type==LightType.Directional);
            int sunMask=sun.cullingMask;sun.cullingMask&=~(1<<30);
            var lights=new List<GameObject>();
            foreach(var setup in new[]{new {Rotation=new Vector3(35,210,0),Power=.75f,Color=new Color(1,.91f,.79f)},new {Rotation=new Vector3(20,125,0),Power=.30f,Color=new Color(.78f,.87f,1)}})
            {
                var lightObject=new GameObject("Crew studio light");lights.Add(lightObject);
                Light light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<30;
                light.color=setup.Color;light.intensity=setup.Power;light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(setup.Rotation);
            }
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="Review studio floor (not game asset)";floor.layer=30;
            floor.transform.position=studio;floor.transform.localScale=new Vector3(2,1,2);
            floor.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Standard")){color=new Color(.76f,.72f,.64f)};
            foreach(string id in wanted)
            {
                // Unlock only in this private QA save; production unlock rules stay unchanged.
                if(!session.State.Adventure.UnlockedCharacters.Contains(id))session.State.Adventure.UnlockedCharacters.Add(id);
                Require(id+" selection through existing game rule",session.TryChooseCharacter(id));
                yard.Present(session);
                Require(id+" instantiated as Main player",yard.Player.GetComponent<CrewActor>().CharacterId==id);
                GameObject prefab=Resources.Load<GameObject>("Characters/"+id);
                Require(id+" prefab exists",prefab!=null);
                GameObject go=UnityEngine.Object.Instantiate(prefab);go.transform.position=studio;
                try
                {
                    foreach(Transform t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                    CrewActor actor=go.GetComponent<CrewActor>();Animator animator=actor.Animator;
                    Require(id+" valid Generic avatar",animator.avatar!=null&&animator.avatar.isValid&&!animator.avatar.isHuman);
                    SkinnedMeshRenderer[] skins=go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    Require(id+" four equipment/body meshes",skins.Length==4);
                    foreach(SkinnedMeshRenderer skin in skins)
                    {
                        Require(id+" "+skin.name+" rig/UV/bind poses",skin.bones.Length>0&&skin.sharedMesh.bindposes.Length>0&&skin.sharedMesh.HasVertexAttribute(VertexAttribute.TexCoord0));
                        Require(id+" "+skin.name+" persistent materials",skin.sharedMaterials.All(m=>m!=null&&AssetDatabase.GetAssetPath(m).StartsWith("Assets/_Project/Art/Crew/Materials/",StringComparison.Ordinal)));
                    }
                    Transform thigh=go.GetComponentsInChildren<Transform>().First(t=>t.name=="Thigh_L");
                    animator.Play("Base Layer.Walk",0,.20f);animator.Update(0);Quaternion first=thigh.localRotation;
                    animator.Play("Base Layer.Walk",0,.70f);animator.Update(0);
                    Require(id+" walk actually moves leg bone",Quaternion.Angle(first,thigh.localRotation)>15);
                    animator.Play("Base Layer.Mine",0,.05f);animator.Update(0);first=actor.Arm.localRotation;
                    animator.Play("Base Layer.Mine",0,.50f);animator.Update(0);
                    Require(id+" mine actually moves arm bone",Quaternion.Angle(first,actor.Arm.localRotation)>35);
                    foreach(string pose in Poses)
                    {
                        string clip=char.ToUpperInvariant(pose[0])+pose.Substring(1);
                        Require(id+" animator state "+clip,animator.HasState(0,Animator.StringToHash("Base Layer."+clip)));
                        AnimationClip source=animator.runtimeAnimatorController.animationClips.First(c=>c.name==clip);
                        Require(id+" animation curves "+clip,AnimationUtility.GetCurveBindings(source).Length>20);
                        actor.Present(pose,false);animator.Update(.2f);
                    }
                    Require(id+" articulated fingers imported",go.GetComponentsInChildren<Transform>(true).Count(t=>new[]{"Thumb","Index","Middle","Ring","Pinky"}.Any(n=>t.name.StartsWith(n,StringComparison.Ordinal)))>=30);
                    actor.Present("mine",true);animator.Update(0);first=actor.Arm.localRotation;animator.Update(.5f);
                    Require(id+" reduced motion freezes rig",animator.speed==0&&Quaternion.Angle(first,actor.Arm.localRotation)<.01f);
                    actor.Present("carry",true);
                    Require(id+" carried ore visible",skins.First(r=>r.name=="CarryLoad").enabled&&!skins.First(r=>r.name=="RoleProp").enabled);
                    actor.Present("idle",false);animator.Play("Base Layer.Idle",0,.1f);animator.Update(0);
                    Bounds bounds=skins.First(r=>r.name=="Body").bounds;
                    Debug.Log("CREW-PLAY-BOUNDS "+id+" "+bounds.size.ToString("F4"));
                    Require(id+" imported upright metre scale",bounds.size.y>1.8f&&bounds.size.y<2.5f&&bounds.size.y>bounds.size.x&&bounds.size.y>bounds.size.z);
                    float scale=bounds.size.y/2;
                    camera.aspect=800f/1100;camera.orthographicSize=1.16f*scale;
                    camera.transform.position=studio+new Vector3(-2.5f,1.65f,5.8f)*scale;camera.transform.LookAt(studio+Vector3.up*scale);
                    SaveCamera(camera,id+"-unity.png",800,1100);
                    camera.aspect=1;camera.orthographicSize=.39f*scale;
                    camera.transform.position=studio+new Vector3(-.30f,1.72f,3)*scale;camera.transform.LookAt(studio+new Vector3(0,1.695f,.015f)*scale);
                    SaveCamera(camera,id+"-face-unity.png",800,800);
                    if(id=="tavi")
                    {
                        camera.aspect=800f/1100;camera.orthographicSize=1.14f;
                        foreach(var view in new[]{new {Name="side",Offset=new Vector3(5,1.4f,0)},new {Name="back",Offset=new Vector3(0,1.4f,-5)}})
                        {camera.transform.position=studio+view.Offset;camera.transform.LookAt(studio+Vector3.up);SaveCamera(camera,id+"-"+view.Name+"-unity.png",800,1100);}
                    }
                    camera.aspect=400f/550;camera.orthographicSize=1.18f*scale;
                    camera.transform.position=studio+new Vector3(-2.5f,1.65f,5.8f)*scale;camera.transform.LookAt(studio+Vector3.up*scale);
                    Directory.CreateDirectory(Path.Combine(output,"poses"));
                    foreach(string p in Poses)
                    {
                        string clip=char.ToUpperInvariant(p[0])+p.Substring(1);
                        actor.Present(p,true);animator.Play("Base Layer."+clip,0,.35f);animator.Update(0);
                        string name="poses/"+id+"-"+p+".png";SaveCamera(camera,name,400,550);
                        stateSamples.Add(new StateSample{character_id=id,state=clip,passed=true,outputs=new[]{Evidence(Path.Combine(output,name),"Unity pose sample")}});
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(go);}
            }
            Require("World NPCs use authored rigs",yard.GetComponentsInChildren<CrewActor>(true).Any(c=>c.CharacterId=="pavi")&&yard.GetComponentsInChildren<CrewActor>(true).Any(c=>c.CharacterId=="sola"));
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Renderer>().sharedMaterial);
            UnityEngine.Object.DestroyImmediate(floor);UnityEngine.Object.DestroyImmediate(cameraObject);
            foreach(GameObject light in lights)UnityEngine.Object.DestroyImmediate(light);
            sun.cullingMask=sunMask;
        }
        static void SaveCamera(Camera camera,string name,int width,int height)
        {
            var target=new RenderTexture(width,height,24){antiAliasing=4};camera.targetTexture=target;camera.Render();
            RenderTexture.active=target;var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
        }
        static void RenderTimber()
        {
            Camera camera=yard.View;int mask=camera.cullingMask;Color background=camera.backgroundColor;
            float aspect=camera.aspect,size=camera.orthographicSize;bool ortho=camera.orthographic;
            Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;
            var layers=new Dictionary<GameObject,int>();
            foreach(Transform child in yard.TimberArea.GetComponentsInChildren<Transform>(true)){layers.Add(child.gameObject,child.gameObject.layer);child.gameObject.layer=30;}
            try
            {
                camera.cullingMask=1<<30;camera.backgroundColor=new Color(.86f,.84f,.77f);
                camera.aspect=1000f/1120;camera.orthographic=true;camera.orthographicSize=3.12f;
                YardPoint door=YardLayout.Entrance(0);Vector3 focus=new Vector3((float)door.X,1.23f,(float)door.Z-.35f);
                camera.transform.position=focus+new Vector3(4.8f,3.7f,-7.2f);camera.transform.LookAt(focus);
                SaveCamera(camera,"timber-closeup-unity.png",1000,1120);
                File.WriteAllText(Path.Combine(output,"timber-closeup-unity.txt"),"Actual imported hero area during Main Play mode; native Unity camera render 1000x1120; orthographic size3.12, offset(4.8,3.7,-7.2), entrance+(0,1.23,-.35). Reference framing is approximate, not recovered calibration. No pixel identity claim.\n");
            }
            finally
            {
                foreach(var row in layers)row.Key.layer=row.Value;
                camera.cullingMask=mask;camera.backgroundColor=background;camera.aspect=aspect;camera.orthographic=ortho;camera.orthographicSize=size;camera.transform.SetPositionAndRotation(position,rotation);
            }
        }
        static void Shot(string name){Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(output,name));}
        static double Inventory(string key){return session.State.Adventure.Inventory.TryGetValue(key,out double value)?value:0;}
        static Transform Panel(){return controls.GetComponentsInChildren<RectTransform>(true).First(t=>t.name=="YardPanel");}
        static void ClosePanel(){typeof(YardControls).GetMethod("ClosePanel",Hidden).Invoke(controls,null);}
        static void Click(string caption,bool panelOnly)
        {
            IEnumerable<Button> candidates=controls.GetComponentsInChildren<Button>().Where(b=>b.gameObject.activeInHierarchy&&b.interactable);
            if(panelOnly)candidates=candidates.Where(b=>b.transform.IsChildOf(Panel()));
            string action=caption=="Mine"?"Action.Mine":caption=="Send cart to mill"?"Action.SendCart":"";
            Button button=action.Length>0?candidates.FirstOrDefault(b=>b.name==action):null;
            if(button==null)button=candidates.FirstOrDefault(b=>UiFactory.Caption(b).text==caption);
            if(button==null)throw new InvalidOperationException("Missing active button "+caption);
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        static void Finish()
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
            File.WriteAllLines(Path.Combine(output,"review-errors.txt"),errors);File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);
            File.WriteAllText(Path.Combine(output,"state-review.json"),JsonUtility.ToJson(new StateReport{states=stateSamples.ToArray()},true));
            if(errors.Count==0)
            {
                var inputFiles=new List<string>();
                foreach(string folder in new[]{"Assets/_Project","Packages","ProjectSettings"})
                    inputFiles.AddRange(Directory.GetFiles(folder,"*",SearchOption.AllDirectories));
                inputFiles.Add("Assets/_Project.meta");
                string evidenceRoot=Environment.GetEnvironmentVariable("DIAMOND_QA_EVIDENCE_ROOT")??Directory.GetCurrentDirectory();
                inputFiles.AddRange(Directory.GetFiles(Path.Combine(evidenceRoot,"ArtSource/Characters"),"*.blend"));
                var captures=Directory.GetFiles(output,"*-unity.png").Where(f=>!Path.GetFileName(f).Contains("reference-vs")).Concat(Directory.GetFiles(output,"play-mode-*.png")).Concat(Directory.GetFiles(Path.Combine(output,"poses"),"*.png"));
                File.WriteAllText(Path.Combine(output,"evidence-manifest.json"),JsonUtility.ToJson(new CaptureManifest{unity=Application.unityVersion,scene="Assets/_Project/Scenes/Main.unity",utc=DateTime.UtcNow.ToString("O"),saveRoot=Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT"),method="Actual Main Play-mode ScreenCapture and framed Unity camera renders; isolated QA save; likeness remains unaccepted.",checks=checks.ToArray(),images=captures.Select(f=>Evidence(f,"Current Unity Play-mode capture")).ToArray(),inputs=inputFiles.Select(f=>Evidence(f,"Capture input")).ToArray()},true));
            }
            Debug.Log("Crew Play review: "+checks.Count+" passed checks; "+errors.Count+" errors.");
            SessionState.EraseString(Key);SessionState.SetInt(Key+".exit",errors.Count==0?0:1);EditorApplication.isPlaying=false;
            EditorApplication.delayCall+=()=>{if(EditorApplication.isPlayingOrWillChangePlaymode)return;int code=SessionState.GetInt(Key+".exit",-1);if(code<0)return;SessionState.EraseInt(Key+".exit");EditorApplication.Exit(code);};
        }

        static EvidenceFile Evidence(string file,string kind)
        {
            string full=Path.GetFullPath(file),basePath=Path.GetFullPath(Environment.GetEnvironmentVariable("DIAMOND_QA_EVIDENCE_ROOT")??Directory.GetCurrentDirectory());
            string current=Path.GetFullPath(Directory.GetCurrentDirectory());
            // Workspace paths must resolve to their project-relative asset names.
            string relative=full.StartsWith(current+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)?full.Substring(current.Length+1):full.StartsWith(basePath+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)?full.Substring(basePath.Length+1):file;
            using(var hash=SHA256.Create())using(var stream=File.OpenRead(full))return new EvidenceFile{file=relative.Replace('\\','/'),sha256=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant(),kind=kind,note="Imported reconstruction; illustration likeness remains unaccepted."};
        }
    }
}
