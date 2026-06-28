using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public enum BlockType
{
    Normal,
    Ice,
    Bomb
}

[Serializable]
public struct LevelConfiguredCell
{
    public Vector2Int point;
    public BlockType blockType;
}

[Serializable]
public struct LevelShapePlacement
{
    [Tooltip("Toa do goc dat shape tren board. x: trai -> phai, y: duoi -> tren.")]
    public Vector2Int origin;

    [Tooltip("Index cua shape trong Polyominos.cs.")]
    public int polyominoIndex;
}

[Serializable]
public struct LevelCellPlacement
{
    [Tooltip("Toa do o da duoc khoa san. x: trai -> phai, y: duoi -> tren.")]
    public Vector2Int point;
}

[Serializable]
public class LevelOpeningBlockGrid
{
    public const int Size = 5;

    [SerializeField]
    private bool[] cells = new bool[Size * Size];

    public void EnsureInitialized()
    {
        if (cells != null && cells.Length == Size * Size)
        {
            return;
        }

        var oldCells = cells;
        cells = new bool[Size * Size];
        if (oldCells == null)
        {
            return;
        }

        Array.Copy(oldCells, cells, Mathf.Min(oldCells.Length, cells.Length));
    }

    public bool HasAnyFilledCell()
    {
        EnsureInitialized();
        for (var i = 0; i < cells.Length; ++i)
        {
            if (cells[i])
            {
                return true;
            }
        }

        return false;
    }

    public bool IsFilled(int x, int y)
    {
        if (IsInBounds(x, y) == false)
        {
            return false;
        }

        EnsureInitialized();
        return cells[ToIndex(x, y)];
    }

    public void SetFilled(int x, int y, bool value)
    {
        if (IsInBounds(x, y) == false)
        {
            return;
        }

        EnsureInitialized();
        cells[ToIndex(x, y)] = value;
    }

    public void ToggleCell(int x, int y)
    {
        SetFilled(x, y, IsFilled(x, y) == false);
    }

    public void Clear()
    {
        EnsureInitialized();
        Array.Clear(cells, 0, cells.Length);
    }

    public int[,] BuildTrimmedShape()
    {
        EnsureInitialized();
        if (HasAnyFilledCell() == false)
        {
            return null;
        }

        var minX = Size;
        var minY = Size;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < Size; ++y)
        {
            for (var x = 0; x < Size; ++x)
            {
                if (cells[ToIndex(x, y)] == false)
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return null;
        }

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        var shape = new int[height, width];

        for (var y = minY; y <= maxY; ++y)
        {
            for (var x = minX; x <= maxX; ++x)
            {
                shape[y - minY, x - minX] = cells[ToIndex(x, y)] ? 1 : 0;
            }
        }

        return shape;
    }

    private static bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Size && y >= 0 && y < Size;
    }

    private static int ToIndex(int x, int y)
    {
        return y * Size + x;
    }
}

[CreateAssetMenu(fileName = "LevelData_", menuName = "Block Blast/Level Data")]
public class LevelData : ScriptableObject
{
    public const int GridSize = 8;
    public const int OpeningBlockCount = 3;

    [Min(1)]
    public int levelId = 1;

    public string displayName = "Level 1";

    [Min(0)]
    public int targetScore = 10;

    [SerializeField]
    private bool[] prePlacedGrid = new bool[GridSize * GridSize];

    [SerializeField]
    private BlockType[] prePlacedBlockTypes = new BlockType[GridSize * GridSize];

    [SerializeField]
    private LevelOpeningBlockGrid[] openingBlockGrids = new LevelOpeningBlockGrid[OpeningBlockCount];

    [Header("Legacy Data")]
    [HideInInspector]
    public LevelShapePlacement[] prePlacedShapes = Array.Empty<LevelShapePlacement>();

    [HideInInspector]
    public LevelCellPlacement[] prePlacedCells = Array.Empty<LevelCellPlacement>();

    public bool HasLegacyPlacementData =>
        (prePlacedShapes != null && prePlacedShapes.Length > 0) ||
        (prePlacedCells != null && prePlacedCells.Length > 0);

    public void EnsureGridInitialized()
    {
        if (prePlacedGrid != null && prePlacedGrid.Length == GridSize * GridSize)
        {
            if (prePlacedBlockTypes != null && prePlacedBlockTypes.Length == GridSize * GridSize)
            {
                return;
            }

            var oldBlockTypes = prePlacedBlockTypes;
            prePlacedBlockTypes = new BlockType[GridSize * GridSize];
            if (oldBlockTypes == null)
            {
                return;
            }

            var blockTypeCopyLength = Mathf.Min(oldBlockTypes.Length, prePlacedBlockTypes.Length);
            Array.Copy(oldBlockTypes, prePlacedBlockTypes, blockTypeCopyLength);
            return;
        }

        var oldGrid = prePlacedGrid;
        prePlacedGrid = new bool[GridSize * GridSize];
        var oldBlockTypesFallback = prePlacedBlockTypes;
        prePlacedBlockTypes = new BlockType[GridSize * GridSize];
        if (oldGrid == null)
        {
            return;
        }

        var copyLength = Mathf.Min(oldGrid.Length, prePlacedGrid.Length);
        Array.Copy(oldGrid, prePlacedGrid, copyLength);

        if (oldBlockTypesFallback == null)
        {
            return;
        }

        var fallbackBlockTypeCopyLength = Mathf.Min(oldBlockTypesFallback.Length, prePlacedBlockTypes.Length);
        Array.Copy(oldBlockTypesFallback, prePlacedBlockTypes, fallbackBlockTypeCopyLength);
    }

    public void EnsureOpeningBlocksInitialized()
    {
        if (openingBlockGrids == null || openingBlockGrids.Length != OpeningBlockCount)
        {
            var oldBlocks = openingBlockGrids;
            openingBlockGrids = new LevelOpeningBlockGrid[OpeningBlockCount];
            if (oldBlocks != null)
            {
                for (var i = 0; i < Mathf.Min(oldBlocks.Length, openingBlockGrids.Length); ++i)
                {
                    openingBlockGrids[i] = oldBlocks[i];
                }
            }
        }

        for (var i = 0; i < openingBlockGrids.Length; ++i)
        {
            if (openingBlockGrids[i] == null)
            {
                openingBlockGrids[i] = new LevelOpeningBlockGrid();
            }

            openingBlockGrids[i].EnsureInitialized();
        }
    }

    public bool HasAnyPrePlacedCell()
    {
        EnsureGridInitialized();
        for (var i = 0; i < prePlacedGrid.Length; ++i)
        {
            if (prePlacedGrid[i])
            {
                return true;
            }
        }

        return false;
    }

    public bool IsCellFilled(int x, int y)
    {
        if (IsInBounds(x, y) == false)
        {
            return false;
        }

        EnsureGridInitialized();
        return prePlacedGrid[ToIndex(x, y)];
    }

    public BlockType GetCellBlockType(int x, int y)
    {
        if (IsInBounds(x, y) == false)
        {
            return BlockType.Normal;
        }

        EnsureGridInitialized();
        if (prePlacedGrid[ToIndex(x, y)] == false)
        {
            return BlockType.Normal;
        }

        return prePlacedBlockTypes[ToIndex(x, y)];
    }

    public void SetCellFilled(int x, int y, bool value)
    {
        if (IsInBounds(x, y) == false)
        {
            return;
        }

        EnsureGridInitialized();
        var index = ToIndex(x, y);
        if (value == false)
        {
            prePlacedGrid[index] = false;
            prePlacedBlockTypes[index] = BlockType.Normal;
            return;
        }

        prePlacedGrid[index] = true;
        if (prePlacedBlockTypes[index] != BlockType.Ice && prePlacedBlockTypes[index] != BlockType.Bomb)
        {
            prePlacedBlockTypes[index] = BlockType.Normal;
        }
    }

    public void SetCellBlockType(int x, int y, BlockType blockType)
    {
        SetCellState(x, y, true, blockType);
    }

    public void SetCellState(int x, int y, bool isFilled, BlockType blockType)
    {
        if (IsInBounds(x, y) == false)
        {
            return;
        }

        EnsureGridInitialized();

        var index = ToIndex(x, y);
        prePlacedGrid[index] = isFilled;
        prePlacedBlockTypes[index] = isFilled ? blockType : BlockType.Normal;
    }

    public void ToggleCell(int x, int y)
    {
        SetCellFilled(x, y, IsCellFilled(x, y) == false);
    }

    public void CycleCellState(int x, int y)
    {
        if (IsCellFilled(x, y) == false)
        {
            SetCellState(x, y, true, BlockType.Normal);
            return;
        }

        switch (GetCellBlockType(x, y))
        {
            case BlockType.Normal:
                SetCellState(x, y, true, BlockType.Ice);
                break;
            case BlockType.Ice:
                SetCellState(x, y, true, BlockType.Bomb);
                break;
            default:
                SetCellState(x, y, false, BlockType.Normal);
                break;
        }
    }

    public void ClearGrid()
    {
        EnsureGridInitialized();
        Array.Clear(prePlacedGrid, 0, prePlacedGrid.Length);
        Array.Clear(prePlacedBlockTypes, 0, prePlacedBlockTypes.Length);
    }

    public bool HasOpeningBlockCells(int slotIndex)
    {
        if (IsOpeningBlockSlotValid(slotIndex) == false)
        {
            return false;
        }

        EnsureOpeningBlocksInitialized();
        return openingBlockGrids[slotIndex].HasAnyFilledCell();
    }

    public bool IsOpeningBlockCellFilled(int slotIndex, int x, int y)
    {
        if (IsOpeningBlockSlotValid(slotIndex) == false)
        {
            return false;
        }

        EnsureOpeningBlocksInitialized();
        return openingBlockGrids[slotIndex].IsFilled(x, y);
    }

    public void SetOpeningBlockCellFilled(int slotIndex, int x, int y, bool value)
    {
        if (IsOpeningBlockSlotValid(slotIndex) == false)
        {
            return;
        }

        EnsureOpeningBlocksInitialized();
        openingBlockGrids[slotIndex].SetFilled(x, y, value);
    }

    public void ToggleOpeningBlockCell(int slotIndex, int x, int y)
    {
        SetOpeningBlockCellFilled(slotIndex, x, y, IsOpeningBlockCellFilled(slotIndex, x, y) == false);
    }

    public void ClearOpeningBlockGrid(int slotIndex)
    {
        if (IsOpeningBlockSlotValid(slotIndex) == false)
        {
            return;
        }

        EnsureOpeningBlocksInitialized();
        openingBlockGrids[slotIndex].Clear();
    }

    public int GetOpeningBlockPolyominoIndex(int slotIndex)
    {
        if (IsOpeningBlockSlotValid(slotIndex) == false)
        {
            return -1;
        }

        EnsureOpeningBlocksInitialized();
        var shape = openingBlockGrids[slotIndex].BuildTrimmedShape();
        if (shape == null)
        {
            return -1;
        }

        for (var i = 0; i < Polyominos.Length; ++i)
        {
            if (AreShapesEqual(shape, Polyominos.Get(i)))
            {
                return i;
            }
        }

        return -1;
    }

    public int[] GetOpeningBlockIndexes(int expectedCount)
    {
        EnsureOpeningBlocksInitialized();

        var maxCount = Mathf.Max(0, expectedCount);
        var indexes = new System.Collections.Generic.List<int>(maxCount);
        for (var i = 0; i < Mathf.Min(OpeningBlockCount, maxCount); ++i)
        {
            var polyominoIndex = GetOpeningBlockPolyominoIndex(i);
            if (polyominoIndex >= 0)
            {
                indexes.Add(polyominoIndex);
            }
        }

        return indexes.ToArray();
    }

    public void SetOpeningBlockFromPolyomino(int slotIndex, int polyominoIndex)
    {
        if (IsOpeningBlockSlotValid(slotIndex) == false)
        {
            return;
        }

        EnsureOpeningBlocksInitialized();
        openingBlockGrids[slotIndex].Clear();

        if (polyominoIndex < 0 || polyominoIndex >= Polyominos.Length)
        {
            return;
        }

        var polyomino = Polyominos.Get(polyominoIndex);
        var rows = polyomino.GetLength(0);
        var columns = polyomino.GetLength(1);
        for (var row = 0; row < rows; ++row)
        {
            for (var column = 0; column < columns; ++column)
            {
                if (polyomino[row, column] > 0)
                {
                    openingBlockGrids[slotIndex].SetFilled(column, row, true);
                }
            }
        }
    }

    public LevelConfiguredCell[] GetPrePlacedCells()
    {
        EnsureGridInitialized();

        if (HasAnyPrePlacedCell())
        {
            return GetCellsFromGrid();
        }

        return GetCellsFromLegacyData();
    }

    public Vector2Int[] GetPrePlacedPoints()
    {
        var cells = GetPrePlacedCells();
        var points = new Vector2Int[cells.Length];
        for (var i = 0; i < cells.Length; ++i)
        {
            points[i] = cells[i].point;
        }

        return points;
    }

    public void ImportLegacyDataToGrid(bool clearLegacyDataAfterImport = true)
    {
        EnsureGridInitialized();
        ClearGrid();

        var legacyCells = GetCellsFromLegacyData();
        foreach (var configuredCell in legacyCells)
        {
            SetCellState(configuredCell.point.x, configuredCell.point.y, true, configuredCell.blockType);
        }

        if (clearLegacyDataAfterImport)
        {
            prePlacedShapes = Array.Empty<LevelShapePlacement>();
            prePlacedCells = Array.Empty<LevelCellPlacement>();
        }
    }

    private LevelConfiguredCell[] GetCellsFromGrid()
    {
        var cells = new System.Collections.Generic.List<LevelConfiguredCell>();
        for (var y = 0; y < GridSize; ++y)
        {
            for (var x = 0; x < GridSize; ++x)
            {
                if (prePlacedGrid[ToIndex(x, y)])
                {
                    cells.Add(new LevelConfiguredCell
                    {
                        point = new Vector2Int(x, y),
                        blockType = prePlacedBlockTypes[ToIndex(x, y)]
                    });
                }
            }
        }

        return cells.ToArray();
    }

    private LevelConfiguredCell[] GetCellsFromLegacyData()
    {
        var cells = new System.Collections.Generic.List<LevelConfiguredCell>();
        var points = new System.Collections.Generic.HashSet<Vector2Int>();

        if (prePlacedShapes != null)
        {
            foreach (var shapePlacement in prePlacedShapes)
            {
                var polyomino = Polyominos.Get(shapePlacement.polyominoIndex);
                var rows = polyomino.GetLength(0);
                var columns = polyomino.GetLength(1);
                for (var row = 0; row < rows; ++row)
                {
                    for (var column = 0; column < columns; ++column)
                    {
                        if (polyomino[row, column] <= 0)
                        {
                            continue;
                        }

                        var point = shapePlacement.origin + new Vector2Int(column, row);
                        if (IsInBounds(point.x, point.y))
                        {
                            points.Add(point);
                        }
                    }
                }
            }
        }

        if (prePlacedCells != null)
        {
            foreach (var cellPlacement in prePlacedCells)
            {
                if (IsInBounds(cellPlacement.point.x, cellPlacement.point.y))
                {
                    points.Add(cellPlacement.point);
                }
            }
        }

        foreach (var point in points)
        {
            cells.Add(new LevelConfiguredCell
            {
                point = point,
                blockType = BlockType.Normal
            });
        }

        return cells.ToArray();
    }

    private static bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < GridSize && y >= 0 && y < GridSize;
    }

    private static bool AreShapesEqual(int[,] left, int[,] right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (left.GetLength(0) != right.GetLength(0) || left.GetLength(1) != right.GetLength(1))
        {
            return false;
        }

        for (var row = 0; row < left.GetLength(0); ++row)
        {
            for (var column = 0; column < left.GetLength(1); ++column)
            {
                if (left[row, column] != right[row, column])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsOpeningBlockSlotValid(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < OpeningBlockCount;
    }

    private static int ToIndex(int x, int y)
    {
        return y * GridSize + x;
    }

#if UNITY_EDITOR
    private const string LevelsFolderPath = "Assets/Resources/Levels";

    [MenuItem("Tools/Block Blast/Create Default Level Data Assets")]
    private static void CreateDefaultLevelDataAssets()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(LevelsFolderPath);

        CreateAssetIfMissing(1, "Level 1", 10,
            new[] { new LevelShapePlacement { origin = new Vector2Int(3, 3), polyominoIndex = 4 } },
            Array.Empty<LevelCellPlacement>());

        CreateAssetIfMissing(2, "Level 2", 20,
            new[]
            {
                new LevelShapePlacement { origin = new Vector2Int(1, 5), polyominoIndex = 7 },
                new LevelShapePlacement { origin = new Vector2Int(4, 2), polyominoIndex = 4 }
            },
            Array.Empty<LevelCellPlacement>());

        CreateAssetIfMissing(3, "Level 3", 30,
            new[]
            {
                new LevelShapePlacement { origin = new Vector2Int(0, 4), polyominoIndex = 9 },
                new LevelShapePlacement { origin = new Vector2Int(5, 1), polyominoIndex = 8 }
            },
            Array.Empty<LevelCellPlacement>());

        CreateAssetIfMissing(4, "Level 4", 40,
            new[]
            {
                new LevelShapePlacement { origin = new Vector2Int(0, 0), polyominoIndex = 12 },
                new LevelShapePlacement { origin = new Vector2Int(4, 4), polyominoIndex = 10 }
            },
            new[]
            {
                new LevelCellPlacement { point = new Vector2Int(7, 7) }
            });

        CreateAssetIfMissing(5, "Level 5", 50,
            new[]
            {
                new LevelShapePlacement { origin = new Vector2Int(0, 5), polyominoIndex = 7 },
                new LevelShapePlacement { origin = new Vector2Int(5, 5), polyominoIndex = 7 },
                new LevelShapePlacement { origin = new Vector2Int(2, 1), polyominoIndex = 4 },
                new LevelShapePlacement { origin = new Vector2Int(4, 0), polyominoIndex = 8 }
            },
            Array.Empty<LevelCellPlacement>());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LevelData assets ready in Assets/Resources/Levels.");
    }

    private static void CreateAssetIfMissing(
        int levelId,
        string displayName,
        int targetScore,
        LevelShapePlacement[] shapes,
        LevelCellPlacement[] cells
    )
    {
        var assetPath = $"{LevelsFolderPath}/Level_{levelId:00}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
        if (existing != null)
        {
            return;
        }

        var asset = CreateInstance<LevelData>();
        asset.levelId = levelId;
        asset.displayName = displayName;
        asset.targetScore = targetScore;
        asset.prePlacedShapes = shapes;
        asset.prePlacedCells = cells;
        asset.ImportLegacyDataToGrid();

        AssetDatabase.CreateAsset(asset, assetPath);
        EditorUtility.SetDirty(asset);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        var parentPath = System.IO.Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        var folderName = System.IO.Path.GetFileName(folderPath);
        if (string.IsNullOrEmpty(parentPath) || string.IsNullOrEmpty(folderName))
        {
            return;
        }

        EnsureFolder(parentPath);
        AssetDatabase.CreateFolder(parentPath, folderName);
    }
#endif
}
