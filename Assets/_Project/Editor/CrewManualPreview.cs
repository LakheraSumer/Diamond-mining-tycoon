using System;
using DiamondMine.Runtime;
using DiamondMine.Simulation;
using UnityEditor;
using UnityEngine;

namespace DiamondMine.Editor
{
    public sealed class CrewManualPreview : EditorWindow
    {
        bool prepared;

        [MenuItem("Diamond Mine/Characters/Play crew with isolated save")]
        static void Play()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before starting the isolated crew preview.");
            if(Resources.Load<GameObject>("Characters/daro")==null)CrewAssetPipeline.Install();
            GetWindow<CrewManualPreview>("Crew preview").prepared=false;
            TimberManualPreview.Play();
        }
        void Update()
        {
            if(!EditorApplication.isPlaying){prepared=false;return;}
            string save=Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT")??"";
            if(!save.Contains("qa-saves")||!save.Contains("timber-manual-"))return;
            GameSession session=UnityEngine.Object.FindAnyObjectByType<GameSession>();
            if(session==null||!session.Ready)return;
            if(!prepared)
            {
                foreach(CharacterDef person in JourneyCatalog.AllCharacters)
                    if(!session.State.Adventure.UnlockedCharacters.Contains(person.Id))session.State.Adventure.UnlockedCharacters.Add(person.Id);
                prepared=true;
            }
            Repaint();
        }
        void OnGUI()
        {
            EditorGUILayout.LabelField("All 12 crew in a separate test save",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use the Game view to move, mine and send the cart. Stop Play to leave this preview. Your normal save and unlocks are preserved.",MessageType.Info);
            if(!prepared){EditorGUILayout.LabelField("Start from Diamond Mine > Characters > Play crew with isolated save.");return;}
            GameSession session=UnityEngine.Object.FindAnyObjectByType<GameSession>();
            YardWorld world=UnityEngine.Object.FindAnyObjectByType<YardWorld>();
            if(session==null||world==null)return;
            foreach(CharacterDef person in JourneyCatalog.AllCharacters)
                if(GUILayout.Button(person.Name))session.TryChooseCharacter(person.Id);
            if(GUILayout.Button("Turn toward camera")){world.Facing=145;world.Present(session);}
        }
    }
}
