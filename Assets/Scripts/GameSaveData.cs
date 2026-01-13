using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public int layoutCols;
    public int layoutRows;

    public int matches;
    public int turns;
    public int score;
    public int combo;

    public List<string> faceIdsByIndex = new List<string>();

    public List<int> matchedIndices = new List<int>();
}
