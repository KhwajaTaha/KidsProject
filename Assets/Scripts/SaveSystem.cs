using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    private string PathFor(BoardConfig config)
        => System.IO.Path.Combine(Application.persistentDataPath, "match_save.json");

    public void Save(BoardConfig config, GameSaveData data)
    {
        try
        {
            var json = JsonUtility.ToJson(data, prettyPrint: false);
            File.WriteAllText(PathFor(config), json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Save failed: {e.Message}");
        }
    }

    public bool TryLoad(BoardConfig config, out GameSaveData data)
    {
        data = null;
        try
        {
            var path = PathFor(config);
            if (!File.Exists(path)) return false;

            var json = File.ReadAllText(path);
            data = JsonUtility.FromJson<GameSaveData>(json);
            return data != null;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Load failed: {e.Message}");
            return false;
        }
    }

    public void Clear(BoardConfig config)
    {
        var path = PathFor(config);
        if (File.Exists(path)) File.Delete(path);
    }
}
