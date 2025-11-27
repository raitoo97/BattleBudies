using UnityEngine;
public class EnergyManager : MonoBehaviour
{
    public static EnergyManager instance;
    [Header("Player Energy")]
    public float maxEnergy = 10f;
    public float currentEnergy;
    [Header("Enemy Energy")]
    public float enemyMaxEnergy = 10f;
    public float enemyCurrentEnergy;
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
        enemyCurrentEnergy = enemyMaxEnergy;
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
    public void RefillPlayerEnergy()
    {
        currentEnergy = maxEnergy;
    }
    public void RefillEnemyEnergy()
    {
        enemyCurrentEnergy = enemyMaxEnergy;
    }
}
