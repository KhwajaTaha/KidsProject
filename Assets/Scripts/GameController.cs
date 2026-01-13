using System.Collections;
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
    [SerializeField] private Transform boardParent;          
    [SerializeField] private BoardLayoutFitter layoutFitter; 
    [SerializeField] private MatchResolver resolver;         
    [SerializeField] private AudioManager audioManager;      
    [SerializeField] private HudView hud;                    
    [SerializeField] private SaveSystem saveSystem;          

    [Header("Win UI")]
    [SerializeField] private WinPanelView winPanelView;      

    [Header("Gameplay")]
    [SerializeField] private float flipDuration = 0.16f;

    [Header("Start Preview")]
    [SerializeField] private bool previewAllCardsOnNewGame = true;
    [SerializeField] private float previewDurationSeconds = 2f;
    [SerializeField] private float previewFlipDuration = 0.12f;

    private readonly List<CardView> _cards = new List<CardView>();
    private readonly List<CardView> _recentReveals = new List<CardView>(2);
    private ScoreSystem _score = new ScoreSystem();

    private bool _gameEnded;
    private bool _inputLocked;

    private int _currentCols;
    private int _currentRows;

    private Coroutine _previewRoutine;

    private void Awake()
    {
        if (resolver != null)
            resolver.OnPairResolved += HandlePairResolved;
        else
            Debug.LogError("GameController: Resolver reference is NULL (assign MatchResolver in Inspector).");
    }

    private void Start()
    {
        if (winPanelView != null)
        {
            winPanelView.Init(RestartGame);
            winPanelView.Hide();
        }

        NewOrLoadGame();
    }

    private void OnDestroy()
    {
        if (resolver != null)
            resolver.OnPairResolved -= HandlePairResolved;
    }

    public void NewOrLoadGame()
    {
        _gameEnded = false;
        _inputLocked = false;
        _recentReveals.Clear();

        if (_previewRoutine != null)
        {
            StopCoroutine(_previewRoutine);
            _previewRoutine = null;
        }

        if (winPanelView != null)
            winPanelView.Hide();


        GameSaveData save = null;
        bool loaded = false;

        if (saveSystem != null)
            loaded = saveSystem.TryLoad(boardConfig, out save);

        if (loaded && save != null && save.faceIdsByIndex != null && save.faceIdsByIndex.Count > 0)
        {
            _currentCols = Mathf.Max(1, save.layoutCols);
            _currentRows = Mathf.Max(1, save.layoutRows);

            BuildBoardFromLayout(_currentCols, _currentRows, save.faceIdsByIndex);
            ApplySave(save);
            UpdateHud();
            return;
        }

        _currentCols = Mathf.Max(1, boardConfig.columns);
        _currentRows = Mathf.Max(1, boardConfig.rows);

        BuildBoardRandom(_currentCols, _currentRows);

        _score.Reset();
        UpdateHud();

        if (previewAllCardsOnNewGame && previewDurationSeconds > 0f)
            _previewRoutine = StartCoroutine(PreviewAllCards(previewDurationSeconds));

        if (saveSystem != null)
            saveSystem.Save(boardConfig, BuildSave());
    }

    private void BuildBoardRandom(int cols, int rows)
    {
        ClearSpawnedCards();

        if (layoutFitter != null)
            layoutFitter.SetGrid(cols, rows);

        int total = cols * rows;
        if (total <= 0 || total % 2 != 0)
        {
            Debug.LogError($"Invalid board size {cols}x{rows}. Total must be positive and even.");
            return;
        }

        if (deck == null || deck.Count < total / 2)
        {
            Debug.LogError($"Not enough CardDefinitions. Need {total / 2}, have {deck?.Count ?? 0}.");
            return;
        }

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

    private void BuildBoardFromLayout(int cols, int rows, List<string> faceIdsByIndex)
    {
        ClearSpawnedCards();

        if (layoutFitter != null)
            layoutFitter.SetGrid(cols, rows);

        int total = cols * rows;
        if (faceIdsByIndex == null || faceIdsByIndex.Count != total)
        {
            Debug.LogError("Saved layout invalid (wrong count). Starting new random board instead.");
            BuildBoardRandom(cols, rows);
            return;
        }

        var lookup = new Dictionary<string, CardDefinition>();
        foreach (var def in deck)
        {
            if (def == null) continue;
            if (!lookup.ContainsKey(def.id))
                lookup.Add(def.id, def);
        }

        for (int i = 0; i < total; i++)
        {
            var faceId = faceIdsByIndex[i];

            if (!lookup.TryGetValue(faceId, out var def) || def == null)
            {
                Debug.LogError($"Missing CardDefinition for saved faceId '{faceId}'. Starting new random board instead.");
                BuildBoardRandom(cols, rows);
                return;
            }

            var card = Instantiate(cardPrefab, boardParent);
            card.Init(instanceId: i.ToString(), faceId: def.id, faceSprite: def.faceSprite);
            card.Clicked += OnCardClicked;
            _cards.Add(card);
        }
    }

    private void ClearSpawnedCards()
    {
        foreach (Transform child in boardParent)
            Destroy(child.gameObject);

        _cards.Clear();
        _recentReveals.Clear();
    }

    private void OnCardClicked(CardView card)
    {
        if (_inputLocked) return;
        if (_gameEnded) return;
        if (card == null) return;
        if (!card.CanInteract) return;
        if (card.State != CardView.CardState.FaceDown) return;

        _score.RegisterFlip();
        UpdateHud();

        card.Reveal(flipDuration);
        audioManager?.PlayFlip();

        _recentReveals.Add(card);

        if (_recentReveals.Count == 2)
        {
            resolver?.Enqueue(_recentReveals[0], _recentReveals[1]);
            _recentReveals.Clear();

            if (saveSystem != null)
                saveSystem.Save(boardConfig, BuildSave());
        }
    }

    private void HandlePairResolved(bool match)
    {
        if (_gameEnded) return;

        if (match)
        {
            _score.RegisterMatch();
            audioManager?.PlayMatch();
        }
        else
        {
            _score.RegisterMismatch();
            audioManager?.PlayMismatch();
        }

        UpdateHud();

        if (saveSystem != null)
            saveSystem.Save(boardConfig, BuildSave());

        CheckWin();
    }

    private void CheckWin()
    {
        if (_cards.Count == 0) return;

        for (int i = 0; i < _cards.Count; i++)
        {
            if (_cards[i].State != CardView.CardState.Matched)
                return;
        }

        _gameEnded = true;
        _inputLocked = true;

        audioManager?.PlayGameOver();

        if (winPanelView != null)
            winPanelView.Show();
    }

    private void UpdateHud()
    {
        if (hud == null) return;
        hud.SetAll(_score.Matches, _score.Turns, _score.Score, _score.Combo);
    }

    private GameSaveData BuildSave()
    {
        var data = new GameSaveData
        {
            layoutCols = _currentCols,
            layoutRows = _currentRows,
            matches = _score.Matches,
            turns = _score.Turns,
            score = _score.Score,
            combo = _score.Combo,
            faceIdsByIndex = new List<string>(_cards.Count),
            matchedIndices = new List<int>()
        };

        for (int i = 0; i < _cards.Count; i++)
        {
            data.faceIdsByIndex.Add(_cards[i].FaceId);

            if (_cards[i].State == CardView.CardState.Matched)
                data.matchedIndices.Add(i);
        }

        return data;
    }

    private void ApplySave(GameSaveData save)
    {
        if (save == null) return;

        _score.LoadFrom(save.matches, save.turns, save.score, save.combo);

        if (save.matchedIndices != null)
        {
            foreach (int idx in save.matchedIndices)
            {
                if (idx < 0 || idx >= _cards.Count) continue;
                _cards[idx].Reveal(0.01f);
                _cards[idx].SetMatched();
            }
        }
    }

    public void RestartGame()
    {
        _gameEnded = false;
        _inputLocked = false;

        if (_previewRoutine != null)
        {
            StopCoroutine(_previewRoutine);
            _previewRoutine = null;
        }

        if (winPanelView != null)
            winPanelView.Hide();

        resolver?.Clear();
        saveSystem?.Clear(boardConfig);

        _score.Reset();
        UpdateHud();

        NewOrLoadGame();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause && saveSystem != null && !_gameEnded)
            saveSystem.Save(boardConfig, BuildSave());
    }

    private void OnApplicationQuit()
    {
        if (saveSystem != null && !_gameEnded)
            saveSystem.Save(boardConfig, BuildSave());
    }

    private IEnumerator PreviewAllCards(float seconds)
    {
        _inputLocked = true;

        yield return null;

        // Reveal all
        for (int i = 0; i < _cards.Count; i++)
        {
            var c = _cards[i];
            if (c != null && c.State == CardView.CardState.FaceDown)
                c.Reveal(previewFlipDuration);
        }

        yield return new WaitForSecondsRealtime(seconds);

        for (int i = 0; i < _cards.Count; i++)
        {
            var c = _cards[i];
            if (c != null && c.State == CardView.CardState.FaceUp)
                c.Hide(previewFlipDuration);
        }

        _inputLocked = false;
        _previewRoutine = null;
    }
}
