using System;
using System.IO;
using System.Reflection;
using DiamondMine.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace DiamondMine.Editor
{
    public static class BuildCommands
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            ApplyPlayerSettings();
            EnsureUnlitReference();
        }

        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = Branding.CompanyPlaceholder;
            PlayerSettings.productName = Branding.WorkingTitle;
            Texture2D appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/AppIcon.png");
            if (appIcon != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { appIcon }, IconKind.Any);
            PlayerSettings.bundleVersion = Branding.Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, Branding.AndroidApplicationId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, Branding.IosBundleId);
            PlayerSettings.Android.bundleVersionCode = Branding.BuildNumber;
            PlayerSettings.iOS.buildNumber = Branding.BuildNumber.ToString();

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.gcIncremental = true;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.muteOtherAudioSources = false;
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.iOS, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.androidTVCompatibility = false;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.Android.useCustomKeystore = false;

            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        public static void ValidateProject()
        {
            ApplyPlayerSettings();
            EnsureUnlitReference();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
            if (!scene.IsValid()) throw new InvalidOperationException("Main scene could not be loaded.");
            var economy = Resources.Load<TextAsset>("economy");
            if (economy == null) throw new InvalidOperationException("Economy resource is missing.");
            DiamondMine.Simulation.EconomyLoader.Load(economy.text);
            Debug.Log("Diamond Mine project import and configuration validation passed.");
        }

        public static void BuildAndroidApk()
        {
            ApplyPlayerSettings();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.diamondmine.tycoon.qa");
            PlayerSettings.bundleVersion = Branding.Version + "-qa-dev";
            PlayerSettings.productName = Branding.WorkingTitle + " QA Dev";
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = true;
            Build(BuildTarget.Android, "Builds/Android/DiamondMineTycoon-" + Branding.Version + "-dev.apk", BuildOptions.Development, "android-apk");
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.development = false;
        }

        public static void BuildAndroidReleaseQa()
        {
            ApplyPlayerSettings();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.diamondmine.tycoon.qa");
            PlayerSettings.bundleVersion = Branding.Version + "-qa";
            PlayerSettings.productName = Branding.WorkingTitle + " QA";
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = false;
            Build(BuildTarget.Android, "Builds/Android/DiamondMineTycoon-" + Branding.Version + "-qa-release.apk", BuildOptions.None, "android-qa-release");
        }

        public static void BuildAndroidAab()
        {
            string keystore = Environment.GetEnvironmentVariable("KEYSTORE_PATH");
            string storePass = Environment.GetEnvironmentVariable("KEYSTORE_PASS");
            string alias = Environment.GetEnvironmentVariable("KEY_ALIAS");
            string keyPass = Environment.GetEnvironmentVariable("KEY_PASS");
            if (string.IsNullOrEmpty(keystore) || string.IsNullOrEmpty(storePass) || string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(keyPass))
                throw new InvalidOperationException("Refusing to build a release Android App Bundle without KEYSTORE_PATH, KEYSTORE_PASS, KEY_ALIAS, and KEY_PASS. A debug signature is not a Play upload key.");
            if (!File.Exists(keystore))
                throw new FileNotFoundException("Keystore was not found.", keystore);

            ApplyPlayerSettings();
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = keyPass;
            try
            {
                Build(BuildTarget.Android, "Builds/Android/DiamondMineTycoon-" + Branding.Version + ".aab", BuildOptions.None, "android-aab");
            }
            finally
            {
                PlayerSettings.Android.useCustomKeystore = false;
                PlayerSettings.Android.keystoreName = "";
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasName = "";
                PlayerSettings.Android.keyaliasPass = "";
            }
        }

        public static void ExportIos()
        {
            ApplyPlayerSettings();
            EditorUserBuildSettings.development = false;
            Build(BuildTarget.iOS, "Builds/iOS", BuildOptions.None, "ios-xcode");
        }

        public static void GenerateStoreArt()
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "Store", "Generated");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, "icon-512.png"), Icon(512).EncodeToPNG());
            File.WriteAllBytes(Path.Combine(directory, "icon-1024.png"), Icon(1024).EncodeToPNG());
            File.WriteAllBytes(Path.Combine(directory, "feature-1024x500.png"), Feature(1024, 500).EncodeToPNG());
            Debug.Log("Wrote procedural store art to " + directory + ". These are not gameplay screenshots.");
        }

        static void Build(BuildTarget target, string relativePath, BuildOptions options, string label)
        {
            string project = Directory.GetCurrentDirectory();
            string artifactRoot = Environment.GetEnvironmentVariable("DIAMOND_ARTIFACT_ROOT");
            if (string.IsNullOrEmpty(artifactRoot)) artifactRoot = project;
            string output = Path.Combine(artifactRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            string folder = target == BuildTarget.iOS ? output : Path.GetDirectoryName(output);
            Directory.CreateDirectory(folder);
            string notePath = Path.Combine(artifactRoot, "Builds", label + "-manifest.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(notePath));
            File.WriteAllText(notePath,
                "product=" + Branding.WorkingTitle + "\n" +
                "version=" + Branding.Version + "\n" +
                "build=" + Branding.BuildNumber + "\n" +
                "unity=" + Application.unityVersion + "\n" +
                "target=" + target + "\n" +
                "applicationId=" + PlayerSettings.GetApplicationIdentifier(target == BuildTarget.Android ? NamedBuildTarget.Android : NamedBuildTarget.iOS) + "\n" +
                "bundleVersion=" + PlayerSettings.bundleVersion + "\n" +
                "development=" + ((options & BuildOptions.Development) != 0) + "\n" +
                "androidArchitectures=" + PlayerSettings.Android.targetArchitectures + "\n" +
                "androidMinApi=" + PlayerSettings.Android.minSdkVersion + "\n" +
                "androidTargetApi=" + PlayerSettings.Android.targetSdkVersion + "\n" +
                "scriptingBackend=IL2CPP\n" +
                "portrait=" + PlayerSettings.defaultInterfaceOrientation + "\n" +
                "output=" + output + "\n" +
                "startedUtc=" + DateTime.UtcNow.ToString("o") + "\n");

            var playerOptions = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = target,
                options = options
            };
            UnityEditor.Build.Reporting.BuildReport report;
            using (var tools = new ProjectAndroidTools(target))
            {
                File.AppendAllText(notePath,"effectiveAndroidTools=" + AndroidTools() + "\n");
                report = BuildPipeline.BuildPlayer(playerOptions);
            }
            bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            File.AppendAllText(notePath,
                "result=" + report.summary.result + "\n" +
                "errors=" + report.summary.totalErrors + "\n" +
                "warnings=" + report.summary.totalWarnings + "\n" +
                "size=" + report.summary.totalSize + "\n" +
                "finishedUtc=" + DateTime.UtcNow.ToString("o") + "\n");
            if (!ok)
                throw new Exception(label + " build failed. See the Unity log and " + notePath);
        }

        static string AndroidTools()
        {
            Type type = Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor.AndroidModule");
            if (type == null)
                type = Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor.Android.Extensions");
            if (type == null)
                return "Android editor module is not loaded, so SDK, NDK, and JDK paths were not read.";
            return "sdk=" + Prop(type, "sdkRootPath") + " ndk=" + Prop(type, "ndkRootPath") + " jdk=" + Prop(type, "jdkRootPath");
        }

        sealed class ProjectAndroidTools : IDisposable
        {
            readonly System.Collections.Generic.Dictionary<PropertyInfo, object> previous = new System.Collections.Generic.Dictionary<PropertyInfo, object>();
            public ProjectAndroidTools(BuildTarget target)
            {
                if (target != BuildTarget.Android) return;
                Type type = Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor.Android.Extensions");
                if (type == null)
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = assembly.GetType("UnityEditor.Android.AndroidExternalToolsSettings");
                        if (type != null) break;
                    }
                string[] properties = { "jdkRootPath", "ndkRootPath", "sdkRootPath" };
                string[] names = { "DIAMOND_ANDROID_JDK", "DIAMOND_ANDROID_NDK", "DIAMOND_ANDROID_SDK" };
                try
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        string value = Environment.GetEnvironmentVariable(names[i]);
                        if (string.IsNullOrEmpty(value)) continue;
                        PropertyInfo property = type == null ? null : type.GetProperty(properties[i], BindingFlags.Static | BindingFlags.Public);
                        if (property == null || !property.CanWrite) throw new InvalidOperationException("Cannot select the project-local Android tool: " + properties[i]);
                        previous[property] = property.GetValue(null, null);
                        property.SetValue(null, value, null);
                    }
                }
                catch { Dispose(); throw; }
            }
            public void Dispose()
            {
                foreach (var property in previous)
                {
                    string original = property.Value as string;
                    if (string.IsNullOrEmpty(original) || !Directory.Exists(original))
                    {
                        Debug.Log("Previous Android " + property.Key.Name + " was unconfigured or unavailable; retaining the verified project-local tool path.");
                        continue;
                    }
                    try { property.Key.SetValue(null, property.Value, null); }
                    catch (Exception ex) { Debug.LogWarning("Could not restore Android " + property.Key.Name + ": " + ex.Message); }
                }
                previous.Clear();
            }
        }

        static string Prop(Type type, string name)
        {
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            if (property == null)
                return "unknown";
            object value = property.GetValue(null, null);
            return value == null ? "" : value.ToString();
        }

        static void EnsureUnlitReference()
        {
            const string path = "Assets/_Project/Resources/IncludedUnlit.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                return;
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
                return;
            Directory.CreateDirectory("Assets/_Project/Resources");
            AssetDatabase.CreateAsset(new Material(shader), path);
        }

        static Texture2D Icon(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center) / center;
                    float dy = Mathf.Abs(y - center) / center;
                    bool gem = dx + dy < 0.72f;
                    bool ground = y < size * 0.22f;
                    Color color = ground
                        ? new Color(0.36f, 0.22f, 0.14f)
                        : gem ? new Color(0.35f, 0.86f, 0.92f) : new Color(0.22f, 0.14f, 0.11f);
                    pixels[y * size + x] = color;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        static Texture2D Feature(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool band = y > height * 0.35f && y < height * 0.62f && x > width * 0.08f && x < width * 0.92f;
                    pixels[y * width + x] = band
                        ? new Color(0.28f, 0.72f, 0.78f)
                        : new Color(0.25f, 0.16f, 0.12f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
}
