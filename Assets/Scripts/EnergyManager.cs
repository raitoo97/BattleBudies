using UnityEngine;
public class EnergyManager : MonoBehaviour
{
    public static EnergyManager instance;
    public float maxEnergy = 10f;
    public float currentEnergy;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        currentEnergy = maxEnergy;
    }
    public bool TryConsumeEnergy(float amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            return true;
        }
        return false;
    }
    public void AddEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
    }
}
