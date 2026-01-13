using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Configs")]
    [SerializeField] private BoardConfig boardConfig;
    [SerializeField] private List<CardDefinition> deck;  
    [SerializeField] private CardView cardPrefab;

    [Header("Scene refs")]
    [SerializeField] private Transform boardParent;
    [SerializeField] private BoardLayoutFitter layoutFitter;
    [SerializeField] private MatchResolver resolver;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private HudView hud;
    [SerializeField] private SaveSystem saveSystem;

    private readonly List<CardView> _cards = new List<CardView>();
    private readonly List<CardView> _recentReveals = new List<CardView>(2);
    private ScoreSystem _score = new ScoreSystem();

    private void Awake()
    {
        resolver.OnPairResolved += HandlePairResolved;
    }

    private void Start()
    {
        NewOrLoadGame();
    }

    public void NewOrLoadGame()
    {
        var loaded = saveSystem.TryLoad(boardConfig, out var save);
        BuildBoard(save?.layoutCols ?? boardConfig.columns, save?.layoutRows ?? boardConfig.rows);

        if (loaded)
        {
            ApplySave(save);
        }

        RefreshHud();
    }

    private void BuildBoard(int cols, int rows)
    {
        foreach (Transform child in boardParent) Destroy(child.gameObject);
        _cards.Clear();
        _recentReveals.Clear();

        layoutFitter.SetGrid(cols, rows);

        int total = cols * rows;
        if (total % 2 != 0) Debug.LogError("Board needs even number of cards.");

        var chosen = deck.OrderBy(_ => Random.value).Take(total / 2).ToList();
        var faces = chosen.Concat(chosen).OrderBy(_ => Random.value).ToList();

        for (int i = 0; i < total; i++)
        {
            var def = faces[i];
            var card = Instantiate(cardPrefab, boardParent);
            card.Init(instanceId: i.ToString(), faceId: def.id, faceSprite: def.faceSprite);
            card.Clicked += OnCardClicked;
            _cards.Add(card);
        }
    }

    private void OnCardClicked(CardView card)
    {
        if (!card.CanInteract) return;
        if (card.State != CardView.CardState.FaceDown) return;

        card.Reveal();
        audioManager.PlayFlip();

        _recentReveals.Add(card);
        if (_recentReveals.Count == 2)
        {
            _score.RegisterTurn();
            resolver.Enqueue(_recentReveals[0], _recentReveals[1]);
            _recentReveals.Clear();
            RefreshHud();
            saveSystem.Save(boardConfig, BuildSave());
        }
    }

    private void HandlePairResolved(bool match)
    {
        if (match)
        {
            _score.RegisterMatch();
            audioManager.PlayMatch();
        }
        else
        {
            _score.RegisterMismatch();
            audioManager.PlayMismatch();
        }

        RefreshHud();
        saveSystem.Save(boardConfig, BuildSave());

        if (_cards.All(c => c.State == CardView.CardState.Matched))
        {
            audioManager.PlayGameOver();
        }
    }

    private void RefreshHud()
    {
        hud.SetMatches(_score.Matches);
        hud.SetTurns(_score.Turns);
        hud.SetScore(_score.Score);
        hud.SetCombo(_score.Combo);
    }

    private GameSaveData BuildSave()
    {
        return new GameSaveData
        {
            layoutCols = layoutFitter != null ? boardConfig.columns : boardConfig.columns,
            layoutRows = layoutFitter != null ? boardConfig.rows : boardConfig.rows,
            matches = _score.Matches,
            turns = _score.Turns,
            score = _score.Score,
            combo = _score.Combo,
            matchedInstanceIds = _cards
                .Where(c => c.State == CardView.CardState.Matched)
                .Select(c => c.InstanceId)
                .ToList()
        };
    }

    private void ApplySave(GameSaveData save)
    {
        _score = new ScoreSystem();
        for (int i = 0; i < save.turns; i++) _score.RegisterTurn();

        var set = new HashSet<string>(save.matchedInstanceIds);
        foreach (var c in _cards)
        {
            if (set.Contains(c.InstanceId))
            {
                c.Reveal(0.01f);
                c.SetMatched();
            }
        }
    }
}
