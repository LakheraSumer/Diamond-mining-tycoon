using System;
using System.Collections.Generic;
using System.IO;
using DiamondMine.Runtime;
using DiamondMine.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiamondMine.Editor
{
    public sealed class EnvironmentModelImport : AssetPostprocessor
    {
        const string Art = "Assets/_Project/Art/GameplayEnvironments/";
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Art + "Models/", StringComparison.Ordinal)) return;
            ModelImporter importer = (ModelImporter)assetImporter;
            importer.globalScale = 1;
            importer.bakeAxisConversion = true;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.isReadable = false;
        }
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Art + "Textures/", StringComparison.Ordinal) || !assetPath.EndsWith(".png",StringComparison.Ordinal)) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = assetPath.Contains("_Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = importer.textureType != TextureImporterType.NormalMap;
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 2;
        }
    }

    public static class EnvironmentAssetPipeline
    {
        const string Art = "Assets/_Project/Art/GameplayEnvironments/";
        const string Prefabs = "Assets/_Project/Resources/Environments/";
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        [MenuItem("Diamond Mine/Art/Import gameplay environments")]
        public static void Install()
        {
            Directory.CreateDirectory(Art + "Materials");
            Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Materials.Clear();
            JsonValue spec = JsonValue.Parse(File.ReadAllText(Art + "material-spec.json"));
            foreach (var pair in spec.AsObject())
            {
                JsonValue value = pair.Value;
                var rgb = value.Array("color");
                float alpha = (float)value.Number("alpha", 1);
                Shader shader = Shader.Find(alpha < 1 ? "DiamondMine/CrewLens" : "DiamondMine/TimberSurface");
                if (shader == null) throw new InvalidOperationException("Environment surface shader missing for " + pair.Key);
                string path = Art + "Materials/" + pair.Key + ".mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
                mat.shader = shader;
                mat.name = pair.Key;
                mat.color = new Color((float)rgb[0].AsNumber(), (float)rgb[1].AsNumber(), (float)rgb[2].AsNumber(), alpha);
                mat.SetFloat("_Metallic", (float)value.Number("metallic", 0));
                mat.SetFloat("_Glossiness", 1 - (float)value.Number("roughness", .65));
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", .4f);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", mat.color * (float)value.Number("emission", 0));
                string texture = value.Str("texture", ""), normal = value.Str("normal", "");
                mat.mainTexture = texture.Length > 0 ? Texture(texture) : null;
                if (normal.Length > 0) mat.SetTexture("_BumpMap", Texture(normal));
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                Materials.Add(pair.Key, mat);
            }
            // Explore contains exact reused portal meshes and their original materials.
            foreach (string file in Directory.GetFiles("Assets/_Project/Art/TimberBunker/Materials", "*.mat"))
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(file.Replace('\\','/'));
                if (mat != null) Materials[mat.name] = mat;
            }
            GameObject root = new GameObject("GameplayEnvironmentSet");
            try
            {
                var component = root.AddComponent<GameplayEnvironmentSet>();
                var serialized = new SerializedObject(component);
                string[] names = { "Explore", "Mine", "Factory", "Workshop", "Boutique", "RegionMap" };
                string[] fields = { "explore", "mine", "factory", "workshop", "boutique", "regionMap" };
                Transform[] contexts = new Transform[names.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    GameObject context = new GameObject(names[i]);
                    context.transform.SetParent(root.transform, false);
                    context.transform.localPosition = new Vector3(i == 0 ? 0 : 20 + i * 20, 0, 0);
                    contexts[i] = context.transform;
                    AddModel(names[i], context.transform);
                    AddReflection(context.transform);
                    serialized.FindProperty(fields[i]).objectReferenceValue = context.transform;
                }
                TimberBunkerArea hero = TimberBunkerArea.Create(contexts[0]);
                if (hero == null) throw new InvalidOperationException("Import the timber bunker before gameplay environments.");
                serialized.FindProperty("timberArea").objectReferenceValue = hero;
                SerializedProperty labels = serialized.FindProperty("bunkerLabels");
                labels.arraySize = WorldGenerator.VisibleYardPlots;
                for (int plot = 0; plot < 6; plot++)
                {
                    YardPoint entrance = YardLayout.Entrance(plot);
                    GameObject sign = new GameObject("PlotState_" + plot);
                    sign.transform.SetParent(contexts[0], false);
                    sign.transform.localPosition = new Vector3((float)entrance.X, 2.92f, (float)entrance.Z - .3f);
                    sign.transform.localRotation = Quaternion.Euler(0,180,0);
                    TextMesh label = sign.AddComponent<TextMesh>();
                    label.text = (plot + 1).ToString();
                    label.anchor = TextAnchor.MiddleCenter;
                    label.alignment = TextAlignment.Center;
                    label.characterSize = .055f;
                    label.fontSize = 40;
                    label.color = new Color(.99f,.91f,.68f);
                    labels.GetArrayElementAtIndex(plot).objectReferenceValue = label;
                    // Other starter portals share the authored imported cart.
                    if (plot > 0)
                    {
                        Transform cart = TimberBunkerArea.CreateCart(contexts[0]);
                        if (cart == null) throw new InvalidOperationException("Authored ore cart prefab missing.");
                        cart.name = "ParkedCart_" + plot;
                        cart.localPosition = new Vector3((float)entrance.X,0,(float)entrance.Z-.98f);
                    }
                }
                AddCrew("oren", contexts[0], new Vector3(-7.25f,0,5.65f), 35);
                AddCrew("pavi", contexts[2], new Vector3(-1.9f,0,-.4f), 10);
                AddCrew("nilo", contexts[2], new Vector3(.05f,0,-.4f), 10);
                AddCrew("zela", contexts[2], new Vector3(2.1f,0,-.45f), 0);
                AddCrew("rumi", contexts[3], new Vector3(0,0,-.43f), 0);
                AddCrew("sola", contexts[4], new Vector3(-.65f,0,1.47f), 180);
                AddCrew("kavi", contexts[4], new Vector3(1.05f,0,-1.55f), 0);
                foreach (Vector3 point in new[] {new Vector3(-2.6f,2.2f,-1.4f),new Vector3(2.6f,2.2f,1.3f)})
                    PointLight(contexts[1],point, new Color(1,.55f,.20f),2.5f,5);
                PointLight(contexts[3],new Vector3(.3f,1.64f,.75f),new Color(1,.79f,.45f),1.5f,3);
                PointLight(contexts[4],new Vector3(0,2.6f,1.8f),new Color(1,.75f,.40f),1.5f,5);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                // The runtime controller activates exactly one context before it is shown.
                foreach (Transform context in contexts) context.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "GameplayEnvironmentSet.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("ENVIRONMENTS-INSTALLED six authored FBX contexts; no runtime primitive replacement.");
        }

        static Texture2D Texture(string name)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Textures/" + name + ".png");
            if (texture == null) throw new InvalidOperationException("Missing authored environment texture " + name);
            return texture;
        }

        static void AddModel(string name, Transform parent)
        {
            string path = Art + "Models/" + name + ".fbx";
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Authored environment FBX missing " + path);
            importer.bakeAxisConversion = true;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) throw new InvalidOperationException("Authored environment FBX failed import " + path);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.name = name + "_AuthoredMeshes";
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string key = mats[i] == null ? "" : mats[i].name;
                    int suffix = key.IndexOf(" (", StringComparison.Ordinal);
                    if (suffix >= 0) key = key.Substring(0,suffix);
                    if (!Materials.TryGetValue(key,out Material replacement))
                    {
                        int dot = key.LastIndexOf('.');
                        if (dot >= 0) Materials.TryGetValue(key.Substring(0,dot),out replacement);
                    }
                    if (replacement == null) throw new InvalidOperationException("Unmapped environment material " + key + " on " + renderer.name);
                    mats[i] = replacement;
                }
                renderer.sharedMaterials = mats;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        static void AddCrew(string id, Transform parent, Vector3 localPosition, float yaw)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/Characters/" + id + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Import modelled crew before environments: " + id);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
            instance.name = "RoleCrew_" + id;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(0,yaw,0);
        }

        static void PointLight(Transform parent,Vector3 position,Color color,float intensity,float range)
        {
            var lamp = new GameObject("AuthoredLanternLight");
            lamp.transform.SetParent(parent,false);
            lamp.transform.localPosition = position;
            Light light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        static void AddReflection(Transform parent)
        {
            Cubemap sky = AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/_Project/Art/TimberBunker/Textures/SoftSkyReflection.asset");
            if (sky == null) throw new InvalidOperationException("Run timber import to create the authored soft-sky reflection map first.");
            var probeObject = new GameObject("SoftSkyMaterialReflection");
            probeObject.transform.SetParent(parent,false);
            probeObject.transform.localPosition = new Vector3(0,2,2);
            ReflectionProbe probe = probeObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Custom;
            probe.customBakedTexture = sky;
            probe.size = new Vector3(32,16,32);
            probe.intensity = .65f;
            probe.importance = 1;
            probe.boxProjection = false;
        }
    }
}
