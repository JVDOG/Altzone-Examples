﻿using System.IO;
using UnityEditor;

namespace Editor.Prg.Util
{
    public static class CreateStandardFolders
    {
        [MenuItem("Window/ALT-Zone/Util/Create Project 'Standard' Folders")]
        private static void _CreateStandardFolders()
        {
            UnityEngine.Debug.Log("*");
            var activeObject = Selection.activeObject;
            if (activeObject == null || Selection.assetGUIDs.Length != 1)
            {
                UnityEngine.Debug.Log("No can do: Select a single FOLDER in 'Project' tab and 'Assets' Pane");
                return;
            }
            var selectedGuid = Selection.assetGUIDs[0];
            var parentFolder = AssetDatabase.GUIDToAssetPath(selectedGuid);
            if (!Directory.Exists(parentFolder))
            {
                UnityEngine.Debug.Log($"No can do: Selected object '{parentFolder}' is not a directory");
                return;
            }
            UnityEngine.Debug.Log($"Create UNITY standard folders for: '{parentFolder}'");

            var folders = new[]
            {
                "Animations",
                "Fonts",
                "Graphics",
                "Materials",
                "Models",
                "Prefabs",
                "Resources",
                "Scenes",
                "ScriptableObjects",
                "Scripts",
                "Sounds",
                "Textures"
            };
            foreach (var folder in folders)
            {
                var path = Path.Combine(parentFolder, folder);
                if (Directory.Exists(path) || File.Exists(path))
                {
                    UnityEngine.Debug.Log($"Skip: <color=white>{folder}</color>");
                    continue;
                }
                var guid = AssetDatabase.CreateFolder(parentFolder, folder);
                UnityEngine.Debug.Log($"Folder: <color=white>{folder}</color> created with guid {guid}");
            }
            UnityEngine.Debug.Log("Done");
        }
    }
}