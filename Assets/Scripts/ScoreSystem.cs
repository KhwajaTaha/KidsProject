using UnityEngine;
public class ScoreSystem
{
    public int Score { get; private set; }
    public int Matches { get; private set; }
    public int Turns { get; private set; }
    public int Combo { get; private set; }

    public void Reset()
    {
        Score = 0;
        Matches = 0;
        Turns = 0;
        Combo = 0;
    }

    public void RegisterFlip() => Turns++;

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

    public void LoadFrom(int matches, int turns, int score, int combo)
    {
        Matches = matches;
        Turns = turns;
        Score = score;
        Combo = combo;
    }
}
