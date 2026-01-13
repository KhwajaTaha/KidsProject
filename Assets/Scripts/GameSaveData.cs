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

    public List<string> matchedInstanceIds;
}
