# Game Data Class Diagram

Ghi chu:
- Project nay khong co database theo kieu SQL/NoSQL.
- Lop data hien tai duoc luu theo 2 tang:
  - `LevelData` (`ScriptableObject`) cho du lieu cau hinh level.
  - `PlayerPrefs` cho du lieu persistence don gian nhu `HighScore` va `HighestUnlockedLevel`.

```mermaid
classDiagram
    class LevelSystemManager {
        - LevelData[] levelDataAssets
        - List~LevelData~ orderedLevelData
        - int currentLevel
        - int highestUnlockedLevel
        + EnterLevelMode()
        + EnterClassicMode()
        + PrepareForMainMenu()
        - LoadLevelDataAssets()
        - GetTargetScore(level) int
        - GetOpeningBlockBatch(level) int[]
        - LoadSavedProgress()
        - SaveUnlockedProgress()
    }

    class LevelData {
        <<ScriptableObject>>
        + int levelId
        + string displayName
        + int targetScore
        - bool[] prePlacedGrid
        - LevelOpeningBlockGrid[] openingBlockGrids
        + GetPrePlacedPoints() Vector2Int[]
        + GetOpeningBlockPolyominoIndex(slotIndex) int
        + SetOpeningBlockFromPolyomino(slotIndex, polyominoIndex)
        + ImportLegacyDataToGrid()
    }

    class LevelOpeningBlockGrid {
        + const int Size = 5
        - bool[] cells
        + IsFilled(x, y) bool
        + SetFilled(x, y, value)
        + Clear()
        + BuildTrimmedShape() int[,]
    }

    class LevelShapePlacement {
        <<Legacy Struct>>
        + Vector2Int origin
        + int polyominoIndex
    }

    class LevelCellPlacement {
        <<Legacy Struct>>
        + Vector2Int point
    }

    class ScoreManager {
        <<Singleton>>
        + ScoreManager Instance
        + event ScoreChanged
        + int CurrentScore
        + int HighScore
        + int PointsPerClearedLine
        + AddScore(points)
        + ResetCurrentScore()
    }

    class PlayerPrefs {
        <<Persistence>>
        + HighScore
        + HighestUnlockedLevel
    }

    class Polyominos {
        <<Static Data>>
        + Get(index) int[,]
        + Length
    }

    LevelSystemManager "1" --> "*" LevelData : loads / sorts
    LevelSystemManager --> ScoreManager : observes ScoreChanged
    LevelSystemManager --> PlayerPrefs : save/load unlocked progress
    LevelSystemManager --> Polyominos : indirect opening block mapping

    LevelData "1" *-- "3" LevelOpeningBlockGrid : opening blocks
    LevelData "1" o-- "*" LevelShapePlacement : legacy shape data
    LevelData "1" o-- "*" LevelCellPlacement : legacy cell data
    LevelData --> Polyominos : shape lookup by index

    ScoreManager --> PlayerPrefs : save/load high score
```

Tom tat:
- `LevelData` la nguon du lieu level chinh: muc tieu diem, o khoa san, va 3 opening blocks dau tien.
- `LevelSystemManager` la lop doc/quan ly data level va data progress.
- `ScoreManager` la lop quan ly runtime score va persistence high score.
- `PlayerPrefs` dong vai tro "database don gian" cua game cho du lieu can giu sau khi tat game.
