using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LevelSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject levelChoiceButtonPrefab;
    public Transform optionsContainer;

    public void Show(List<LevelSO> levelChoices)
    {
        foreach (Transform child in optionsContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (LevelSO level in levelChoices)
        {
            GameObject buttonGO = Instantiate(levelChoiceButtonPrefab, optionsContainer);
            Button button = buttonGO.GetComponent<Button>();

            buttonGO.transform.Find("LevelNameText").GetComponent<TextMeshProUGUI>().text = level.levelName;
            buttonGO.transform.Find("LevelDescriptionText").GetComponent<TextMeshProUGUI>().text = level.levelDescription;
            buttonGO.transform.Find("LevelIcon").GetComponent<Image>().sprite = level.levelIcon;

            button.onClick.AddListener(() => {
                OnLevelSelected(level);
            });
        }

        // Show the panel
        gameObject.SetActive(true);

        // --- NEW: Pause the game ---
        Debug.Log("Game Paused for level selection.");
        Time.timeScale = 0f;
    }

    private void OnLevelSelected(LevelSO selectedLevel)
    {
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.LoadLevel(selectedLevel.sceneName);
        }
        else
        {
            Debug.LogError("Could not find LevelManager in the scene!");
        }
    }
}