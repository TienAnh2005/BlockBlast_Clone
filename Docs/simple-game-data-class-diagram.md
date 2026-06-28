# Simple Game Data Class Diagram

Phien ban rut gon, tap trung vao cac lop du lieu chinh va persistence cua game.

```mermaid
classDiagram
    direction LR

    class LevelData {
        <<ScriptableObject>>
        +levelId : int
        +displayName : string
        +targetScore : int
        -prePlacedGrid : bool[]
        -openingBlockGrids : LevelOpeningBlockGrid[]
        +GetPrePlacedPoints()
        +GetOpeningBlockPolyominoIndex()
    }

    class LevelOpeningBlockGrid {
        +cells : bool[]
        +BuildTrimmedShape()
    }

    class LevelSystemManager {
        -levelDataAssets : LevelData[]
        -orderedLevelData : List~LevelData~
        -currentLevel : int
        -highestUnlockedLevel : int
        +LoadLevelDataAssets()
        +GetTargetScore()
        +GetOpeningBlockBatch()
        +SaveUnlockedProgress()
    }

    class ScoreManager {
        <<Singleton>>
        -currentScore : int
        -highScore : int
        +CurrentScore : int
        +HighScore : int
        +AddScore()
        +ResetCurrentScore()
    }

    class PlayerPrefs {
        <<Persistence>>
        +HighScore
        +HighestUnlockedLevel
    }

    LevelData "1" *-- "3" LevelOpeningBlockGrid : contains
    LevelSystemManager --> "many" LevelData : loads
    LevelSystemManager --> PlayerPrefs : save/load progress
    ScoreManager --> PlayerPrefs : save/load high score
    LevelSystemManager --> ScoreManager : reads score
```

Tom tat ngan:
- `LevelData` luu cau hinh tung level.
- `LevelOpeningBlockGrid` luu hinh dang 3 block mo dau.
- `LevelSystemManager` doc level data va quan ly tien trinh unlock level.
- `ScoreManager` quan ly diem hien tai va high score.
- `PlayerPrefs` dong vai tro bo nho luu du lieu don gian cua game.
