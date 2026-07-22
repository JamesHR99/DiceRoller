using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // Required for List

public class ScrollingBattleText : MonoBehaviour
{
    [Header("Animation Settings")]
    public float scrollSpeed = 200f;
    public float lifetime = 7f;

    [Header("Component References")]
    public TextMeshProUGUI displayText;
    public TextMeshProUGUI agilityText;
    public Image backgroundImage;
    public Transform diceIconContainer;
    public GameObject diceIconPrefab;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);
    }

    public void Initialize(string text, Color bgColor, List<Sprite> diceSprites, int agility = 0)
    {
        if (displayText != null)
        {
            displayText.text = text;
        }

        if (agilityText != null)
        {
            agilityText.text = $"Agility: {agility}";
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = bgColor;
        }

        if (diceIconContainer != null && diceIconPrefab != null && diceSprites != null)
        {
            foreach (Transform child in diceIconContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (Sprite sprite in diceSprites)
            {
                GameObject icon = Instantiate(diceIconPrefab, diceIconContainer);
                icon.GetComponent<Image>().sprite = sprite;
            }
        }
    }
}