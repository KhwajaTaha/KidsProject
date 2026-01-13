using UnityEngine;
using TMPro;

public class HudView : MonoBehaviour
{
    [Header("TextMeshProUGUI refs")]
    [SerializeField] private TMP_Text matchesText;
    [SerializeField] private TMP_Text turnsText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;

    public void SetMatches(int value)
    {
        if (matchesText == null) return;
        matchesText.text = $"Matches:\n{value}";
    }

    public void SetTurns(int value)
    {
        if (turnsText == null) return;
        turnsText.text = $"Turns:\n{value}";
    }

    public void SetScore(int value)
    {
        if (scoreText == null) return;
        scoreText.text = $"Score:\n{value}";
    }

    public void SetCombo(int value)
    {
        if (comboText == null) return;

        
            comboText.text = $"Combo: x{value}";
    }


    public void SetAll(int matches, int turns, int score, int combo)
    {
        SetMatches(matches);
        SetTurns(turns);
        SetScore(score);
        SetCombo(combo);
    }
}
