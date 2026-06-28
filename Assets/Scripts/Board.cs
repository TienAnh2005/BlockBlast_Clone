using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public const int Size = 8;

    [SerializeField] private Cell cellPrefab;
    [SerializeField] private Transform cellsTransform;

    [Header("Special Block Visuals")]
    [SerializeField] private Sprite iceBlockSprite;
    [SerializeField] private Sprite bombBlockSprite;
    [SerializeField, Min(0.1f)] private float iceBlockScaleMultiplier = 0.8f;
    [SerializeField, Min(0.1f)] private float bombBlockScaleMultiplier = 0.8f;

    private readonly Cell[,] cells = new Cell[Size, Size];
    private readonly int[,] data = new int[Size, Size]; // 0 Emty, 1 Hover, 2 Normal
    private readonly BlockType[,] blockTypes = new BlockType[Size, Size];
    private readonly int[,] iceLayers = new int[Size, Size];

    private readonly List<Vector2Int> hoverPoints = new();
    private readonly List<int> highlightPolyominoColumns = new();
    private readonly List<int> highlightPolyominoRows = new();
    private readonly List<int> fullLineColumns = new();
    private readonly List<int> fullLineRows = new();

    private bool forcedGameOver;

    public bool HasForcedGameOver => forcedGameOver;

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
            AudioSettingsManager.Instance?.PlayPlaceSound();
            return true;
        }

        return false;
    }

    private void Place(Vector2Int point, int polyominoColumns, int polyominoRows)
    {
        foreach (var hoverPoint in hoverPoints)
        {
            SetOccupiedCell(hoverPoint.y, hoverPoint.x, BlockType.Normal);
        }

        ClearFullLines(point, polyominoColumns, polyominoRows);

        hoverPoints.Clear();
    }

    private void ClearFullLines(Vector2Int point, int polyominoColumns, int polyominoRows)
    {
        FullLineColumns(point.x, point.x + polyominoColumns);
        FullLineRows(point.y, point.y + polyominoRows);

        var clearedLineCount = fullLineColumns.Count + fullLineRows.Count;
        if (clearedLineCount <= 0)
        {
            return;
        }

        if (TriggeredBombLine())
        {
            AudioSettingsManager.Instance?.PlayBombSound();
            forcedGameOver = true;
            return;
        }

        ClearTriggeredCells();
        AudioSettingsManager.Instance?.PlayClearSound();

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
                ClearCell(r, c);
            }
        }
    }
    private void ClearFullLineRows()
    {
        foreach (var r in fullLineRows)
        {
            for (var c = 0; c < Size; ++c)
            {
                ClearCell(r, c);
            }
        }
    }

    private bool TriggeredBombLine()
    {
        foreach (var column in fullLineColumns)
        {
            if (LineContainsBomb(column, isColumn: true))
            {
                return true;
            }
        }

        foreach (var row in fullLineRows)
        {
            if (LineContainsBomb(row, isColumn: false))
            {
                return true;
            }
        }

        return false;
    }

    private bool LineContainsBomb(int lineIndex, bool isColumn)
    {
        for (var offset = 0; offset < Size; ++offset)
        {
            var row = isColumn ? offset : lineIndex;
            var column = isColumn ? lineIndex : offset;
            if (data[row, column] == 2 && blockTypes[row, column] == BlockType.Bomb)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearTriggeredCells()
    {
        var clearedPoints = new HashSet<Vector2Int>();

        foreach (var column in fullLineColumns)
        {
            for (var row = 0; row < Size; ++row)
            {
                clearedPoints.Add(new Vector2Int(column, row));
            }
        }

        foreach (var row in fullLineRows)
        {
            for (var column = 0; column < Size; ++column)
            {
                clearedPoints.Add(new Vector2Int(column, row));
            }
        }

        foreach (var point in clearedPoints)
        {
            ResolveClearedCell(point.y, point.x);
        }
    }

    private void ResolveClearedCell(int row, int column)
    {
        if (data[row, column] != 2)
        {
            return;
        }

        if (blockTypes[row, column] == BlockType.Ice && iceLayers[row, column] > 1)
        {
            iceLayers[row, column] -= 1;
            blockTypes[row, column] = BlockType.Normal;
            RefreshOccupiedCellVisual(row, column);
            return;
        }

        ClearCell(row, column);
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
                    RefreshOccupiedCellVisual(r, c);
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
                    RefreshOccupiedCellVisual(r, c);
                }
            }
        }
    }

    public void ClearBoardState()
    {
        hoverPoints.Clear();
        highlightPolyominoColumns.Clear();
        highlightPolyominoRows.Clear();
        fullLineColumns.Clear();
        fullLineRows.Clear();
        forcedGameOver = false;

        for (var r = 0; r < Size; ++r)
        {
            for (var c = 0; c < Size; ++c)
            {
                ClearCell(r, c);
            }
        }
    }

    public bool TryPrePlaceCell(Vector2Int point)
    {
        return TryPrePlaceCell(point, BlockType.Normal);
    }

    public bool TryPrePlaceCell(Vector2Int point, BlockType blockType)
    {
        if (point.x < 0 || point.x >= Size) return false;
        if (point.y < 0 || point.y >= Size) return false;
        if (data[point.y, point.x] != 0) return false;

        SetOccupiedCell(point.y, point.x, blockType);
        return true;
    }

    public bool TryPrePlacePolyomino(Vector2Int origin, int polyominoIndex)
    {
        var polyomino = Polyominos.Get(polyominoIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);
        var points = new List<Vector2Int>();

        for (var r = 0; r < polyominoRows; ++r)
        {
            for (var c = 0; c < polyominoColumns; ++c)
            {
                if (polyomino[r, c] <= 0)
                {
                    continue;
                }

                var point = origin + new Vector2Int(c, r);
                if (point.x < 0 || point.x >= Size) return false;
                if (point.y < 0 || point.y >= Size) return false;
                if (data[point.y, point.x] != 0) return false;

                points.Add(point);
            }
        }

        foreach (var point in points)
        {
            SetOccupiedCell(point.y, point.x, BlockType.Normal);
        }

        return true;
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

    private void SetOccupiedCell(int row, int column, BlockType blockType)
    {
        data[row, column] = 2;
        blockTypes[row, column] = blockType;
        iceLayers[row, column] = blockType == BlockType.Ice ? 2 : 0;
        RefreshOccupiedCellVisual(row, column);
    }

    private void RefreshOccupiedCellVisual(int row, int column)
    {
        if (cells[row, column] == null)
        {
            return;
        }

        cells[row, column].ShowPlaced(
            blockTypes[row, column],
            iceBlockSprite,
            bombBlockSprite,
            GetScaleMultiplier(blockTypes[row, column]));
    }

    private void ClearCell(int row, int column)
    {
        data[row, column] = 0;
        blockTypes[row, column] = BlockType.Normal;
        iceLayers[row, column] = 0;

        if (cells[row, column] != null)
        {
            cells[row, column].Hide();
        }
    }

    private float GetScaleMultiplier(BlockType blockType)
    {
        switch (blockType)
        {
            case BlockType.Ice:
                return iceBlockScaleMultiplier;
            case BlockType.Bomb:
                return bombBlockScaleMultiplier;
            default:
                return 1.0f;
        }
    }

    public int[,] GetOccupiedGridCopy()
    {
        var copy = new int[Size, Size];
        for (var r = 0; r < Size; ++r)
        {
            for (var c = 0; c < Size; ++c)
            {
                copy[r, c] = data[r, c] == 2 ? 1 : 0;
            }
        }

        return copy;
    }

    public List<int> HighlightPolyominoColumns => highlightPolyominoColumns;
    public List<int> HighlightPolyominoRows => highlightPolyominoRows;
}
