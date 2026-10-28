using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DiamondMine.Editor
{
    /// <summary>Installs the exported source assets into this project before review or building.</summary>
    public static class ReferenceAssetImport
    {
        [MenuItem("Diamond Mine/Art/Import all authored assets")]
        public static void Install()
        {
            TimberAssetPipeline.Install();
            CrewAssetPipeline.Install();
            EnvironmentAssetPipeline.Install();
            AssetDatabase.SaveAssets();
            Debug.Log("REFERENCE-ASSETS-INSTALLED: timber, twelve crew and six environment contexts.");
        }

        public static void Run()
        {
            try { Install(); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }
}
