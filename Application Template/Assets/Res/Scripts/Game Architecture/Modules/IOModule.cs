using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using blu.FileIO;

namespace blu
{
    public class IOModule : Module
    {
        public SaveData ActiveSaveData
        { get; set; }

        public const int MaxSaveFiles = 4;

        public StrippedSaveData[] SaveSlots
        { get; private set; }

        public string ApplicationPath
        { get; private set; }

        private StrippedSaveData m_activeStrippedSaveData = null;
        private const string m_kSaveGameDir = "SavedGames/";

        public bool IsSaveLoading
        { get; private set; }

        public bool IsSaving
        { get; private set; }

        public bool IsSaveLoaded
        { get { return ActiveSaveData != null; } }

        public bool DisableSaving
        { get; private set; }

        public bool OverrideLoad
        { get; private set; }

        private string DebugFilepath
        { get; set; }

        public override void Initialize()
        {
            IsSaveLoading = false;
            IsSaving = false;

            Debug.Log("[IOModule] Initializing IO module");
            ApplicationPath = Application.persistentDataPath;

            LoadDebugSettings();

            SaveSlots = new StrippedSaveData[MaxSaveFiles];
            LoadSaveSlots();
        }

        public Task AwaitSaveLoaded() => Task.Run(() => AwaitSaveLoadedImpl());

        private void AwaitSaveLoadedImpl()
        {
            while (!IsSaveLoaded) { }
        }

        // Public Functions ////////////////////////////////////////////////////////////////////////

        public bool Reload(bool logToConsole = true)
        {
            Debug.Log($"[IOModule] Reloading IOModule");
            DiscardActiveSaveData();
            SaveSlots = new StrippedSaveData[MaxSaveFiles];
            return LoadSaveSlots();
        }

        public bool Save()
        {
            if (DisableSaving)
                return false;

            if (IsSaving)
                return false;

            IsSaving = true;
            bool success = false;

            if (m_activeStrippedSaveData != null && m_activeStrippedSaveData.Filepath != null && ActiveSaveData != null)
            {
                BaseFileLoader<SaveData> fileloader = CreateFileLoader<SaveData>(m_activeStrippedSaveData.Filepath);
                success = fileloader.WriteData(ActiveSaveData);
            }

            IsSaving = false;
            return success;
        }

        public bool CreateNewSave(int slot, bool loadSave, bool allowOverride = false)
        {
            if (loadSave)
                DiscardActiveSaveData();

            if (slot >= MaxSaveFiles)
                return false;

            if (SaveSlots[slot] != null)
                return false;

            System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            int cur_time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;

            SaveData savedata = new SaveData();
            savedata.slot = slot;

            string filename = m_kSaveGameDir + cur_time.ToString();
            string filepath = ApplicationPath + "/" + filename + "." + savedata.FileExtension();
            BaseFileLoader<SaveData> fileloader = CreateFileLoader<SaveData>(filepath);

            if (fileloader.FileExists())
            {
                if (allowOverride)
                {
                    if (!fileloader.DeleteFile())
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (!fileloader.WriteData(savedata))
                return false;

            SaveSlots[slot] = new StrippedSaveData(savedata, filepath);

            if (loadSave)
            {
                ActiveSaveData = savedata;
                m_activeStrippedSaveData = SaveSlots[slot];
            }

            return true;
        }

        public bool LoadSave(StrippedSaveData slotData, bool logToConsole = true)
        {
            if (slotData == null)
                return false;

            if (IsSaveLoading)
                return false;

            IsSaveLoading = true;

            DiscardActiveSaveData();

            m_activeStrippedSaveData = slotData;
            ActiveSaveData = null;

            BaseFileLoader<SaveData> fileloader = CreateFileLoader<SaveData>(m_activeStrippedSaveData.Filepath);
            SaveData savedata = fileloader.ReadData();

            if (savedata == null)
            {
                m_activeStrippedSaveData = null;
                IsSaveLoading = false;
                return false;
            }
            ActiveSaveData = savedata;

            if (logToConsole)
                Debug.Log($"[IOModule] Save file loaded: [File = {m_activeStrippedSaveData.Filepath}]");

            IsSaveLoading = false;
            return true;
        }

        public bool DeleteSave(StrippedSaveData slotData, bool logToConsole = true)
        {
            BaseFileLoader<SaveData> fileloader = CreateFileLoader<SaveData>(slotData.Filepath);

            // we use filepath instead of reference in case a copy has been made of the StrippedSaveData

            if (slotData.Filepath == m_activeStrippedSaveData.Filepath)
                DiscardActiveSaveData();

            for (int i = 0; i < SaveSlots.Length; i++)
            {
                if (SaveSlots[i].Filepath == slotData.Filepath)
                {
                    SaveSlots[i] = null;
                    break;
                }
            }

            bool success = fileloader.DeleteFile();

            if (logToConsole)
            {
                if (success)
                    Debug.Log($"[IOModule] Save file deleted: [File = {slotData.Filepath}]");
                else
                    Debug.LogWarning($"[IOModule] Failed to delete save file: [File = {slotData.Filepath}]");
            }

            return success;
        }

        // Public Async Functions //////////////////////////////////////////////////////////////////
        public Task<bool> SaveAsync() => Task.Run(() => Save());

        public Task<bool> CreateNewSaveAsync(int slot, bool loadSave) => Task.Run(() => CreateNewSave(slot, loadSave));

        public Task<bool> LoadSaveAsync(StrippedSaveData slotData, bool logToConsole = true) => Task.Run(() => LoadSave(slotData, logToConsole));

        public Task<bool> DeleteSaveAsync(StrippedSaveData slotData, bool logToConsole = true) => Task.Run(() => DeleteSave(slotData, logToConsole));

        // Internal Methods ////////////////////////////////////////////////////////////////////////

        private void LoadDebugSettings()
        {
            if (Application.isEditor)
            {
                DisableSaving = PlayerPrefs.GetInt("Editor_IOModuleConfig_DisableSaving") == 1;
                OverrideLoad = PlayerPrefs.GetInt("Editor_IOModuleConfig_OverrideFileLoad") == 1;
                DebugFilepath = PlayerPrefs.GetString("Editor_IOModuleConfig_Filepath");

                blu.FileIO.BaseFileLoader<blu.FileIO.SaveData> fileloader = new blu.FileIO.JsonFileLoader<blu.FileIO.SaveData>(DebugFilepath);
                if (!fileloader.FileExists())
                {
                    DebugFilepath = "";
                    OverrideLoad = false;
                }
            }
        }

        private void DiscardActiveSaveData()
        {
            ActiveSaveData = null;
            m_activeStrippedSaveData = null;
        }

        private List<string> GetAllFilesOfType<T>(string directory = "") where T : IFileFormat, new()
        {
            string dir = ApplicationPath + "/" + directory;

            string[] files = FileLoaderStaticUtility.GetFilesInDirectory(dir);
            List<string> ret = new List<string>();

            IFileFormat format = new T();
            string extension = format.FileExtension();

            for (int i = 0; i < files.Length; i++)
            {
                // if extension is correct add it to list
                string[] fileSplit = files[i].Split('.');
                if (fileSplit[fileSplit.Length - 1] == extension)
                {
                    ret.Add(files[i]);
                }
            }

            return ret;
        }

        private BaseFileLoader<T> CreateFileLoader<T>(string path) where T : class, IFileFormat
        {
            return new JsonFileLoader<T>(path);
        }

        private bool LoadSaveSlots()
        {
            for (int i = SaveSlots.Length - 1; i >= 0; i--)
            {
                SaveSlots[i] = null;
            }

            FileLoaderStaticUtility.CreateDirectory(ApplicationPath + "/" + m_kSaveGameDir);

            if (OverrideLoad)
            {
                BaseFileLoader<SaveData> fileloader = CreateFileLoader<SaveData>(DebugFilepath);
                SaveData savedata = fileloader.ReadData();

                if (savedata == null)
                    return false;

                SaveSlots[0] = new StrippedSaveData(savedata, DebugFilepath);
                return true;
            }

            try
            {
                List<string> files = GetAllFilesOfType<SaveData>(m_kSaveGameDir);

                while (files.Count > MaxSaveFiles)
                {
                    files.RemoveAt(files.Count - 1);
                }

                for (int i = 0; i < files.Count; i++)
                {
                    if (files[i] == null)
                        continue;

                    BaseFileLoader<SaveData> fileloader = CreateFileLoader<SaveData>(files[i]);
                    SaveData savedata = fileloader.ReadData();

                    if (savedata == null)
                        continue;

                    StrippedSaveData data = new StrippedSaveData(savedata, files[i]);

                    if (savedata == null)
                    {
                        Debug.LogWarning($"[IOModule] could not load savedata");
                    }

                    int slot = savedata.slot;
                    int startSlot = slot;

                    while (true)
                    {
                        if (slot >= SaveSlots.Length)
                            slot = 0;

                        if (SaveSlots[slot] == null)
                        {
                            SaveSlots[slot] = data;
                            break;
                        }

                        slot++;

                        if (slot == startSlot)
                        {
                            Debug.LogWarning("[IOModule] error assigning savedata to save slot");
                            break;
                        }
                    }

                    if (slot != startSlot)
                    {
                        savedata.slot = slot;
                        fileloader.WriteData(savedata); // set new slot
                    }
                }
            }
            catch
            {
                Debug.LogWarning("[IOModule]: Error while loading Save Slots");
                return false;
            }

            return true;
        }
    }
}