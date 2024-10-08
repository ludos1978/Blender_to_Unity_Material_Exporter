/* Copyright
2024 Reto Spoerri
rspoerri@nouser.org
*/

using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;
// using System.Diagnostics;
using System.Text;
using UnityEditor.AssetImporters;
using NUnit.Framework.Constraints;

[System.Serializable]

public class MaterialTextureData {
    public List<MaterialTextureInfo> materials; // Changed to a list
}

[System.Serializable]
public class MaterialTextureInfo {
    public string materialName; // Material name
    public List<TextureInfo> textureInfos; // List of texture info
    public float roughness; // Store roughness if necessary
}

[System.Serializable]
public class TextureInfo {
    public string image_name; // Relative path to the texture from the model's path
    public string channel; // Store channels if necessary
    public string comments; // Store comments if necessary
}



public class GPUInstancing : AssetPostprocessor
{
    private const string targetModelPath = "Assets/YourModelPath/YourModelName.fbx"; // Update this path to your specific model's path

    public Material OnAssignMaterialModel(Material material, Renderer renderer)
    {
        // Check if the current model being processed is the one you want to target
        if (assetPath.Equals(targetModelPath, System.StringComparison.OrdinalIgnoreCase))
        {
            ModelImporter importer = (ModelImporter)assetImporter;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(material), 
                (Material)AssetDatabase.LoadAssetAtPath("Assets/ProfilingData/Materials/material.2.mat", typeof(Material)));
            return null; // Returning null means the material is replaced
        }

        // Return the original material if the model does not match
        return material;
    }
}

public class MaterialTextureUpdaterEditor : EditorWindow {
    private string jsonFilePath; // Path to the JSON file
    private GameObject selectedModel; // Reference to the selected model

    [MenuItem("Tools/Material Texture Updater")]
    public static void ShowWindow() {
        GetWindow<MaterialTextureUpdaterEditor>("Material Texture Updater");
    }

    GameObject _previousModel = null;
    private int materialCount = 0;
    private void OnGUI() {
        GUILayout.Label("Material Texture Updater", EditorStyles.boldLabel);

        // Field to select a model from the project
        selectedModel = (GameObject)EditorGUILayout.ObjectField("Selected Model", selectedModel, typeof(GameObject), false);
        if (selectedModel != _previousModel) {
            _previousModel = selectedModel;
            materialCount = -1;
            jsonFilePath = "";
        }

        if (selectedModel != null) {


            if (GUILayout.Button("Export JSON from Blender")) {
                string assetPath = AssetDatabase.GetAssetPath(selectedModel);
                string fullModelPath = Path.GetFullPath(Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length)));
                Debug.Log($"Run Blender on {fullModelPath}");
                LaunchBlender(fullModelPath);
                materialCount = -1;
            }

            if (String.IsNullOrEmpty(jsonFilePath)) {
                string expectedJsonFile = GetExpectedJsonFilePath(selectedModel);

                if (File.Exists(expectedJsonFile)) {
                    jsonFilePath = expectedJsonFile;
                }
                // else {
                //     EditorGUILayout.LabelField("JSON File Not Found: " + expectedJsonFile);
                // }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"JSON File: {jsonFilePath}");

            if (GUILayout.Button("Manually select"))  {
                jsonFilePath = EditorUtility.OpenFilePanel("Select JSON File", Path.GetDirectoryName(AssetDatabase.GetAssetPath(selectedModel)), "json");
                jsonFilePath = FileUtil.GetProjectRelativePath(jsonFilePath);
                materialCount = -1;
            }
            if (GUILayout.Button("Reset"))  {
                jsonFilePath = "";
                materialCount = -1;
            }
            EditorGUILayout.EndHorizontal();

            if ((!String.IsNullOrEmpty(jsonFilePath)) && (jsonFilePath!="ERROR")) {

                if (materialCount < 0) {
                    string jsonData = File.ReadAllText(jsonFilePath);
                    MaterialTextureData materialData = JsonUtility.FromJson<MaterialTextureData>(jsonData);
                    if (materialData == null) {
                        Debug.LogError("Failed to deserialize JSON data.");
                        jsonFilePath = "ERROR";
                    } else {
                        materialCount = materialData.materials.Count;
                    }
                }
                EditorGUILayout.LabelField($"JSON File with {materialCount} Materials");

                GUILayout.Space(10);

                if (GUILayout.Button("Create new Materials from JSON")) {
                    ApplyTexturesToSelectedModel();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply Material Remap")) {
                    ApplyMaterialRemapSettings();
                }

                if (GUILayout.Button("Clear Material Remapping for Model")) {
                    string selectedModelPath = AssetDatabase.GetAssetPath(selectedModel);
                    AssetImporter assetImporter = AssetImporter.GetAtPath(selectedModelPath);
                    Dictionary<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> externalObjectMap = assetImporter.GetExternalObjectMap();
                    foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> kvp in externalObjectMap) {
                        Debug.Log(kvp.Key + " -> " + kvp.Value);
                        assetImporter.RemoveRemap(kvp.Key);
                    }
                    AssetDatabase.WriteImportSettingsIfDirty(selectedModelPath);
                    AssetDatabase.ImportAsset(selectedModelPath, ImportAssetOptions.ForceUpdate);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }


    private void LaunchBlender(string blenderModelFile) {
        string blenderPath = GetBlenderPath();
        if (string.IsNullOrEmpty(blenderPath)) {
            UnityEngine.Debug.LogError("Blender not found.");
            return;
        }

        string blendFile = blenderModelFile; // Adjust your blend file path if needed

        // Construct the path to the Python script
        string pythonScript = Path.Combine(Application.dataPath, "Editor", "mat-export.py");
        // string pythonScript = "mat-export.py"; // Adjust your Python script path if needed

        // Prepare the process start info
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo {
            FileName = blenderPath,
            Arguments = $"--background \"{blendFile}\" --python \"{pythonScript}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(blenderModelFile) // Set the working directory
        };

        using (var process = new System.Diagnostics.Process { StartInfo = startInfo }) {
            StringBuilder output = new StringBuilder();
            StringBuilder errorOutput = new StringBuilder();

            // Hook up output and error streams
            process.OutputDataReceived += (sender, args) => {
                if (!string.IsNullOrEmpty(args.Data)) {
                    output.AppendLine(args.Data);
                }
            };

            process.ErrorDataReceived += (sender, args) => {
                if (!string.IsNullOrEmpty(args.Data)) {
                    errorOutput.AppendLine(args.Data);
                }
            };

            process.Start();

            // Begin reading the output
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for the process to exit
            process.WaitForExit();

            // Get the output and error messages
            string outputString = output.ToString();
            string errorString = errorOutput.ToString();

            Debug.Log("Output: " + outputString);
            if (!string.IsNullOrEmpty(errorString)) {
                Debug.LogError("Error Output: " + errorString);
            }
        }

        AssetDatabase.Refresh();
    }

    private string GetBlenderPath() {
        #if UNITY_STANDALONE_OSX
            return "/Applications/Blender.app/Contents/MacOS/Blender";
        #elif UNITY_STANDALONE_WIN
            // Adjust the path as needed for Windows
            return @"C:\Program Files\Blender Foundation\Blender\blender.exe";
        #elif UNITY_STANDALONE_LINUX
            // Adjust the path as needed for Linux
            return "/usr/bin/blender";
        #else
            return null; // Unsupported platform
        #endif
    }

    private string GetExpectedJsonFilePath(GameObject model) {
        string modelPath = AssetDatabase.GetAssetPath(model);
        string modelName = Path.GetFileNameWithoutExtension(modelPath);
        string jsonFileName = $"{modelName}_materials_data.json";
        string jsonFilePath = Path.Combine(Path.GetDirectoryName(modelPath), jsonFileName);

        return jsonFilePath;
    }

    // Method to apply material remapping settings
    private void ApplyMaterialRemapSettings() {
        // Get the path to the model's asset
        string assetPath = AssetDatabase.GetAssetPath(selectedModel);
        if (string.IsNullOrEmpty(assetPath)) {
            Debug.LogError("No valid model selected!");
            return;
        }

        // Get the ModelImporter for the selected model
        ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (modelImporter == null) {
            Debug.LogError("Selected object is not a valid 3D model!");
            return;
        }

        // Apply the material remap settings
        // modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; // On Demand Remap
        // modelImporter.materialSearch = ModelImporterMaterialSearch.Local; // Search and Remap
        // modelImporter.materialLocation = ModelImporterMaterialLocation.InPrefab; // From Model's Material, Local Folder

        AssetDatabase.Refresh();

        modelImporter.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Local);

        // Save the changes and reimport the model to apply the settings
        AssetDatabase.WriteImportSettingsIfDirty(assetPath);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        Debug.Log($"Material remap settings applied to {selectedModel.name}.");
    }

    // to incorporate
    // https://discussions.unity.com/t/access-models-remapped-materials-through-code/217739/2
    // https://docs.unity3d.com/ScriptReference/AssetImporter.AddRemap.html

    private void ApplyTexturesToSelectedModel() {
        if (selectedModel == null) {
            Debug.LogError("No model selected.");
            return;
        }

        if (string.IsNullOrEmpty(jsonFilePath) || !File.Exists(jsonFilePath)) {
            Debug.LogError("Invalid JSON file path.");
            return;
        }

        string jsonData = File.ReadAllText(jsonFilePath);
        MaterialTextureData materialData = JsonUtility.FromJson<MaterialTextureData>(jsonData);
        if (materialData == null) {
            Debug.LogError("Failed to deserialize JSON data.");
            return;
        }

        string modelPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(selectedModel));
        string jsonPath = Path.GetDirectoryName(jsonFilePath);

        string materialsPath = Path.Combine(modelPath, "Materials");
        if (!AssetDatabase.IsValidFolder(materialsPath)) {
            Debug.Log($"Create 'Materials' folder in '{modelPath}' ('{selectedModel}' '{modelPath}')");
            AssetDatabase.CreateFolder(modelPath, "Materials");
        }

        Debug.Log("Material count: " + materialData.materials.Count);
        Renderer[] renderers = selectedModel.GetComponentsInChildren<Renderer>();

        Dictionary<string, string> texture_channel_mapping = new Dictionary<string, string>() {
            {"COLOR", "_BaseMap"}, // _MainTex, _BaseColor, _BaseColorMap, _Color
            {"METALNESS", "_MetallicGlossMap"},
            {"SPECULAR", "_SpecGlossMap"},
            {"NORMAL", "_BumpMap"},
            {"ROUGHNESS", "_MetallicGlossMap"}, // it's the same in URP?
            {"GLOSS", "_MetallicGlossMap"},
            {"OPACITY", "_OpacityMap"}, // dont overwrite _BaseMap
            {"OCCLUSION", "_OcclusionMap"},
            {"EMISSION", "_EmissionMap"},
            {"DISPLACEMENT", "_Height"},
            {"AMBIENT_OCCLUSION", "_Occlusion"},
        };

        for (int i = 0; i < materialData.materials.Count; i++) {
            MaterialTextureInfo materialInfo = materialData.materials[i];
            if (materialInfo != null) {
                // Create a copy of the material to apply changes
                string materialAssetPath = Path.Combine(materialsPath, materialInfo.materialName);
                Debug.Log($"Create {materialAssetPath}.mat ({materialsPath})");
                Material instanceMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(instanceMaterial, materialAssetPath+".mat");

                Debug.Log($"Created {AssetDatabase.GetAssetPath(instanceMaterial)}");

                AssetDatabase.WriteImportSettingsIfDirty(materialAssetPath);
                EditorUtility.SetDirty(instanceMaterial);
                AssetDatabase.SaveAssetIfDirty(instanceMaterial);
                AssetDatabase.Refresh();

                instanceMaterial.SetFloat("_Smoothness", materialInfo.roughness);

                if (true) {
                    foreach (var textureInfo in materialInfo.textureInfos) {
                        string textureAssetPath = Path.Combine(jsonPath, textureInfo.image_name);
                        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);

                        Debug.Log($"Setting {textureInfo.image_name} to {textureInfo.channel} w={texture.width}");

                        if (texture != null) {
                            string texture_channel = "";
                            texture_channel_mapping.TryGetValue(textureInfo.channel, out texture_channel);

                            if (!String.IsNullOrEmpty(texture_channel)) {
                                Debug.Log($"{materialInfo.materialName} {textureInfo.image_name} applying to {texture_channel} ({textureInfo.channel})");
                                instanceMaterial.SetTexture(texture_channel, texture);
                                
                                // set additional parameters
                                switch (textureInfo.channel) {
                                    case "OPACITY":
                                        instanceMaterial.SetFloat("_Mode", 2);
                                        break;
                                }
                            } else {
                                Debug.LogError($"{materialInfo.materialName} {textureInfo.image_name} Unknown channel type: {textureInfo.channel}");
                            }
                        }

                        else {
                            Debug.LogError($"Texture not found at path: {textureAssetPath}");
                        }
                    }

                    EditorUtility.SetDirty(instanceMaterial);
                    AssetDatabase.SaveAssetIfDirty(instanceMaterial);
                }

                EditorUtility.SetDirty(instanceMaterial);
                AssetDatabase.SaveAssetIfDirty(instanceMaterial);

                Debug.Log($"Reading='{instanceMaterial.name}' {texture_channel_mapping["COLOR"]}='{instanceMaterial.GetTexture(texture_channel_mapping["COLOR"])}'");
            }
        }

        Debug.Log("Textures applied to the selected model.");
    }
}
