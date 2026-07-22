using UnityEngine;

public class PersistentObject : MonoBehaviour
{
    void Awake()
    {
        // Tell Unity to not destroy this GameObject when loading new scenes.
        DontDestroyOnLoad(gameObject);
    }
}