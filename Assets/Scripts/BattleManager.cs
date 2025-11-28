using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    public void StartBattle(List<Units> unitsInBattle)
    {
        StartCoroutine(BattleCoroutine(unitsInBattle));
    }
    private IEnumerator BattleCoroutine(List<Units> unitsInBattle)
    {
        Debug.Log("¡Comienza la batalla!");
        foreach (Units attacker in unitsInBattle)
        {
            int damage = attacker.RollDamage();
            Debug.Log($"{attacker.name} lanza {attacker.diceCount} dados y hace {damage} de daño.");
            foreach (Units target in unitsInBattle)
            {
                if (target.isPlayerUnit != attacker.isPlayerUnit)
                {
                    target.TakeDamage(damage);
                    Debug.Log($"{target.name} recibe {damage} de daño. HP restante: {target.currentHealth}");
                }
            }
            yield return new WaitForSeconds(1f); // espera entre ataques
        }
        Debug.Log("¡Batalla finalizada!");
    }
}
