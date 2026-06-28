using System.Collections.Generic;
using UnityEngine;

public struct BoardAnalysis
{
    public int[,] grid;
    public float danger;
    public int occupiedCells;
    public int placeablePieceCount;
    public int totalPlacementCount;
}

public struct PieceEvaluation
{
    public int pieceIndex;
    public float score;
    public int placementCount;
    public int bestClearCount;
}

public static class BoardAnalyzer
{
    public static BoardAnalysis Analyze(Board board)
    {
        var grid = board != null ? board.GetOccupiedGridCopy() : new int[Board.Size, Board.Size];

        var occupiedCells = 0;
        for (var row = 0; row < Board.Size; ++row)
        {
            for (var column = 0; column < Board.Size; ++column)
            {
                if (grid[row, column] != 0)
                {
                    ++occupiedCells;
                }
            }
        }

        var placeablePieceCount = 0;
        var totalPlacementCount = 0;
        for (var pieceIndex = 0; pieceIndex < Polyominos.Length; ++pieceIndex)
        {
            var placementCount = CountValidPlacements(grid, pieceIndex);
            if (placementCount > 0)
            {
                ++placeablePieceCount;
                totalPlacementCount += placementCount;
            }
        }

        var fillRatio = occupiedCells / (float)(Board.Size * Board.Size);
        var pieceAvailability = placeablePieceCount / (float)Polyominos.Length;
        var placementFreedom = Mathf.Clamp01(totalPlacementCount / 60.0f);

        // Danger goes up when the board is crowded and the player has fewer valid moves.
        var danger = Mathf.Clamp01(fillRatio * 0.55f + (1.0f - pieceAvailability) * 0.25f + (1.0f - placementFreedom) * 0.20f);

        return new BoardAnalysis
        {
            grid = grid,
            danger = danger,
            occupiedCells = occupiedCells,
            placeablePieceCount = placeablePieceCount,
            totalPlacementCount = totalPlacementCount
        };
    }

    public static int CountValidPlacements(int[,] grid, int pieceIndex)
    {
        var polyomino = Polyominos.Get(pieceIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);
        var placements = 0;

        for (var row = 0; row <= Board.Size - polyominoRows; ++row)
        {
            for (var column = 0; column <= Board.Size - polyominoColumns; ++column)
            {
                if (CanPlace(grid, row, column, polyomino))
                {
                    ++placements;
                }
            }
        }

        return placements;
    }

    public static bool CanPlace(int[,] grid, int row, int column, int[,] polyomino)
    {
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);

        for (var shapeRow = 0; shapeRow < polyominoRows; ++shapeRow)
        {
            for (var shapeColumn = 0; shapeColumn < polyominoColumns; ++shapeColumn)
            {
                if (polyomino[shapeRow, shapeColumn] > 0 && grid[row + shapeRow, column + shapeColumn] != 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static int CountClearedLines(int[,] grid, int row, int column, int[,] polyomino)
    {
        var tempGrid = (int[,])grid.Clone();
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);

        for (var shapeRow = 0; shapeRow < polyominoRows; ++shapeRow)
        {
            for (var shapeColumn = 0; shapeColumn < polyominoColumns; ++shapeColumn)
            {
                if (polyomino[shapeRow, shapeColumn] > 0)
                {
                    tempGrid[row + shapeRow, column + shapeColumn] = 1;
                }
            }
        }

        var clearedColumns = 0;
        for (var boardColumn = 0; boardColumn < Board.Size; ++boardColumn)
        {
            var full = true;
            for (var boardRow = 0; boardRow < Board.Size; ++boardRow)
            {
                if (tempGrid[boardRow, boardColumn] == 0)
                {
                    full = false;
                    break;
                }
            }

            if (full)
            {
                ++clearedColumns;
            }
        }

        var clearedRows = 0;
        for (var boardRow = 0; boardRow < Board.Size; ++boardRow)
        {
            var full = true;
            for (var boardColumn = 0; boardColumn < Board.Size; ++boardColumn)
            {
                if (tempGrid[boardRow, boardColumn] == 0)
                {
                    full = false;
                    break;
                }
            }

            if (full)
            {
                ++clearedRows;
            }
        }

        return clearedColumns + clearedRows;
    }
}

public static class PieceEvaluator
{
    public static PieceEvaluation Evaluate(int pieceIndex, BoardAnalysis analysis)
    {
        var polyomino = Polyominos.Get(pieceIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);

        var placementCount = 0;
        var bestClearCount = 0;
        var clearPlacementCount = 0;

        for (var row = 0; row <= Board.Size - polyominoRows; ++row)
        {
            for (var column = 0; column <= Board.Size - polyominoColumns; ++column)
            {
                if (BoardAnalyzer.CanPlace(analysis.grid, row, column, polyomino) == false)
                {
                    continue;
                }

                ++placementCount;

                var clearedLines = BoardAnalyzer.CountClearedLines(analysis.grid, row, column, polyomino);
                if (clearedLines > 0)
                {
                    ++clearPlacementCount;
                }

                if (clearedLines > bestClearCount)
                {
                    bestClearCount = clearedLines;
                }
            }
        }

        if (placementCount <= 0)
        {
            return new PieceEvaluation
            {
                pieceIndex = pieceIndex,
                score = 0.0f
            };
        }

        var cellCount = GetFilledCellCount(polyomino);
        var maxDimension = Mathf.Max(polyominoRows, polyominoColumns);

        // Reward pieces that fit in many places and can create clears.
        var score = placementCount * Mathf.Lerp(1.1f, 2.0f, analysis.danger);
        score += bestClearCount * 18.0f;
        score += clearPlacementCount * 4.0f;

        if (analysis.danger >= 0.60f)
        {
            if (cellCount <= 3)
            {
                score += 18.0f;
            }
            else if (cellCount <= 4)
            {
                score += 10.0f;
            }
            else if (cellCount >= 8)
            {
                score -= 14.0f;
            }
            else if (cellCount >= 6)
            {
                score -= 8.0f;
            }

            if (maxDimension >= 5)
            {
                score -= 10.0f;
            }
            else if (maxDimension == 4)
            {
                score -= 4.0f;
            }
        }
        else if (analysis.danger <= 0.35f)
        {
            score += cellCount * 1.5f;
            if (maxDimension >= 4)
            {
                score += 4.0f;
            }
        }

        return new PieceEvaluation
        {
            pieceIndex = pieceIndex,
            score = Mathf.Max(0.0f, score),
            placementCount = placementCount,
            bestClearCount = bestClearCount
        };
    }

    private static int GetFilledCellCount(int[,] polyomino)
    {
        var count = 0;
        var rows = polyomino.GetLength(0);
        var columns = polyomino.GetLength(1);

        for (var row = 0; row < rows; ++row)
        {
            for (var column = 0; column < columns; ++column)
            {
                if (polyomino[row, column] > 0)
                {
                    ++count;
                }
            }
        }

        return count;
    }
}

public static class PieceGenerator
{
    public static int[] Generate(Board board, int pieceCount)
    {
        var generatedPieces = new int[Mathf.Max(0, pieceCount)];
        if (generatedPieces.Length == 0)
        {
            return generatedPieces;
        }

        var analysis = BoardAnalyzer.Analyze(board);
        var candidates = new List<PieceEvaluation>();
        for (var pieceIndex = 0; pieceIndex < Polyominos.Length; ++pieceIndex)
        {
            var evaluation = PieceEvaluator.Evaluate(pieceIndex, analysis);
            if (evaluation.score > 0.0f)
            {
                candidates.Add(evaluation);
            }
        }

        if (candidates.Count == 0)
        {
            FillWithPureRandom(generatedPieces);
            return generatedPieces;
        }

        candidates.Sort((left, right) => right.score.CompareTo(left.score));
        var topCandidateCount = Mathf.Clamp(Mathf.CeilToInt(candidates.Count * 0.4f), generatedPieces.Length, candidates.Count);
        var topCandidates = new List<PieceEvaluation>(topCandidateCount);
        for (var i = 0; i < topCandidateCount; ++i)
        {
            topCandidates.Add(candidates[i]);
        }

        // Keep randomness by rolling only inside the strongest candidate group.
        var pickedCounts = new Dictionary<int, int>();
        for (var slot = 0; slot < generatedPieces.Length; ++slot)
        {
            generatedPieces[slot] = PickWeightedRandom(topCandidates, pickedCounts);
        }

        return generatedPieces;
    }

    private static int PickWeightedRandom(List<PieceEvaluation> candidates, Dictionary<int, int> pickedCounts)
    {
        var totalWeight = 0.0f;
        for (var i = 0; i < candidates.Count; ++i)
        {
            var repeatPenalty = 1.0f;
            if (pickedCounts.TryGetValue(candidates[i].pieceIndex, out var pickedCount))
            {
                repeatPenalty += pickedCount * 0.75f;
            }

            totalWeight += candidates[i].score / repeatPenalty;
        }

        if (totalWeight <= 0.0f)
        {
            return Random.Range(0, Polyominos.Length);
        }

        var roll = Random.Range(0.0f, totalWeight);
        var accumulatedWeight = 0.0f;
        for (var i = 0; i < candidates.Count; ++i)
        {
            var repeatPenalty = 1.0f;
            if (pickedCounts.TryGetValue(candidates[i].pieceIndex, out var pickedCount))
            {
                repeatPenalty += pickedCount * 0.75f;
            }

            accumulatedWeight += candidates[i].score / repeatPenalty;
            if (roll <= accumulatedWeight)
            {
                RegisterPick(candidates[i].pieceIndex, pickedCounts);
                return candidates[i].pieceIndex;
            }
        }

        var fallbackIndex = candidates[candidates.Count - 1].pieceIndex;
        RegisterPick(fallbackIndex, pickedCounts);
        return fallbackIndex;
    }

    private static void RegisterPick(int pieceIndex, Dictionary<int, int> pickedCounts)
    {
        if (pickedCounts.ContainsKey(pieceIndex))
        {
            pickedCounts[pieceIndex] += 1;
        }
        else
        {
            pickedCounts[pieceIndex] = 1;
        }
    }

    private static void FillWithPureRandom(int[] generatedPieces)
    {
        for (var i = 0; i < generatedPieces.Length; ++i)
        {
            generatedPieces[i] = Random.Range(0, Polyominos.Length);
        }
    }
}
