namespace blu.FileIO
{
    [System.Serializable]
    public class SaveData : IFileFormat
    {
        public string FileExtension()
        { return "sv"; }

        public int slot = 0;
    }

    // constructed from a SaveData to hold basic info about a file without keeping the whole thing in memory
    public class StrippedSaveData
    {
        public StrippedSaveData(SaveData data, string filepath)
        {
            Filepath = filepath;
        }

        public string Filepath { get; protected set; }
    }
}