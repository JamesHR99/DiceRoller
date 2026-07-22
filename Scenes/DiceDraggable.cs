using UnityEngine;

public class DiceDraggable : MonoBehaviour
{
    private Vector3 offset;
    private bool isDragging = false;

    public Dice dice; // This will reference the Dice object associated with this draggable dice

    private void OnMouseDown()
    {
        // Debugging: Make sure the dice is being clicked
        Debug.Log("Mouse down on dice: " + gameObject.name);

        // Check if the mouse is over the dice using a raycast
        if (IsMouseOverObject())
        {
            Debug.Log("Mouse is over the dice, starting drag...");

            isDragging = true;
            offset = transform.position - GetMouseWorldPos();
        }
    }

    private void OnMouseUp()
    {
        // Stop dragging when mouse is released
        isDragging = false;
        Debug.Log("Dice released: " + gameObject.name);
    }

    private void Update()
    {
        if (isDragging)
        {
            // Update position as the mouse moves, applying the offset to keep the drag consistent
            transform.position = GetMouseWorldPos() + offset;
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        // Convert the mouse position from screen space to world space
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private bool IsMouseOverObject()
    {
        // Check if the mouse is over the object's collider using Raycast
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero); // Raycasting from mouse position

        // Debugging: Check if hit is valid
        if (hit.collider != null)
        {
            Debug.Log("Raycast hit: " + hit.collider.gameObject.name);
        }

        return hit.collider != null && hit.collider.gameObject == gameObject;
    }
}
