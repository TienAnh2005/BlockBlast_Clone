using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    private const float BoardCellSize = 30f;
    private const float OpeningBlockCellSize = 24f;

    private SerializedProperty levelIdProperty;
    private SerializedProperty displayNameProperty;
    private SerializedProperty targetScoreProperty;

    private LevelData levelData;

    private void OnEnable()
    {
        levelData = (LevelData)target;
        levelIdProperty = serializedObject.FindProperty("levelId");
        displayNameProperty = serializedObject.FindProperty("displayName");
        targetScoreProperty = serializedObject.FindProperty("targetScore");
        levelData.EnsureGridInitialized();
        levelData.EnsureOpeningBlocksInitialized();

        if (levelData.HasAnyPrePlacedCell() == false && levelData.HasLegacyPlacementData)
        {
            levelData.ImportLegacyDataToGrid(clearLegacyDataAfterImport: false);
            EditorUtility.SetDirty(levelData);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(levelIdProperty);
        EditorGUILayout.PropertyField(displayNameProperty);
        EditorGUILayout.PropertyField(targetScoreProperty);

        serializedObject.ApplyModifiedProperties();

        DrawOpeningBlocksSection();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Pre-placed Grid (8x8)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Bam vao tung o de luan phien: Empty -> Normal -> Ice -> Bomb -> Empty. Hang tren cung la y=7, hang duoi cung la y=0.",
            MessageType.Info);
        EditorGUILayout.LabelField("Mau sac: xanh la Normal, xanh nhat la Ice, do la Bomb.", EditorStyles.miniBoldLabel);

        DrawCoordinateHeader(LevelData.GridSize, BoardCellSize);
        for (var y = LevelData.GridSize - 1; y >= 0; --y)
        {
            DrawBoardGridRow(y);
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField($"So o dang bat: {CountFilledCells()}", EditorStyles.miniBoldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear Grid"))
            {
                Undo.RecordObject(levelData, "Clear Level Grid");
                levelData.ClearGrid();
                EditorUtility.SetDirty(levelData);
            }

            if (GUILayout.Button("Fill Full Board"))
            {
                Undo.RecordObject(levelData, "Fill Level Grid");
                FillFullBoard();
                EditorUtility.SetDirty(levelData);
            }
        }

        if (levelData.HasLegacyPlacementData)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "Level nay van con du lieu cu (shape / toa do). Neu muon chuyen het sang grid moi, bam nut ben duoi.",
                MessageType.Warning);

            if (GUILayout.Button("Import Legacy Data To Grid"))
            {
                Undo.RecordObject(levelData, "Import Legacy Level Data");
                levelData.ImportLegacyDataToGrid();
                EditorUtility.SetDirty(levelData);
            }
        }
    }

    private void DrawOpeningBlocksSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Opening Blocks (3 block dau tien)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Moi slot la mot grid 5x5. Ve shape block vao day. Sau khi nguoi choi dung het 3 block mo dau, he thong se quay lai adaptive generator hien tai. Slot de trong hoac shape khong hop le se fallback sang generator binh thuong.",
            MessageType.Info);

        for (var slotIndex = 0; slotIndex < LevelData.OpeningBlockCount; ++slotIndex)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Opening Block {slotIndex + 1}", EditorStyles.miniBoldLabel);

            var polyominoIndex = levelData.GetOpeningBlockPolyominoIndex(slotIndex);
            var hasCells = levelData.HasOpeningBlockCells(slotIndex);
            if (hasCells == false)
            {
                EditorGUILayout.LabelField(
                    "Trang thai: de trong -> slot nay se duoc sinh boi adaptive generator.",
                    EditorStyles.miniLabel);
            }
            else if (polyominoIndex >= 0)
            {
                EditorGUILayout.LabelField(
                    $"Trang thai: hop le (Polyomino Index = {polyominoIndex}).",
                    EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Shape nay khong khop voi bat ky Polyomino nao dang co trong game. Runtime se bo qua slot nay va fallback sang generator.",
                    MessageType.Warning);
            }

            DrawCoordinateHeader(LevelOpeningBlockGrid.Size, OpeningBlockCellSize);
            for (var y = LevelOpeningBlockGrid.Size - 1; y >= 0; --y)
            {
                DrawOpeningBlockRow(slotIndex, y);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button($"Clear Opening Block {slotIndex + 1}"))
                {
                    Undo.RecordObject(levelData, "Clear Opening Block Grid");
                    levelData.ClearOpeningBlockGrid(slotIndex);
                    EditorUtility.SetDirty(levelData);
                }
            }
        }
    }

    private void DrawCoordinateHeader(int gridSize, float cellSize)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(cellSize);
            for (var x = 0; x < gridSize; ++x)
            {
                GUILayout.Label(x.ToString(), GetCenteredMiniLabel(), GUILayout.Width(cellSize), GUILayout.Height(18f));
            }
        }
    }

    private void DrawBoardGridRow(int y)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(y.ToString(), GetCenteredMiniLabel(), GUILayout.Width(BoardCellSize), GUILayout.Height(BoardCellSize));

            for (var x = 0; x < LevelData.GridSize; ++x)
            {
                DrawBoardCellState(x, y);
            }
        }
    }

    private void DrawBoardGridCell(int x, int y)
    {
        var isFilled = levelData.IsCellFilled(x, y);
        var previousBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = isFilled
            ? new Color(0.25f, 0.74f, 0.31f, 1f)
            : new Color(0.22f, 0.24f, 0.29f, 1f);

        if (GUILayout.Button(isFilled ? "■" : string.Empty, GUILayout.Width(BoardCellSize), GUILayout.Height(BoardCellSize)))
        {
            Undo.RecordObject(levelData, "Toggle Level Cell");
            levelData.ToggleCell(x, y);
            EditorUtility.SetDirty(levelData);
        }

        GUI.backgroundColor = previousBackgroundColor;
    }

    private void DrawBoardCellState(int x, int y)
    {
        var isFilled = levelData.IsCellFilled(x, y);
        var blockType = levelData.GetCellBlockType(x, y);
        var previousBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = GetBoardCellColor(isFilled, blockType);

        if (GUILayout.Button(GetBoardCellLabel(isFilled, blockType), GUILayout.Width(BoardCellSize), GUILayout.Height(BoardCellSize)))
        {
            Undo.RecordObject(levelData, "Cycle Level Cell State");
            levelData.CycleCellState(x, y);
            EditorUtility.SetDirty(levelData);
        }

        GUI.backgroundColor = previousBackgroundColor;
    }

    private void DrawOpeningBlockRow(int slotIndex, int y)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(y.ToString(), GetCenteredMiniLabel(), GUILayout.Width(OpeningBlockCellSize), GUILayout.Height(OpeningBlockCellSize));

            for (var x = 0; x < LevelOpeningBlockGrid.Size; ++x)
            {
                DrawOpeningBlockCell(slotIndex, x, y);
            }
        }
    }

    private void DrawOpeningBlockCell(int slotIndex, int x, int y)
    {
        var isFilled = levelData.IsOpeningBlockCellFilled(slotIndex, x, y);
        var previousBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = isFilled
            ? new Color(0.22f, 0.67f, 0.93f, 1f)
            : new Color(0.22f, 0.24f, 0.29f, 1f);

        if (GUILayout.Button(isFilled ? "■" : string.Empty, GUILayout.Width(OpeningBlockCellSize), GUILayout.Height(OpeningBlockCellSize)))
        {
            Undo.RecordObject(levelData, "Toggle Opening Block Cell");
            levelData.ToggleOpeningBlockCell(slotIndex, x, y);
            EditorUtility.SetDirty(levelData);
        }

        GUI.backgroundColor = previousBackgroundColor;
    }

    private int CountFilledCells()
    {
        var count = 0;
        for (var y = 0; y < LevelData.GridSize; ++y)
        {
            for (var x = 0; x < LevelData.GridSize; ++x)
            {
                if (levelData.IsCellFilled(x, y))
                {
                    ++count;
                }
            }
        }

        return count;
    }

    private void FillFullBoard()
    {
        for (var y = 0; y < LevelData.GridSize; ++y)
        {
            for (var x = 0; x < LevelData.GridSize; ++x)
            {
                levelData.SetCellState(x, y, true, BlockType.Normal);
            }
        }
    }

    private static Color GetBoardCellColor(bool isFilled, BlockType blockType)
    {
        if (isFilled == false)
        {
            return new Color(0.22f, 0.24f, 0.29f, 1f);
        }

        switch (blockType)
        {
            case BlockType.Ice:
                return new Color(0.45f, 0.78f, 0.98f, 1f);
            case BlockType.Bomb:
                return new Color(0.86f, 0.27f, 0.24f, 1f);
            default:
                return new Color(0.25f, 0.74f, 0.31f, 1f);
        }
    }

    private static string GetBoardCellLabel(bool isFilled, BlockType blockType)
    {
        if (isFilled == false)
        {
            return string.Empty;
        }

        switch (blockType)
        {
            case BlockType.Ice:
                return "I";
            case BlockType.Bomb:
                return "B";
            default:
                return "N";
        }
    }

    private static GUIStyle GetCenteredMiniLabel()
    {
        var style = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        return style;
    }
}
