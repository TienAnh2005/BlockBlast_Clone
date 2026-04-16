using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Block : MonoBehaviour
{
    public const int Size = 5;

    private readonly Vector3 inputOffset =  new(0.0f, 2.0f, 0.0f);

    [SerializeField] private Board board;
    [SerializeField] private Blocks blocks;
    [SerializeField] private Cell cellPrefab;

    private SortingGroup sortingGroup;

    private int polyominoIndex;

    private readonly Cell[,] cells = new Cell[Size, Size];

    private Vector3 position;
    private Vector3 scale;
    private Vector2 inputPoint;

    private Vector3 previousMousePosition = Vector3.positiveInfinity;
    private Vector2Int previousDragPoint;
    private Vector2Int currentDragPoint;

    // Cache
    private Camera mainCamera;

    private Vector2 center;

    private void Awake()
    {
        sortingGroup = gameObject.GetComponent<SortingGroup>();
        mainCamera = Camera.main;
    }
    public void Initialize()
    {
        if (cellPrefab == null)
        {
            Debug.LogError($"{name} is missing a Cell prefab reference.", this);
            enabled = false;
            return;
        }

        for (var r = 0; r < Size; ++r)
        {
            for (var c = 0; c < Size; ++c)
            {
                cells[r, c] = Instantiate(cellPrefab, transform);
            }
        }

        position = transform.localPosition;
        scale = transform.localScale;
    }

    public void Show(int polyominoIndex)
    {
        this.polyominoIndex = polyominoIndex;

        Hide();

        var polyomino = Polyominos.Get(polyominoIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);
        center = new Vector2(polyominoColumns * 0.5f, polyominoRows * 0.5f);

        for (var r = 0; r < polyominoRows; ++r)
        {
            for (var c = 0; c < polyominoColumns; ++c)
            {
                if (polyomino[r, c] > 0)
                {
                    cells[r, c].transform.localPosition = new(c - center.x + 0.5f, r - center.y + 0.5f, 0.0f);
                    cells[r, c].Normal();
                }
            }
        }
    }

    private void Hide()
    {
        for (var r = 0; r < Size; ++r)
        {
            for (var c = 0; c < Size; ++c)
            {
                cells[r, c].Hide();
            }
        }
    }

    private void OnMouseDown()
    {

        inputPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        transform.localPosition = position + inputOffset;
        transform.localScale = Vector3.one;

        blocks.ResetBlocksSortingOrders();
        SetSortingOrder(1);

        currentDragPoint = Vector2Int.RoundToInt((Vector2)transform.position - center);
        board.Hover(currentDragPoint, polyominoIndex);
        Highlight(board.HighlightPolyominoColumns, board.HighlightPolyominoRows);
        previousDragPoint = currentDragPoint;

        previousMousePosition = Input.mousePosition;
    }
    private void OnMouseDrag()
    {
        var currentMousePosition = Input.mousePosition;
        if (currentMousePosition != previousMousePosition)
        {
            previousMousePosition = currentMousePosition;

            var inputDelta = (Vector2)mainCamera.ScreenToWorldPoint(Input.mousePosition) - inputPoint;
            transform.localPosition = position + inputOffset + (Vector3)inputDelta*1.4f;

            currentDragPoint = Vector2Int.RoundToInt((Vector2)transform.position - center);
            if (previousDragPoint != currentDragPoint)
            {
                previousDragPoint = currentDragPoint;
                board.Hover(currentDragPoint, polyominoIndex);
                Highlight(board.HighlightPolyominoColumns, board.HighlightPolyominoRows);
            }
        }
    }
    private void OnMouseUp()
    {
        previousMousePosition = Vector3.positiveInfinity;

        currentDragPoint = Vector2Int.RoundToInt((Vector2)transform.position - center);
        if (board.Place(currentDragPoint, polyominoIndex) == true)
        {
            gameObject.SetActive(false);
            blocks.Remove();
        }

        transform.localPosition = position;
        transform.localScale = scale;
    }

    private void Highlight(List<int> columns, List<int> rows)
    {
        var polyomino = Polyominos.Get(polyominoIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);

        Unhighlight(polyominoColumns, polyominoRows, polyomino);

        HighlightColumns(polyominoRows, polyomino, columns);
        HighlightRows(polyominoColumns, polyomino, rows);
    }

    private void Unhighlight(int polyominoColumns, int polyominoRows, int[,] polyoimno)
    {
        for (var r = 0; r < polyominoRows; ++r)
        {
            for (var c = 0; c < polyominoColumns; ++c)
            {
                if (polyoimno[r, c] > 0)
                {
                    cells[r, c].Normal();
                }
            }
        }
    }

    private void HighlightColumns(int polyominoRows, int[,] polyomino, List<int> columns)
    {
        foreach (var c in columns)
        {
            for (var r = 0; r < polyominoRows; ++r)
            {
                if (polyomino[r, c] > 0)
                {
                    cells[r, c].Highlight();
                }
            }
        }
    }

    private void HighlightRows(int polyominoColumns, int[,] polyomino, List<int> rows)
    {
        foreach (var r in rows)
        {
            for (var c = 0; c < polyominoColumns; ++c)
            {
                if (polyomino[r, c] > 0)
                {
                    cells[r, c].Highlight();
                }
            }
        }
    }

    public void SetSortingOrder(int sortingOrder)
    {
        sortingGroup.sortingOrder = sortingOrder;
    }
}
