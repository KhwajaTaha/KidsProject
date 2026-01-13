using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    private const string FileName = "match_save.json";

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public void Save(BoardConfig config, GameSaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("SaveSystem.Save called with null data.");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveSystem.Save failed: {e}");
        }
    }
    public bool TryLoad(BoardConfig config, out GameSaveData data)
    {
        data = null;

        try
        {
            if (!File.Exists(SavePath))
                return false;

            string json = File.ReadAllText(SavePath);
            if (string.IsNullOrEmpty(json))
                return false;

            data = JsonUtility.FromJson<GameSaveData>(json);
            return data != null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveSystem.TryLoad failed: {e}");
            return false;
        }
    }
    public void Clear(BoardConfig config)
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveSystem.Clear failed: {e}");
        }
    }

    public bool HasSave()
    {
        return File.Exists(SavePath);
    }
}
