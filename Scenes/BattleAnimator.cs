using UnityEngine;
using System.Collections.Generic;

public class BattleAnimator : MonoBehaviour
{
    public static BattleAnimator Instance { get; private set; }

    [Header("Setup")]
    public GameObject scrollingTextPrefab;
    public Transform animationCanvas;

    [Header("Style")]
    public Color playerAttackColor = new Color(0.5f, 0.8f, 1f, 0.8f);
    public Color enemyAttackColor = new Color(1f, 0.5f, 0.5f, 0.8f);
    public Color statusEffectColor = new Color(0.6f, 0.2f, 0.8f, 0.8f);
    public Color healColor = new Color(0.2f, 1f, 0.2f, 0.8f);
    public Color blockColor = new Color(0.3f, 0.5f, 1f, 0.8f);

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else { Destroy(gameObject); }
    }

    public void StopAllAnimations()
    {
        if (animationCanvas == null) return;

        foreach (Transform child in animationCanvas)
        {
            Destroy(child.gameObject);
        }
    }

    public void PlayAttackAnimation(string text, bool isPlayerAttack, List<Sprite> diceSprites, int agility)
    {
        if (scrollingTextPrefab == null || animationCanvas == null) return;

        GameObject textInstance = Instantiate(scrollingTextPrefab, animationCanvas);

        RectTransform canvasRect = animationCanvas.GetComponent<RectTransform>();
        RectTransform textRect = textInstance.GetComponent<RectTransform>();
        float startX = (canvasRect.rect.width / 2) + (textRect.rect.width / 2);
        textRect.anchoredPosition = new Vector2(startX, 0);

        ScrollingBattleText scrollingText = textInstance.GetComponent<ScrollingBattleText>();
        if (scrollingText != null)
        {
            Color attackColor = isPlayerAttack ? playerAttackColor : enemyAttackColor;
            scrollingText.Initialize(text, attackColor, diceSprites, agility);
        }
    }

    public void PlayStatusEffectAnimation(string text)
    {
        if (scrollingTextPrefab == null || animationCanvas == null) return;

        GameObject textInstance = Instantiate(scrollingTextPrefab, animationCanvas);

        RectTransform canvasRect = animationCanvas.GetComponent<RectTransform>();
        RectTransform textRect = textInstance.GetComponent<RectTransform>();
        float startX = (canvasRect.rect.width / 2) + (textRect.rect.width / 2);
        textRect.anchoredPosition = new Vector2(startX, 0);

        ScrollingBattleText scrollingText = textInstance.GetComponent<ScrollingBattleText>();
        if (scrollingText != null)
        {
            scrollingText.Initialize(text, statusEffectColor, null, 0);
        }
    }
    
    public void PlayHealAnimation(string text, bool isPlayer, List<Sprite> diceSprites)
    {
        if (scrollingTextPrefab == null || animationCanvas == null) return;

        GameObject textInstance = Instantiate(scrollingTextPrefab, animationCanvas);

        RectTransform canvasRect = animationCanvas.GetComponent<RectTransform>();
        RectTransform textRect = textInstance.GetComponent<RectTransform>();
        float startX = (canvasRect.rect.width / 2) + (textRect.rect.width / 2);
        textRect.anchoredPosition = new Vector2(startX, 0);

        ScrollingBattleText scrollingText = textInstance.GetComponent<ScrollingBattleText>();
        if (scrollingText != null)
        {
            scrollingText.Initialize(text, healColor, diceSprites, 0);
        }
    }
    
    public void PlayBlockAnimation(string text, bool isPlayer, List<Sprite> diceSprites)
    {
        if (scrollingTextPrefab == null || animationCanvas == null) return;

        GameObject textInstance = Instantiate(scrollingTextPrefab, animationCanvas);

        RectTransform canvasRect = animationCanvas.GetComponent<RectTransform>();
        RectTransform textRect = textInstance.GetComponent<RectTransform>();
        float startX = (canvasRect.rect.width / 2) + (textRect.rect.width / 2);
        textRect.anchoredPosition = new Vector2(startX, 0);

        ScrollingBattleText scrollingText = textInstance.GetComponent<ScrollingBattleText>();
        if (scrollingText != null)
        {
            scrollingText.Initialize(text, blockColor, diceSprites, 0);
        }
    }
}