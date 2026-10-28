using System;
using System.Collections.Generic;
using System.IO;
using DiamondMine.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiamondMine.Editor
{
    public sealed class TimberModelImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/_Project/Art/TimberBunker/Models/", StringComparison.Ordinal)) return;
            ModelImporter importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.bakeAxisConversion = true;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.isReadable = false;
            importer.generateSecondaryUV = false;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/_Project/Art/TimberBunker/Textures/", StringComparison.Ordinal)) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = assetPath.EndsWith("_Normal.png", StringComparison.Ordinal)
                ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = importer.textureType != TextureImporterType.NormalMap;
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 2;
        }
    }

    public static class TimberAssetPipeline
    {
        const string Art = "Assets/_Project/Art/TimberBunker/";
        const string Prefabs = "Assets/_Project/Resources/Yard/";
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        public static void Inspect()
        {
            foreach(string name in new[]{"TimberBunker","TerrainPatch","MineTrack","OreCart"})
            {
                GameObject model=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Models/"+name+".fbx");
                foreach(Renderer r in model.GetComponentsInChildren<Renderer>())
                    if(r.name.Contains("LanternGlow") || r.name.Contains("BlackIron") || r.name.Contains("Soil") || r.name.Contains("Steel"))
                        Debug.Log("IMPORT-BOUNDS "+name+" / "+r.name+" "+r.bounds.center.ToString("F3")+" size "+r.bounds.size.ToString("F3"));
            }
        }

        [MenuItem("Diamond Mine/Art/Import timber bunker")]
        public static void Install()
        {
            Directory.CreateDirectory(Art + "Materials");
            Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            mats.Clear();
            Make("Oak", new Color(.54f,.30f,.12f), .65f, 0, "AgedOak");
            Make("OakEnd", new Color(.64f,.39f,.18f), .77f);
            Make("TunnelOak", new Color(.25f,.13f,.06f), .85f);
            Make("Rock", new Color(.51f,.48f,.47f), .82f, 0, "StratifiedStone");
            Make("RockLight", new Color(.49f,.47f,.44f), .88f);
            Make("RockShadow", new Color(.34f,.35f,.37f), .88f);
            Make("Soil", new Color(.60f,.43f,.24f), .95f, 0, "PackedEarth");
            Make("EarthCut", new Color(.32f,.235f,.15f), .96f);
            Make("Moss", new Color(.28f,.36f,.10f), .95f);
            Make("Steel", new Color(.30f,.34f,.36f), .46f,.65f,"ForgedSteel");
            Make("WornSteel",new Color(.52f,.57f,.59f),.32f,.7f);
            Make("Oxide",new Color(.29f,.18f,.095f),.8f,.25f);
            Make("BlackIron",new Color(.12f,.15f,.155f),.42f,.6f);
            Make("Brass",new Color(.65f,.40f,.12f),.35f,.65f);
            Make("PlaqueLetter",new Color(.96f,.84f,.58f),.6f);
            Make("Leaf",new Color(.20f,.37f,.065f),.82f);
            Make("LeafLight",new Color(.42f,.54f,.105f),.87f);
            Make("LeafDark",new Color(.115f,.25f,.06f),.86f);
            Make("Grass",new Color(.47f,.54f,.12f),.85f);
            Make("Flower",new Color(.97f,.66f,.13f),.7f);
            Make("LanternGlow",new Color(1,.48f,.085f),.3f,0,null,.9f);
            Make("TunnelBlack",new Color(.035f,.028f,.022f),1f);
            Make("Ore",new Color(.49f,.51f,.53f),.78f);
            Make("OreLight",new Color(.67f,.65f,.60f),.7f);
            GameObject cart = new GameObject("OreCart");
            try
            {
                AddModel("OreCart", cart.transform);
                PrefabUtility.SaveAsPrefabAsset(cart, Prefabs + "OreCart.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(cart); }
            GameObject area = new GameObject("TimberBunker");
            try
            {
                AddModel("TimberBunker",area.transform);
                AddModel("TerrainPatch",area.transform);
                AddModel("MineTrack",area.transform);
                GameObject cartPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"OreCart.prefab");
                GameObject parked=(GameObject)PrefabUtility.InstantiatePrefab(cartPrefab,area.transform);
                parked.name="ParkedCart";
                parked.transform.localPosition=new Vector3(0,0,-.98f);
                var lamp=new GameObject("LanternLight"); lamp.transform.SetParent(area.transform,false);
                lamp.transform.localPosition=new Vector3(-1.10f,1.49f,-.62f);
                Light light=lamp.AddComponent<Light>(); light.type=LightType.Point;
                light.color=new Color(1,.52f,.16f); light.range=2.5f; light.intensity=2.1f;
                light.renderMode=LightRenderMode.ForcePixel; light.shadows=LightShadows.None;
                var reflection=new GameObject("SoftSkyReflection");reflection.transform.SetParent(area.transform,false);
                reflection.transform.localPosition=new Vector3(0,1.2f,-.7f);
                ReflectionProbe probe=reflection.AddComponent<ReflectionProbe>();
                probe.mode=ReflectionProbeMode.Custom;
                probe.customBakedTexture=MakeReflection();
                probe.size=new Vector3(8,7,9);probe.intensity=.9f;probe.importance=2;
                probe.boxProjection=false;
                TimberBunkerArea component=area.AddComponent<TimberBunkerArea>();
                var serialized=new SerializedObject(component);
                serialized.FindProperty("parkedCart").objectReferenceValue=parked.transform;
                serialized.FindProperty("lantern").objectReferenceValue=light;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(area,Prefabs+"TimberBunker.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(area); }
            AssetDatabase.SaveAssets();
            Debug.Log("Timber bunker imported: 4 FBX models, 8 texture maps, 24 material assets; Main uses Resources/Yard prefabs.");
        }

        static Cubemap MakeReflection()
        {
            string path=Art+"Textures/SoftSkyReflection.asset";
            Cubemap cube=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if(cube==null)
            {
                cube=new Cubemap(64,TextureFormat.RGBAHalf,true);
                cube.name="SoftSkyReflection";
                AssetDatabase.CreateAsset(cube,path);
            }
            Vector3 key=new Vector3(-.4f,.8f,-.4f).normalized;
            for(int face=0;face<6;face++) for(int y=0;y<64;y++) for(int x=0;x<64;x++)
            {
                float u=(x+.5f)/32-1,v=(y+.5f)/32-1;
                Vector3 d=face==0?new Vector3(1,-v,-u):face==1?new Vector3(-1,-v,u):
                    face==2?new Vector3(u,1,v):face==3?new Vector3(u,-1,-v):
                    face==4?new Vector3(u,-v,1):new Vector3(-u,-v,-1);
                d.Normalize();
                Color color=Color.Lerp(new Color(.18f,.16f,.13f),new Color(.66f,.74f,.84f),Mathf.Clamp01(d.y*.7f+.5f));
                color+=new Color(1,.91f,.76f)*Mathf.Pow(Mathf.Max(0,Vector3.Dot(d,key)),20)*1.1f;
                cube.SetPixel((CubemapFace)face,x,y,color);
            }
            cube.Apply(true,false);EditorUtility.SetDirty(cube);
            return cube;
        }

        static void Make(string name,Color color,float rough,float metal=0,string texture=null,float emission=0)
        {
            string path=Art+"Materials/"+name+".mat";
            Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader=Shader.Find("DiamondMine/TimberSurface");
            if(shader==null) throw new InvalidOperationException("TimberSurface shader failed to import.");
            if(m==null) { m=new Material(shader); AssetDatabase.CreateAsset(m,path); }
            m.shader=shader; m.name=name;
            m.color=texture==null ? color : Color.white;
            m.SetFloat("_Metallic",metal); m.SetFloat("_Glossiness",1-rough);
            m.SetFloat("_BumpScale",.45f);
            m.SetColor("_EmissionColor",color*emission);
            if(texture!=null)
            {
                m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Textures/"+texture+"_BaseColor.png");
                m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Textures/"+texture+"_Normal.png"));
                if(m.mainTexture==null) throw new InvalidOperationException("Missing authored texture: "+texture);
            }
            EditorUtility.SetDirty(m); mats.Add(name,m);
        }

        static void AddModel(string name,Transform parent)
        {
            GameObject model=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Models/"+name+".fbx");
            if(model==null) throw new InvalidOperationException("Blender FBX missing or failed import: "+name);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,parent);
            instance.name=name;
            foreach(Renderer r in instance.GetComponentsInChildren<Renderer>())
            {
                Material[] imported=r.sharedMaterials;
                for(int i=0;i<imported.Length;i++)
                {
                    string key=imported[i]==null ? "" : imported[i].name;
                    int suffix=key.IndexOf(" (",StringComparison.Ordinal);
                    if(suffix>=0) key=key.Substring(0,suffix);
                    if(!mats.TryGetValue(key,out Material replacement))
                        throw new InvalidOperationException("Unmapped imported material: "+key+" on "+r.name);
                    imported[i]=replacement;
                }
                r.sharedMaterials=imported; r.shadowCastingMode=ShadowCastingMode.On; r.receiveShadows=true;
            }
        }
    }
}
