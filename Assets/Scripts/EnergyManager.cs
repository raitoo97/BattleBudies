using UnityEngine;
public class EnergyManager : MonoBehaviour
{
    public static EnergyManager instance;
    [Header("Player Energy")]
    public int maxEnergy = 10;
    public int currentEnergy;
    [Header("Enemy Energy")]
    public int enemyMaxEnergy = 10;
    public int enemyCurrentEnergy;
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
    public bool TryConsumeEnergy(int amount, bool isPlayer)
    {
        if (isPlayer)
        {
            if (currentEnergy >= amount)
            {
                currentEnergy -= amount;
                return true;
            }
            return false;
        }
        else
        {
            if (enemyCurrentEnergy >= amount)
            {
                enemyCurrentEnergy -= amount;
                return true;
            }
            return false;
        }
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