using UnityEngine;
using System.Collections.Generic;

public class HeldDicePool : MonoBehaviour
{
    public GameObject heldDicePrefab;  // Prefab of the dice to clone
    public Transform poolArea;  // Area where the dice will be placed

    // List to track currently held dice
    private List<Dice> heldDice = new List<Dice>();

    private void Start()
    {
        if (poolArea == null)
        {
            Debug.LogError("Pool area not set in HeldDicePool.");
        }
    }

    // Function to add a held dice to the pool
    public void AddDiceToPool(Dice die)
    {
        if (die.isHeld && !heldDice.Contains(die))  // Ensure the die is held and not already in the pool
        {
            // Add to the held dice list
            heldDice.Add(die);

            // Instantiate a new dice clone
            GameObject diceClone = Instantiate(heldDicePrefab, poolArea.position, Quaternion.identity);
            Rigidbody2D rb = diceClone.GetComponent<Rigidbody2D>();
            rb.gravityScale = 1;  // Apply gravity to the dice

            // Transfer the sprite from the original dice to the new one
            SpriteRenderer sr = diceClone.GetComponent<SpriteRenderer>();
            sr.sprite = die.diceImage.sprite;

            // Set the clone to be draggable
            DiceDraggable draggable = diceClone.AddComponent<DiceDraggable>();
            draggable.dice = die;  // Assign the Dice object to the DiceDraggable component
        }
        else
        {
            Debug.Log("Dice is either not held or already in the pool.");
        }
    }

    // Clear all the dice from the pool (optional, can use if you want to reset the pool)
    public void ClearPool()
    {
        // Reset held dice list
        heldDice.Clear();

        // Destroy all the dice in the pool
        foreach (Transform child in poolArea)
        {
            Destroy(child.gameObject);
        }
    }
}
