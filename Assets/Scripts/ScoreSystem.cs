using UnityEngine;

public class ScoreSystem
{
    public int Score { get; private set; }
    public int Matches { get; private set; }
    public int Turns { get; private set; }
    public int Combo { get; private set; }

    public void RegisterTurn() => Turns++;

    public void RegisterMatch()
    {
        Matches++;
        Combo++;
        int basePts = 100;
        int comboBonus = Mathf.Clamp(Combo - 1, 0, 10) * 25;
        Score += basePts + comboBonus;
    }

    public void RegisterMismatch()
    {
        Combo = 0;
        Score = Mathf.Max(0, Score - 10);
    }
}
