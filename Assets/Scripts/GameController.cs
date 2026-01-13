using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Configs")]
    [SerializeField] private BoardConfig boardConfig;
    [SerializeField] private List<CardDefinition> deck = new List<CardDefinition>();
    [SerializeField] private CardView cardPrefab;

    [Header("Scene refs")]
    [SerializeField] private Transform boardParent;          // Board (has GridLayoutGroup)
    [SerializeField] private BoardLayoutFitter layoutFitter; // on Board
    [SerializeField] private MatchResolver resolver;         // on Game object
    [SerializeField] private AudioManager audioManager;      // on Game object
    [SerializeField] private HudView hud;                    // on LeftPanel (TMP version)
    [SerializeField] private SaveSystem saveSystem;          // on Game object

    [Header("Game Over UI (optional but recommended)")]
   // [SerializeField] private GameOverView gameOverView;      // panel script
   // [SerializeField] private CanvasGroup inputBlocker;       // optional full-screen blocker

    [Header("Gameplay")]
    [SerializeField] private float flipDuration = 0.16f;

    private readonly List<CardView> _cards = new List<CardView>();
    private readonly List<CardView> _recentReveals = new List<CardView>(2);

    private ScoreSystem _score = new ScoreSystem();
    private bool _gameEnded;

    private void Awake()
    {
        Debug.Log("GameController Awake");

        if (resolver == null)
            Debug.LogError("Resolver is NULL on GameController (Inspector reference missing).");
        else
            resolver.OnPairResolved += HandlePairResolved;
    }

    private void Start()
    {
       

        NewOrLoadGame();
        UpdateHud();
    }

    private void OnDestroy()
    {
        if (resolver != null)
            resolver.OnPairResolved -= HandlePairResolved;
    }

    public void NewOrLoadGame()
    {
#if UNITY_EDITOR
if (saveSystem != null)
{
    saveSystem.Clear(boardConfig);
}
#endif

        _gameEnded = false;
        _recentReveals.Clear();

        _score.Reset();   // ✅ RESET HERE

        GameSaveData save = null;
        bool loaded = false;

        if (saveSystem != null)
            loaded = saveSystem.TryLoad(boardConfig, out save);

        int cols = loaded ? save.layoutCols : boardConfig.columns;
        int rows = loaded ? save.layoutRows : boardConfig.rows;

        BuildBoard(cols, rows);

        if (loaded && save != null)
            _score.LoadFrom(save.matches, save.turns, save.score, save.combo);

        UpdateHud();
    }


    private void BuildBoard(int cols, int rows)
    {
        // Clean old
        foreach (Transform child in boardParent)
            Destroy(child.gameObject);

        _cards.Clear();

        if (layoutFitter != null)
            layoutFitter.SetGrid(cols, rows);

        int total = cols * rows;
        if (total <= 0 || total % 2 != 0)
        {
            Debug.LogError($"Invalid board size: {cols}x{rows}. Total must be positive and even.");
            return;
        }

        if (deck == null || deck.Count < total / 2)
        {
            Debug.LogError($"Not enough CardDefinitions in deck. Need at least {total / 2}, have {deck?.Count ?? 0}.");
            return;
        }

        // Pick N/2 unique defs, duplicate, shuffle
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
        if (_gameEnded) return;
        if (card == null) return;
        if (!card.CanInteract) return;
        if (card.State != CardView.CardState.FaceDown) return;

        // Count each flip as a turn
        _score.RegisterFlip();
        UpdateHud();

        // Reveal
        card.Reveal(flipDuration);
        if (audioManager != null) audioManager.PlayFlip();

        _recentReveals.Add(card);

        // When we have a pair, enqueue it (resolver handles delay/hide)
        if (_recentReveals.Count == 2)
        {
            if (resolver != null)
                resolver.Enqueue(_recentReveals[0], _recentReveals[1]);

            _recentReveals.Clear();

            // Save after forming a pair (optional, but nice)
            if (saveSystem != null)
                saveSystem.Save(boardConfig, BuildSave());
        }
    }

    private void HandlePairResolved(bool match)
    {
        Debug.Log($"HandlePairResolved called. match={match}");
        if (_gameEnded) return;

        if (match)
        {
            _score.RegisterMatch();
            if (audioManager != null) audioManager.PlayMatch();
        }
        else
        {
            _score.RegisterMismatch();
            if (audioManager != null) audioManager.PlayMismatch();
        }
        Debug.Log($"After resolve => Matches={_score.Matches} Score={_score.Score} Turns={_score.Turns}");
        UpdateHud();

        if (saveSystem != null)
            saveSystem.Save(boardConfig, BuildSave());

        CheckGameOver();
    }

    private void CheckGameOver()
    {
        if (_cards.Count == 0) return;

        for (int i = 0; i < _cards.Count; i++)
        {
            if (_cards[i].State != CardView.CardState.Matched)
                return;
        }

        // All matched
        _gameEnded = true;

        if (audioManager != null) audioManager.PlayGameOver();

       // if (inputBlocker != null)
       // {
            // If you want an invisible blocker, set alpha=0 but blocksRaycasts=true
          
     //   }

       // if (gameOverView != null)
          //  gameOverView.Show(_score.Matches, _score.Turns, _score.Score);
    }

    private void UpdateHud()
    {
        if (hud == null)
        {
            Debug.LogError("HUD is NULL on GameController (Inspector reference missing).");
            return;
        }

        Debug.Log("UpdateHud called");

        hud.SetAll(_score.Matches, _score.Turns, _score.Score);
    }

    private GameSaveData BuildSave()
    {
        // NOTE: This save only remembers which instanceIds are matched.
        // If you need full state (face positions) persist the shuffle seed or face ids by instance.
        return new GameSaveData
        {
            layoutCols = boardConfig.columns,
            layoutRows = boardConfig.rows,
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
        if (save == null) return;

        _score.LoadFrom(save.matches, save.turns, save.score, save.combo);

        if (save.matchedInstanceIds == null) return;

        var set = new HashSet<string>(save.matchedInstanceIds);
        foreach (var c in _cards)
        {
            if (!set.Contains(c.InstanceId)) continue;

            // Show quickly and mark matched
            c.Reveal(0.01f);
            c.SetMatched();
        }
    }

    public void RestartGame()
    {
        _gameEnded = false;

       

       

        if (saveSystem != null)
            saveSystem.Clear(boardConfig);

        _score.Reset();   // ✅ RESET HERE TOO
        UpdateHud();

        NewOrLoadGame();
    }


}
