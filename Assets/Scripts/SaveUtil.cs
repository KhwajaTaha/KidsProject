using System.IO;
using UnityEngine;

public static class SaveUtil
{
    private const string FileName = "match_save.json";

    public static bool HasSave()
    {
        string path = Path.Combine(Application.persistentDataPath, FileName);
        return File.Exists(path) && new FileInfo(path).Length > 0;
    }

    public static void DeleteSave()
    {
        string path = Path.Combine(Application.persistentDataPath, FileName);
        if (File.Exists(path)) File.Delete(path);
    }
}
