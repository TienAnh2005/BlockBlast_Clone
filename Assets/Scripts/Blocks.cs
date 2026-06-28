using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blocks : MonoBehaviour
{
    [SerializeField] Board board;
    [SerializeField] private Block[] blocks;

    private int[] polyominoIndexes;
    private int[] queuedPolyominoIndexes;
    private int[] nextBatchOverrideIndexes;

    private int blockCount = 0;

    private void Start()
    {
        var blockWidth = (float)Board.Size / blocks.Length;
        var cellSize = (float)Board.Size / (Block.Size * blocks.Length + blocks.Length + 1);
        for (var i = 0; i < blocks.Length; ++i)
        {
            blocks[i].transform.localPosition = new(blockWidth * (i + 0.5f), -0.25f - cellSize * 4.0f, 0.0f);
            blocks[i].transform.localScale = new(cellSize, cellSize, cellSize);
            blocks[i].Initialize();
        }

        polyominoIndexes = new int[blocks.Length];

        Generate();
    }

    private void Generate()
    {
        blockCount = 0;

        // Use the latest analyzed batch when available; otherwise build a fresh one
        // from the board state right now.
        var generatedIndexes = ConsumeQueuedBatch();
        for (var i = 0; i < blocks.Length; ++i)
        {
            polyominoIndexes[i] = generatedIndexes[i];
            blocks[i].gameObject.SetActive(true);
            blocks[i].Show(polyominoIndexes[i]);

            ++blockCount;
        }
    }

    public void Remove()
    {
        --blockCount;

        if (board != null && board.HasForcedGameOver)
        {
            if (blockCount < 0)
            {
                blockCount = 0;
            }

            return;
        }

        // Rebuild the hidden next batch after every move using the latest board state.
        queuedPolyominoIndexes = PieceGenerator.Generate(board, blocks.Length);

        if (blockCount <= 0)
        {
            blockCount = 0;
            Generate();
        }

        var lose = true;
        for (var i = 0; i < blocks.Length; ++i)
        {
            if (blocks[i].gameObject.activeSelf == true && board.CheckPlace(polyominoIndexes[i]) == true)
            {
                lose = false;
                break;
            }
        }
        if (lose == true)
        {
            Debug.Log("Thua cmnr");
        }
    }

    public void ResetBlocksSortingOrders()
    {
        for (var i = 0; i < blocks.Length; ++i)
        {
            blocks[i].SetSortingOrder(0);
        }
    }

    public void SetNextBatchOverride(int[] overrideIndexes)
    {
        if (overrideIndexes == null || overrideIndexes.Length != blocks.Length)
        {
            nextBatchOverrideIndexes = null;
            return;
        }

        nextBatchOverrideIndexes = (int[])overrideIndexes.Clone();
    }

    public void ClearPendingBatches()
    {
        nextBatchOverrideIndexes = null;
        queuedPolyominoIndexes = null;
    }

    private int[] ConsumeQueuedBatch()
    {
        if (nextBatchOverrideIndexes != null && nextBatchOverrideIndexes.Length == blocks.Length)
        {
            var generatedIndexes = nextBatchOverrideIndexes;
            nextBatchOverrideIndexes = null;
            return generatedIndexes;
        }

        if (queuedPolyominoIndexes != null && queuedPolyominoIndexes.Length == blocks.Length)
        {
            var generatedIndexes = queuedPolyominoIndexes;
            queuedPolyominoIndexes = null;
            return generatedIndexes;
        }

        return PieceGenerator.Generate(board, blocks.Length);
    }
}
