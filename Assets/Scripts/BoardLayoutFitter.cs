using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class BoardLayoutFitter : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private int columns = 4;
    [SerializeField] private int rows = 4;
    [SerializeField] private Vector2 padding = new Vector2(16, 16);
    [SerializeField] private Vector2 spacing = new Vector2(12, 12);

    private GridLayoutGroup _grid;

    private void Awake()
    {
        _grid = GetComponent<GridLayoutGroup>();
        _grid.spacing = spacing;
        _grid.padding = new RectOffset((int)padding.x, (int)padding.x, (int)padding.y, (int)padding.y);
    }

    public void SetGrid(int cols, int rws)
    {
        columns = cols;
        rows = rws;
        Recalculate();
    }

    public void Recalculate()
    {
        if (_grid == null) _grid = GetComponent<GridLayoutGroup>();
        if (container == null) container = (RectTransform)transform;

        var rect = container.rect;
        if (rect.width <= 0 || rect.height <= 0) return;

        var w = rect.width - _grid.padding.left - _grid.padding.right - (_grid.spacing.x * (columns - 1));
        var h = rect.height - _grid.padding.top - _grid.padding.bottom - (_grid.spacing.y * (rows - 1));

        var cell = Mathf.Floor(Mathf.Min(w / columns, h / rows));
        _grid.cellSize = new Vector2(cell, cell);
    }


    private void OnRectTransformDimensionsChange() => Recalculate();
}
