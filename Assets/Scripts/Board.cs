using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public const int Size = 8;

    [SerializeField] private Cell cellPrefab;
    [SerializeField] private Transform cellsTransform;

    private readonly Cell[,] cells = new Cell[Size, Size];
    private readonly int[,] data = new int[Size, Size]; // 0 Emty, 1 Hover, 2 Normal

    private readonly List<Vector2Int> hoverPoints = new();
    private readonly List<int> highlightPolyominoColumns = new();
    private readonly List<int> highlightPolyominoRows = new();
    private readonly List<int> fullLineColumns = new();
    private readonly List<int> fullLineRows = new();

    private void Awake()
    {
        if (cellsTransform == null)
        {
            var existingCellsTransform = transform.Find("Cells");
            if (existingCellsTransform == null)
            {
                existingCellsTransform = new GameObject("Cells").transform;
                existingCellsTransform.SetParent(transform, false);
            }

            cellsTransform = existingCellsTransform;
        }
    }

    private void Start()
    {
        if (cellPrefab == null)
        {
            Debug.LogError("Board is missing a Cell prefab reference.", this);
            enabled = false;
            return;
        }

        for (var r = 0; r < Size; ++r)
        {
            for (var c = 0; c < Size; ++c)
            {
                cells[r, c] = Instantiate(cellPrefab, cellsTransform);
                cells[r, c].transform.position = new(c + 0.5f, r + 0.5f, 0.0f);
                cells[r, c].Hide();
            }
        }
    }

    public void Hover(Vector2Int point, int polyominoIndex)
    {
        var polyomino = Polyominos.Get(polyominoIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);

        UnHover();
        Unhighlight();

        highlightPolyominoColumns.Clear();
        highlightPolyominoRows.Clear();
        HoverPoints(point, polyominoRows, polyominoColumns, polyomino);
        if (hoverPoints.Count > 0)
        {
            Hover();
            Highlight(point, polyominoColumns, polyominoRows);

            foreach (var fullLineColumn in fullLineColumns)
            {
                highlightPolyominoColumns.Add(fullLineColumn - point.x);
            }
            foreach (var fullLineRow in fullLineRows)
            {
                highlightPolyominoRows.Add(fullLineRow - point.y);
            }
        }
    }

    private void HoverPoints(Vector2Int point, int polyominoRows, int polyominoColumns, int[,] polyomino)
    {
        for (var r = 0; r < polyominoRows; ++r)
        {
            for (var c = 0;c < polyominoColumns; ++c)
            {
                if (polyomino[r, c] > 0)
                {
                    var hoverPoint = point + new Vector2Int(c, r);
                    if (IsValidPoint(hoverPoint) == false)
                    {
                        hoverPoints.Clear();
                        return;
                    }

                    hoverPoints.Add(hoverPoint);
                }
            }
        }
    }

    private bool IsValidPoint(Vector2Int point)
    {
        if (point.x < 0 || point.x >= Size) return false;

        if (point.y < 0 || point.y >= Size) return false;

        if (data[point.y, point.x] > 0) return false;

        return true;
    }

    private void Hover()
    {
        foreach (var hoverPoint in hoverPoints)
        {
            data[hoverPoint.y, hoverPoint.x] = 1;
            cells[hoverPoint.y, hoverPoint.x].Hover();
        }
    }

    private void UnHover()
    {
        foreach (var hoverPoint in hoverPoints)
        {
            data[hoverPoint.y, hoverPoint.x] = 0;
            cells[hoverPoint.y, hoverPoint.x].Hide();
        }
        hoverPoints.Clear();
    }

    public bool Place(Vector2Int point, int polyominoIndex)
    {
        var polyomino = Polyominos.Get(polyominoIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);

        UnHover();
        HoverPoints(point, polyominoRows, polyominoColumns, polyomino);
        if (hoverPoints.Count > 0)
        {
            Place(point, polyominoColumns, polyominoRows);
            return true;
        }

        return false;
    }

    private void Place(Vector2Int point, int polyominoColumns, int polyominoRows)
    {
        foreach (var hoverPoint in hoverPoints)
        {
            data[hoverPoint.y, hoverPoint.x] = 2;
            cells[hoverPoint.y, hoverPoint.x].Normal();
        }

        ClearFullLines(point, polyominoColumns, polyominoRows);

        hoverPoints.Clear();
    }

    private void ClearFullLines(Vector2Int point, int polyominoColumns, int polyominoRows)
    {
        FullLineColumns(point.x, point.x + polyominoColumns);
        FullLineRows(point.y, point.y + polyominoRows);

        ClearFullLineColumns();
        ClearFullLineRows();

        var clearedLineCount = fullLineColumns.Count + fullLineRows.Count;
        if (clearedLineCount > 0 && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(clearedLineCount * ScoreManager.Instance.PointsPerClearedLine);
        }
    }

    private void FullLineColumns(int fromColumn, int toColumnExclusive)
    {
        fullLineColumns.Clear();
        for (var c = fromColumn; c < toColumnExclusive; ++c)
        {
            var isFullLine = true;
            for (var r = 0; r < Size; ++r)
            {
                if (data[r, c] != 2)
                {
                    isFullLine = false; 
                    break;
                }
            }
            if (isFullLine == true)
            {
                fullLineColumns.Add(c);
            }
        }
    }

    private void FullLineRows(int fromRow, int toRowExclusive)
    {
        fullLineRows.Clear();
        for (var r = fromRow; r < toRowExclusive; ++r)
        {
            var isFullLine = true;
            for (var c = 0; c < Size; ++c)
            {
                if (data[r, c] != 2)
                {
                    isFullLine = false;
                    break;
                }
            }
            if (isFullLine == true)
            {
                fullLineRows.Add(r);
            }
        }
    }

    private void ClearFullLineColumns()
    {
        foreach (var c in fullLineColumns)
        {
            for (var r = 0; r < Size; ++r)
            {
                data[r, c] = 0;
                cells[r, c].Hide();
            }
        }
    }
    private void ClearFullLineRows()
    {
        foreach (var r in fullLineRows)
        {
            for (var c = 0; c < Size; ++c)
            {
                data[r, c] = 0;
                cells[r, c].Hide();
            }
        }
    }

    private void Highlight(Vector2Int point, int polyominoColumns, int polyominoRows)
    {
        PredictFullLineColumns(point.x, point.x + polyominoColumns);
        PredictFullLineRows(point.y, point.y + polyominoRows);

        HighlightFullLineColumns();
        HighlightFullLineRows();
    }

    private void Unhighlight()
    {
        UnhighlightFullLineColumns();
        UnhighlightFullLineRows();
    }

    private void PredictFullLineColumns(int fromColumn, int toColumnExclusive)
    {
        fullLineColumns.Clear();
        for (var c = fromColumn; c < toColumnExclusive; ++c)
        {
            var isFullLine = true;
            for (var r = 0; r < Size; ++r)
            {
                if (data[r, c] != 1 && data[r, c] != 2)
                {
                    isFullLine = false;
                    break;
                }
            }
            if (isFullLine == true)
            {
                fullLineColumns.Add(c);
            }
        }
    }

    private void PredictFullLineRows(int fromRow, int toRowExclusive)
    {
        fullLineRows.Clear();
        for (var r = fromRow; r < toRowExclusive; ++r)
        {
            var isFullLine = true;
            for (var c = 0; c < Size; ++c)
            {
                if (data[r, c] != 1 && data[r, c] != 2)
                {
                    isFullLine = false;
                    break;
                }
            }
            if (isFullLine == true)
            {
                fullLineRows.Add(r);
            }
        }
    }
    private void HighlightFullLineColumns()
    {
        foreach (var c in fullLineColumns)
        {
            for (var r = 0; r < Size; ++r)
            {
                if (data[r, c] == 2)
                {
                    cells[r, c].Highlight();
                }
            }
        }
    }
    private void HighlightFullLineRows()
    {
        foreach (var r in fullLineRows)
        {
            for (var c = 0; c < Size; ++c)
            {
                if (data[r, c] == 2)
                {
                    cells[r, c].Highlight();
                }
            }
        }
    }
    private void UnhighlightFullLineColumns()
    {
        foreach (var c in fullLineColumns)
        {
            for (var r = 0; r < Size; ++r)
            {
                if (data[r, c] == 2)
                {
                    cells[r, c].Normal();
                }
            }
        }
    }
    private void UnhighlightFullLineRows()
    {
        foreach (var r in fullLineRows)
        {
            for (var c = 0; c < Size; ++c)
            {
                if (data[r, c] == 2)
                {
                    cells[r, c].Normal();
                }
            }
        }
    }

    public bool CheckPlace(int polyominoIndex)
    {
        var polyomino = Polyominos.Get(polyominoIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);
        
        for (var r = 0; r <= Size - polyominoRows; ++r)
        {
            for (var c = 0; c <= Size - polyominoColumns; ++c)
            {
                if (CheckPlace(c, r, polyominoColumns, polyominoRows, polyomino) == true)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool CheckPlace(int column, int row, int polyominoColumns, int polyominoRows, int[,] polyomino)
    {
        for (var r = 0; r < polyominoRows; ++r)
        {
            for (var c = 0; c < polyominoColumns; ++c)
            {
                if (polyomino[r, c] > 0 && data[row + r, column + c] == 2)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public List<int> HighlightPolyominoColumns => highlightPolyominoColumns;
    public List<int> HighlightPolyominoRows => highlightPolyominoRows;
}
