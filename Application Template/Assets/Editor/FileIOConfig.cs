using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
internal class OpenFileIOConfig
{
    public const string MenuName = "Tools/IOModule Config";

    [MenuItem(MenuName, priority = (int)blu.EditorTools.EditorMenuItemPriority.IOModuleSettings)]
    private static void ButtonCall()
    {
        FileIOConfig window = (FileIOConfig)EditorWindow.GetWindow(typeof(FileIOConfig), false, "Save Editor");
    }
}

public class FileIOConfig : EditorWindow
{
    private bool disableSaving = false;
    private bool overrideLoad = false;
    private string filepath = "";

    private void OnEnable()
    {
        Load();
    }

    private void OnGUI()
    {
        if (Application.isPlaying)
            GUI.enabled = false;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Disable Saving", GUILayout.Width(120));
        disableSaving = blu.EditorTools.EditorUtil.BoolField(disableSaving);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Override File Load", GUILayout.Width(120));
        overrideLoad = blu.EditorTools.EditorUtil.BoolField(overrideLoad);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Filepath", GUILayout.Width(120));
        FindFile();
        EditorGUILayout.EndHorizontal();

        Buttons();
    }

    private void Load()
    {
        disableSaving = PlayerPrefs.GetInt("Editor_IOModuleConfig_DisableSaving") == 1;
        overrideLoad = PlayerPrefs.GetInt("Editor_IOModuleConfig_OverrideFileLoad") == 1;
        filepath = PlayerPrefs.GetString("Editor_IOModuleConfig_Filepath");

        blu.FileIO.BaseFileLoader<blu.FileIO.SaveData> fileloader = new blu.FileIO.JsonFileLoader<blu.FileIO.SaveData>(filepath);
        if (!fileloader.FileExists())
        {
            filepath = "";
            Save();
        }
    }

    private void Save()
    {
        filepath ??= "";

        PlayerPrefs.SetInt("Editor_IOModuleConfig_DisableSaving", disableSaving ? 1 : 0);
        PlayerPrefs.SetInt("Editor_IOModuleConfig_OverrideFileLoad", overrideLoad ? 1 : 0);
        PlayerPrefs.SetString("Editor_IOModuleConfig_Filepath", filepath);
        PlayerPrefs.Save();
    }

    private void FindFile()
    {
        filepath ??= "";
        string[] shortpath = filepath.Split('/');

        GUIContent content = new GUIContent(shortpath[shortpath.Length - 1], filepath);

        if (shortpath != null && shortpath.Length != 0)
            GUILayout.Label(content, EditorStyles.boldLabel, GUILayout.Width(100));

        // GUILayout.Label(filepath, EditorStyles.boldLabel);
        if (GUILayout.Button("Find", GUILayout.Width(100f)))
        {
            blu.FileIO.SaveData savedata = new blu.FileIO.SaveData();
            filepath = EditorUtility.OpenFilePanel("Debug Save File", Application.persistentDataPath, savedata.FileExtension());
        }
    }

    private void Buttons()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save", GUILayout.Width(80)))
        {
            Save();
        }

        if (GUILayout.Button("Reload", GUILayout.Width(80)))
        {
            Load();
        }

        GUILayout.EndHorizontal();
    }
}