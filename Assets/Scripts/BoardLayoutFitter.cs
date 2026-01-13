using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public class BoardLayoutFitter : MonoBehaviour
{
    [Header("Grid Size")]
    [Min(1)][SerializeField] private int columns = 4;
    [Min(1)][SerializeField] private int rows = 4;

    [Header("Sizing Source")]
    [Tooltip("If null, uses this RectTransform.")]
    [SerializeField] private RectTransform container;

    [Header("Layout")]
    [SerializeField] private Vector2 spacing = new Vector2(12, 12);
    [SerializeField] private Vector2 padding = new Vector2(16, 16);

    private GridLayoutGroup _grid;
    private RectTransform _selfRect;
    private Coroutine _co;

    private void Awake()
    {
        _grid = GetComponent<GridLayoutGroup>();
        _selfRect = GetComponent<RectTransform>();
        ApplyStaticGridSettings();
    }

    private void OnEnable()
    {
        _grid = GetComponent<GridLayoutGroup>();
        _selfRect = GetComponent<RectTransform>();
        ApplyStaticGridSettings();

        if (Application.isPlaying)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(DelayedRecalc());
        }
        else
        {
            Recalculate();
        }
    }

    private void OnDisable()
    {
        if (_co != null) StopCoroutine(_co);
        _co = null;
    }

    public void SetGrid(int cols, int rws)
    {
        columns = Mathf.Max(1, cols);
        rows = Mathf.Max(1, rws);
        ApplyStaticGridSettings();
        if (Application.isPlaying)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(DelayedRecalc());
        }
        else
        {
            Recalculate();
        }
    }

    private IEnumerator DelayedRecalc()
    {
        yield return null;
        yield return null;
        Recalculate();
        _co = null;
    }

    private void ApplyStaticGridSettings()
    {
        if (_grid == null) _grid = GetComponent<GridLayoutGroup>();

        _grid.spacing = spacing;
        _grid.padding = new RectOffset((int)padding.x, (int)padding.x, (int)padding.y, (int)padding.y);

        _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _grid.constraintCount = columns;
        _grid.childAlignment = TextAnchor.MiddleCenter;
    }

    public void Recalculate()
    {
        if (_grid == null) _grid = GetComponent<GridLayoutGroup>();
        if (_selfRect == null) _selfRect = GetComponent<RectTransform>();

        var source = container != null ? container : _selfRect;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(source);

        var rect = source.rect;
        if (rect.width <= 1f || rect.height <= 1f) return; // still not ready

        float usableW = rect.width - _grid.padding.left - _grid.padding.right - _grid.spacing.x * (columns - 1);
        float usableH = rect.height - _grid.padding.top - _grid.padding.bottom - _grid.spacing.y * (rows - 1);

        float cell = Mathf.Floor(Mathf.Min(usableW / columns, usableH / rows));
        cell = Mathf.Max(1f, cell);

        _grid.cellSize = new Vector2(cell, cell);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled) return;

        if (Application.isPlaying)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(DelayedRecalc());
        }
        else
        {
            Recalculate();
        }
    }
}
