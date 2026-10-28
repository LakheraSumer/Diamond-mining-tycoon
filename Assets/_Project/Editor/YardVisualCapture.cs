using System;
using System.IO;
using DiamondMine.Runtime;
using DiamondMine.Simulation;
using UnityEditor;
using UnityEngine;

namespace DiamondMine.Editor
{
    // Renders the playable yard meshes. Trigger by creating Logs/capture-yard-visual.flag.
    public static class YardVisualCapture
    {
        const string PlayKey = "DiamondMine.YardVisual.Play";
        static double playStart;
        static int playStep;

        [InitializeOnLoadMethod]
        static void Boot()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying)
                return;
            string flag = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "capture-yard-visual.flag");
            if (!File.Exists(flag))
                return;
            File.Delete(flag);
            try
            {
                RenderStill();
            }
            catch (System.Exception error)
            {
                Debug.LogError("Yard visual render failed: " + error);
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Logs", "yard-visual-error.txt"), error.ToString());
                return;
            }
            SessionState.SetBool(PlayKey, true);
            playStart = 0d;
            playStep = 0;
            string save = Path.Combine(Directory.GetCurrentDirectory(), ".cache", "yard-visual-save");
            Directory.CreateDirectory(save);
            Environment.SetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT", save);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            EditorApplication.isPlaying = true;
            EditorApplication.update -= PlayTick;
            EditorApplication.update += PlayTick;
        }

        static void PlayTick()
        {
            if (!SessionState.GetBool(PlayKey, false))
            {
                EditorApplication.update -= PlayTick;
                return;
            }
            if (!EditorApplication.isPlaying)
                return;
            if (playStart <= 0d)
                playStart = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup < playStart + 3d)
                return;
            YardWorld yard = UnityEngine.Object.FindAnyObjectByType<YardWorld>();
            GameSession session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            if (yard == null || session == null || !session.Ready)
            {
                if (EditorApplication.timeSinceStartup > playStart + 20d)
                {
                    SessionState.SetBool(PlayKey, false);
                    EditorApplication.isPlaying = false;
                }
                return;
            }
            if (playStep == 0)
            {
                Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                for (int i = 0; i < canvases.Length; i++)
                    canvases[i].enabled = false;
                Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                for (int i = 0; i < cameras.Length; i++)
                    cameras[i].enabled = cameras[i] == yard.View;
                YardPoint door = YardLayout.Entrance(0);
                session.State.Adventure.SelectedCharacter = "tavi";
                session.State.Adventure.PlayerX = door.X;
                session.State.Adventure.PlayerZ = door.Z - 1.7d;
                yard.Facing = 0f;
                yard.View.enabled = true;
                yard.View.fieldOfView = 48f;
                yard.View.transform.position = new Vector3(-4.2f, 3.6f, 1.6f);
                yard.View.transform.LookAt(new Vector3(-6.4f, 1.1f, 5.4f));
                playStep = 1;
                return;
            }
            string path = Path.Combine(Directory.GetCurrentDirectory(), "release", "screenshots", "yard-visual", "game-view.png");
            ScreenCapture.CaptureScreenshot(path);
            SessionState.SetBool(PlayKey, false);
            EditorApplication.update -= PlayTick;
            EditorApplication.delayCall += delegate { EditorApplication.isPlaying = false; };
        }

        static void RenderStill()
        {
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "release", "screenshots", "yard-visual");
            Directory.CreateDirectory(folder);
            var root = new GameObject("YardVisualStill");
            try
            {
                var lightObject = new GameObject("CaptureLight");
                lightObject.transform.SetParent(root.transform, false);
                lightObject.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
                Light sun = lightObject.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = new Color(1f, 0.96f, 0.86f);
                sun.intensity = 1.25f;
                sun.shadows = LightShadows.Soft;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.62f, 0.7f, 0.68f);
                YardArt.BuildSite(root.transform);
                YardPoint door = YardLayout.Entrance(0);
                YardArt.Crew tavi = YardArt.BuildCrew(JourneyCatalog.FindCharacter("tavi"), root.transform);
                tavi.Root.position = new Vector3((float)door.X + 0.35f, 0f, (float)door.Z - 1.65f);
                tavi.Root.rotation = Quaternion.Euler(0f, 15f, 0f);
                var cameraObject = new GameObject("CaptureCamera");
                cameraObject.transform.SetParent(root.transform, false);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.55f, 0.78f, 0.92f);
                camera.fieldOfView = 48f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 80f;
                camera.transform.position = new Vector3(-4.2f, 3.6f, 1.6f);
                camera.transform.LookAt(new Vector3(-6.4f, 1.1f, 5.4f));
                Save(camera, Path.Combine(folder, "hero.png"), 1280, 720);
                camera.fieldOfView = 55f;
                camera.transform.position = new Vector3(0.5f, 14f, -8f);
                camera.transform.LookAt(new Vector3(0f, 0.5f, 3.5f));
                Save(camera, Path.Combine(folder, "overview.png"), 1600, 900);
                Compare(Path.Combine(folder, "hero.png"), Path.Combine(folder, "compare-hero.png"));
                File.WriteAllText(Path.Combine(folder, "capture.txt"), "editor camera render of YardArt meshes\n" + Application.unityVersion);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        static void Save(Camera camera, string path, int width, int height)
        {
            var texture = new RenderTexture(width, height, 24);
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(image);
        }

        static void Compare(string heroPath, string comparePath)
        {
            string referencePath = Path.Combine(Directory.GetCurrentDirectory(), "Docs", "design-boards", "yard-board.jpg");
            if (!File.Exists(referencePath) || !File.Exists(heroPath))
                return;
            var reference = new Texture2D(2, 2, TextureFormat.RGB24, false);
            reference.LoadImage(File.ReadAllBytes(referencePath));
            int cropW = Mathf.RoundToInt(reference.width * 0.46f);
            int cropY = Mathf.RoundToInt(reference.height * 0.12f);
            int cropH = Mathf.RoundToInt(reference.height * 0.76f);
            var crop = new Texture2D(cropW, cropH, TextureFormat.RGB24, false);
            crop.SetPixels(reference.GetPixels(0, cropY, cropW, cropH));
            crop.Apply();
            var hero = new Texture2D(2, 2, TextureFormat.RGB24, false);
            hero.LoadImage(File.ReadAllBytes(heroPath));
            int height = 720;
            Texture2D left = Scale(crop, Mathf.RoundToInt(cropW * (height / (float)cropH)), height);
            Texture2D right = Scale(hero, Mathf.RoundToInt(hero.width * (height / (float)hero.height)), height);
            var board = new Texture2D(left.width + right.width + 16, height, TextureFormat.RGB24, false);
            var clear = new Color[board.width * board.height];
            for (int i = 0; i < clear.Length; i++)
                clear[i] = new Color(0.12f, 0.1f, 0.08f);
            board.SetPixels(clear);
            board.SetPixels(0, 0, left.width, height, left.GetPixels());
            board.SetPixels(left.width + 16, 0, right.width, height, right.GetPixels());
            board.Apply();
            File.WriteAllBytes(comparePath, board.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(reference);
            UnityEngine.Object.DestroyImmediate(crop);
            UnityEngine.Object.DestroyImmediate(hero);
            UnityEngine.Object.DestroyImmediate(left);
            UnityEngine.Object.DestroyImmediate(right);
            UnityEngine.Object.DestroyImmediate(board);
        }

        static Texture2D Scale(Texture2D source, int width, int height)
        {
            var scaled = new Texture2D(width, height, TextureFormat.RGB24, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float v = (y + 0.5f) / height;
                    scaled.SetPixel(x, y, source.GetPixelBilinear(u, v));
                }
            }
            scaled.Apply();
            return scaled;
        }
    }
}
