using UnityEngine;

public class TestEquipment : MonoBehaviour
{
    public EquipmentItemSO startingWeapon;
    public EquipmentItemSO startingArmor;

    void Start()
    {
        PlayerEquipment.Instance.EquipItem(startingWeapon);
        PlayerEquipment.Instance.EquipItem(startingArmor);
    }
}
