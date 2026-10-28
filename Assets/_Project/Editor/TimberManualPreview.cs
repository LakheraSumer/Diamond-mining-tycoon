using System;
using System.IO;
using System.Reflection;
using DiamondMine.Runtime;
using DiamondMine.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiamondMine.Editor
{
    public static class TimberManualPreview
    {
        const string Key="DiamondMine.TimberManualPreview";

        [MenuItem("Diamond Mine/Art/Play bunker 1 with isolated save")]
        public static void Play()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before starting an isolated art preview.");
            if(Resources.Load<GameObject>(TimberBunkerArea.AreaResource)==null) TimberAssetPipeline.Install();
            SessionState.SetString(Key+".previousSave",Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT")??"");
            string save=Path.Combine(Directory.GetCurrentDirectory(),".cache","qa-saves","timber-manual-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(save);
            Environment.SetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT",save);
            SessionState.SetBool(Key,true);
            PortraitReview.SetSize(720,1280);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            EditorApplication.isPlaying=true;
            Boot();
        }

        [InitializeOnLoadMethod]
        static void Boot()
        {
            if(!SessionState.GetBool(Key,false))return;
            EditorApplication.update-=Prepare;EditorApplication.update+=Prepare;
            EditorApplication.playModeStateChanged-=StateChanged;EditorApplication.playModeStateChanged+=StateChanged;
        }

        static void Prepare()
        {
            if(!EditorApplication.isPlaying)return;
            GameSession session=UnityEngine.Object.FindAnyObjectByType<GameSession>();
            YardWorld yard=UnityEngine.Object.FindAnyObjectByType<YardWorld>();
            YardControls controls=UnityEngine.Object.FindAnyObjectByType<YardControls>();
            if(session==null || !session.Ready || yard==null || controls==null)return;
            session.TryChooseCharacter("tavi");
            typeof(YardControls).GetMethod("ClosePanel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controls,null);
            YardPoint door=YardLayout.Entrance(0);
            session.State.Adventure.PlayerX=door.X-.83;
            session.State.Adventure.PlayerZ=door.Z-1.16;
            yard.SetShown(true);
            Debug.Log("Bunker 1 preview uses isolated save: "+Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT"));
            EditorApplication.update-=Prepare;
        }

        static void StateChanged(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredEditMode)return;
            string previous=SessionState.GetString(Key+".previousSave","");
            Environment.SetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT",previous.Length==0?null:previous);
            SessionState.EraseString(Key+".previousSave");SessionState.EraseBool(Key);
            EditorApplication.update-=Prepare;EditorApplication.playModeStateChanged-=StateChanged;
        }
    }
}
