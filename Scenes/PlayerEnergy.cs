using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerEnergy : MonoBehaviour
{
    public static PlayerEnergy Instance { get; private set; }

    [Header("Energy Settings")]
    [SerializeField] private int maxEnergy = 6;
    [SerializeField] private int energyPerTurn = 2;
    private int currentEnergy;
    private int temporaryEnergy = 0;
    private int pendingTemporaryEnergy = 0;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI energyText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReacquireUIReferences();
        currentEnergy = maxEnergy;
        Debug.Log($"[OnSceneLoaded] PlayerEnergy reset to {currentEnergy}/{maxEnergy}. Temp preserved: {temporaryEnergy}, Pending preserved: {pendingTemporaryEnergy}");
        UpdateUI();
    }

    private void ReacquireUIReferences()
    {
        if (energyText == null)
        {
            var allTexts = FindObjectsOfType<TextMeshProUGUI>();
            foreach (var text in allTexts)
            {
                if (text.name == "EnergyText" || text.name == "Energy Text")
                {
                    energyText = text;
                    Debug.Log("Found Energy UI text in new scene.");
                    break;
                }
            }
        }
    }

    void Start()
    {
        currentEnergy = maxEnergy;
        Debug.Log($"[PlayerEnergy.Start] Energy initialized to {currentEnergy}/{maxEnergy}");
        UpdateUI();
    }

    public void AddEnergyForNewTurn()
    {
        Debug.Log($"[AddEnergyForNewTurn] BEFORE - Current: {currentEnergy}, Temp: {temporaryEnergy}, Pending: {pendingTemporaryEnergy}, energyPerTurn: {energyPerTurn}");
        
        temporaryEnergy = pendingTemporaryEnergy;
        pendingTemporaryEnergy = 0;
        
        int energyBefore = currentEnergy;
        currentEnergy += energyPerTurn;
        if (currentEnergy > maxEnergy) { currentEnergy = maxEnergy; }
        
        Debug.Log($"[AddEnergyForNewTurn] AFTER - Current: {energyBefore} + {energyPerTurn} = {currentEnergy}/{maxEnergy}, Temp: {temporaryEnergy}, Pending: {pendingTemporaryEnergy}");
        UpdateUI();
    }

    public void AddEnergy(int amount)
    {
        if (amount <= 0) return;

        int before = currentEnergy;
        currentEnergy += amount;
        if (currentEnergy > maxEnergy)
        {
            currentEnergy = maxEnergy;
        }
        Debug.Log($"Player recharged {amount} energy. Current Energy: {currentEnergy}/{maxEnergy}");
        UpdateUI();
    }

    public void AddTemporaryEnergy(int amount)
    {
        if (amount <= 0) return;
        
        pendingTemporaryEnergy += amount;
        Debug.Log($"Player will gain {amount} temporary energy next turn! Total pending: {pendingTemporaryEnergy}");
        UpdateUI();
    }

    public bool CanSelectDie() 
    { 
        return (currentEnergy + temporaryEnergy) > 0; 
    }
    
    public void OnDieSelected() 
    { 
        if (!CanSelectDie()) return;
        
        if (temporaryEnergy > 0)
        {
            temporaryEnergy--;
            Debug.Log($"[OnDieSelected] Used temporary energy. Remaining temp: {temporaryEnergy}, Current: {currentEnergy}");
        }
        else if (currentEnergy > 0)
        {
            currentEnergy--;
            Debug.Log($"[OnDieSelected] Used normal energy. Current: {currentEnergy}, Temp: {temporaryEnergy}");
        }
        
        UpdateUI(); 
    }
    
    public void OnDieDeselected() 
    { 
        int totalEnergy = currentEnergy + temporaryEnergy;
        int totalMax = maxEnergy;
        
        if (totalEnergy < totalMax)
        {
            currentEnergy++;
            Debug.Log($"[OnDieDeselected] Refunded energy. Current: {currentEnergy}, Temp: {temporaryEnergy}");
        }
        else
        {
            Debug.Log($"[OnDieDeselected] At max, no refund. Current: {currentEnergy}, Temp: {temporaryEnergy}");
        }
        UpdateUI(); 
    }
    
    private void UpdateUI() 
    { 
        if (energyText != null) 
        { 
            string tempEnergyDisplay = temporaryEnergy > 0 ? $" <color=#FFFF00>(+{temporaryEnergy} temp)</color>" : "";
            string pendingDisplay = pendingTemporaryEnergy > 0 ? $" <color=#00FFFF>[+{pendingTemporaryEnergy} next turn]</color>" : "";
            energyText.text = $"Energy: {currentEnergy} / {maxEnergy}{tempEnergyDisplay}{pendingDisplay}"; 
            
            Debug.Log($"UI Updated - Current: {currentEnergy}/{maxEnergy}, Temp: {temporaryEnergy}, Pending: {pendingTemporaryEnergy}");
        } 
    }
}