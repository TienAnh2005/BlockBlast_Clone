using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private Sprite normal;
    [SerializeField] private Sprite highlight;

    private SpriteRenderer spriteRenderer;
    private Sprite placedSprite;
    private bool placedSpriteIsSpecial;
    private Vector3 baseLocalScale;

    private void Awake()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        baseLocalScale = transform.localScale;
    }

    public void Normal()
    {
        ShowPlaced(BlockType.Normal);
    }

    public void ShowPlaced(BlockType blockType, Sprite iceSprite = null, Sprite bombSprite = null, float scaleMultiplier = 1.0f)
    {
        gameObject.SetActive(true);

        placedSprite = ResolvePlacedSprite(blockType, iceSprite, bombSprite);
        placedSpriteIsSpecial = blockType != BlockType.Normal;

        transform.localScale = baseLocalScale * Mathf.Max(0.01f, scaleMultiplier);
        spriteRenderer.color = Color.white;
        spriteRenderer.sprite = placedSprite;
    }

    public void Highlight()
    {
        gameObject.SetActive(true);

        if (placedSpriteIsSpecial)
        {
            spriteRenderer.color = new Color(1.0f, 0.9f, 0.55f, 1.0f);
            spriteRenderer.sprite = placedSprite;
            return;
        }

        spriteRenderer.color = Color.white;
        spriteRenderer.sprite = highlight != null ? highlight : placedSprite;
    }

    public void Hover()
    {
        gameObject.SetActive(true);
        transform.localScale = baseLocalScale;
        spriteRenderer.color = new(1.0f, 1.0f, 1.0f, 0.5f);
        spriteRenderer.sprite = normal;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        transform.localScale = baseLocalScale;
        placedSprite = normal;
        placedSpriteIsSpecial = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    private Sprite ResolvePlacedSprite(BlockType blockType, Sprite iceSprite, Sprite bombSprite)
    {
        switch (blockType)
        {
            case BlockType.Ice:
                return iceSprite != null ? iceSprite : normal;
            case BlockType.Bomb:
                return bombSprite != null ? bombSprite : normal;
            default:
                return normal;
        }
    }
}
