using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiamondMine.Runtime;
using DiamondMine.Simulation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiamondMine.Editor
{
    public sealed class CrewModelImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/_Project/Art/Crew/Models/",StringComparison.Ordinal)) return;
            ModelImporter importer=(ModelImporter)assetImporter;
            importer.globalScale=1;
            importer.bakeAxisConversion=true;
            importer.importAnimation=true;
            importer.animationType=ModelImporterAnimationType.Generic;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects=false;
            importer.importCameras=false;importer.importLights=false;
            importer.importNormals=ModelImporterNormals.Import;
            importer.importTangents=ModelImporterTangents.CalculateMikk;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.isReadable=false;
        }
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/_Project/Art/Crew/Textures/",StringComparison.Ordinal) || !assetPath.EndsWith(".png",StringComparison.Ordinal)) return;
            TextureImporter importer=(TextureImporter)assetImporter;
            importer.textureType=assetPath.Contains("_Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=importer.textureType!=TextureImporterType.NormalMap;
            importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;
        }
    }

    public static class CrewAssetPipeline
    {
        const string Art="Assets/_Project/Art/Crew/";
        const string Prefabs="Assets/_Project/Resources/Characters/";
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static readonly string[] Clips={"Idle","Walk","Run","Mine","Collect","Carry","Wave","Celebrate","Machine","Craft"};

        [MenuItem("Diamond Mine/Characters/Import modelled crew")]
        public static void Install()
        {
            Directory.CreateDirectory(Art+"Materials");Directory.CreateDirectory(Art+"Animation");Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            JsonValue spec=JsonValue.Parse(File.ReadAllText(Art+"material-spec.json"));
            materials.Clear();
            Shader shader=Shader.Find("DiamondMine/TimberSurface");
            if(shader==null)throw new InvalidOperationException("Character surface shader is missing.");
            foreach(var entry in spec.AsObject())
            {
                JsonValue value=entry.Value;var rgb=value.Array("color");
                float alpha=(float)value.Number("alpha",1);
                Color tint=new Color((float)rgb[0].AsNumber(),(float)rgb[1].AsNumber(),(float)rgb[2].AsNumber(),alpha);
                Shader surface=alpha<1?Shader.Find("DiamondMine/CrewLens"):shader;
                if(surface==null)throw new InvalidOperationException("Character lens shader is missing.");
                string path=Art+"Materials/"+entry.Key+".mat";
                Material mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(surface);AssetDatabase.CreateAsset(mat,path);}
                mat.shader=surface;mat.name=entry.Key;mat.color=tint;
                mat.SetFloat("_Glossiness",1-(float)value.Number("roughness",.6));
                mat.SetFloat("_Metallic",(float)value.Number("metallic",0));
                if(mat.HasProperty("_BumpScale"))mat.SetFloat("_BumpScale",.35f);
                if(mat.HasProperty("_EmissionColor"))mat.SetColor("_EmissionColor",tint*(float)value.Number("emission",0));
                string texture=value.Str("texture","");string normal=value.Str("normal","");
                if(texture.Length>0)mat.mainTexture=Texture(texture);
                if(normal.Length>0)mat.SetTexture("_BumpMap",Texture(normal));
                EditorUtility.SetDirty(mat);materials.Add(entry.Key,mat);
            }
            int count=0;
            foreach(string file in Directory.GetFiles(Art+"Models","*.fbx"))
            {
                string path=file.Replace('\\','/');string id=Path.GetFileNameWithoutExtension(path);
                InstallCharacter(id,path);count++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("CREW-INSTALLED "+count+" skinned character prefabs with ten animation states each.");
        }

        static Texture2D Texture(string name)
        {
            string path=Art+"Textures/"+name+(Path.HasExtension(name)?"":".png");
            Texture2D texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null)throw new InvalidOperationException("Missing authored character map: "+path);
            return texture;
        }

        static string ClipName(string raw)
        {
            foreach(string known in Clips)
                if(raw.EndsWith(known,StringComparison.Ordinal))return known;
            return raw;
        }

        static void InstallCharacter(string id,string path)
        {
            ModelImporter importer=(ModelImporter)AssetImporter.GetAtPath(path);
            ModelImporterClipAnimation[] takes=importer.defaultClipAnimations;
            foreach(var take in takes)
            {
                take.name=ClipName(take.name);
                take.loopTime=take.name!="Wave" && take.name!="Celebrate" && take.name!="Collect";
                take.loopPose=take.loopTime;
                take.lockRootPositionXZ=true;take.lockRootHeightY=true;take.lockRootRotation=true;
            }
            importer.clipAnimations=takes;importer.SaveAndReimport();
            AnimationClip[] clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal)).ToArray();
            foreach(string required in Clips)
                if(!clips.Any(c=>c.name==required))throw new InvalidOperationException(id+" is missing imported clip "+required+"; found "+string.Join(",",clips.Select(c=>c.name)));
            string controllerPath=Art+"Animation/"+id+".controller";
            AnimatorController controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            AnimatorStateMachine machine=controller.layers[0].stateMachine;
            foreach(var state in machine.states)machine.RemoveState(state.state);
            foreach(string name in Clips)
            {
                AnimatorState state=machine.AddState(name);state.motion=clips.First(c=>c.name==name);
                state.writeDefaultValues=true;if(name=="Idle")machine.defaultState=state;
            }
            EditorUtility.SetDirty(controller);
            var root=new GameObject(id);
            try
            {
                GameObject model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,root.transform);
                instance.name="SkinnedCharacter";
                // Keep animation on the imported hierarchy root: FBX curve paths are relative to it.
                foreach(Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] slots=renderer.sharedMaterials;
                    for(int i=0;i<slots.Length;i++)
                    {
                        string key=slots[i]==null?"":slots[i].name;
                        int suffix=key.IndexOf(" (",StringComparison.Ordinal);if(suffix>=0)key=key.Substring(0,suffix);
                        if(!materials.ContainsKey(key) && key.Length>4 && key[key.Length-4]=='.' &&
                            int.TryParse(key.Substring(key.Length-3),out int duplicate))key=key.Substring(0,key.Length-4);
                        if(!materials.TryGetValue(key,out Material replacement))throw new InvalidOperationException("Unmapped character material "+key);
                        slots[i]=replacement;
                    }
                    renderer.sharedMaterials=slots;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
                    if(renderer is SkinnedMeshRenderer skinned)skinned.updateWhenOffscreen=true;
                }
                Animator animator=instance.GetComponent<Animator>();
                if(animator==null)animator=instance.AddComponent<Animator>();
                animator.runtimeAnimatorController=controller;
                animator.avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                animator.applyRootMotion=false;
                CrewActor actor=root.AddComponent<CrewActor>();
                Transform[] bones=instance.GetComponentsInChildren<Transform>(true);
                var serialized=new SerializedObject(actor);
                serialized.FindProperty("characterId").stringValue=id;
                serialized.FindProperty("animator").objectReferenceValue=animator;
                serialized.FindProperty("head").objectReferenceValue=bones.First(t=>t.name=="Head");
                serialized.FindProperty("arm").objectReferenceValue=bones.First(t=>t.name=="UpperArm_R");
                serialized.FindProperty("socket").objectReferenceValue=bones.First(t=>t.name=="ToolSocket");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,Prefabs+id+".prefab");
                Debug.Log("CREW-IMPORT "+id+" clips="+clips.Length+" renderers="+root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
