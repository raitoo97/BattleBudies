using System.Collections;
using UnityEngine;
public class HealthTowerManager : MonoBehaviour
{
    public static HealthTowerManager instance;
    [HideInInspector]public bool onColectedHealth;
    private DiceRoll diceRoll;
    private int pendingHealth;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this.gameObject);
    }
    public void AddHealthTower(bool isPlayer, int amount)
    {
        if (isPlayer)
        {
            Debug.Log("Se han añadido " + amount + " de salud a la torre del jugador");
        }
        else
        {
            Debug.Log("Se han añadido " + amount + " de salud a la torre del enemigo");
            var GetLowestHealthTower = TowerManager.instance.GetEnemyTower();
            if(GetLowestHealthTower == null)return;
            GetLowestHealthTower.Healt(amount);
        }
    }
    public void StartRecolectedHealth(Defenders defender)
    {
        if (onColectedHealth) return;
        if (defender == null) return;
        onColectedHealth = true;
        diceRoll = defender.diceInstance;
        StartCoroutine(DefenderRollDiceHealth(defender));
        Debug.Log("Iniciando tirada de recolección de salud");
    }
    IEnumerator DefenderRollDiceHealth(Defenders defender)
    {
        if (defender == null)
        {
            onColectedHealth = false;
            yield break;
        }
        int numberOfDiceToRoll = defender.healthTowerDice;
        for(int i = 0; i < numberOfDiceToRoll; i++)
        {
            diceRoll.PrepareForRoll();
            if (defender.isPlayerUnit)
            {
                CanvasManager.instance.rollClicked = false;
                CanvasManager.instance.TryShowCombatUI(playerCanRoll: true);
                yield return new WaitUntil(() => CanvasManager.instance.rollClicked);
                CanvasManager.instance.TryShowCombatUI(playerCanRoll: false);
            }
            else
            {
                CanvasManager.instance.TryShowCombatUI(playerCanRoll: false);
                yield return new WaitForSeconds(0.5f);
            }
            diceRoll.RollDice();
            yield return new WaitUntil(() => diceRoll.hasBeenThrown && diceRoll.hasBeenCounted && diceRoll.IsDiceStill());
            print("Tirada de vida " + (i + 1) + " de " + defender.healthTowerDice + ": " + pendingHealth + " vida pendientes.");
            if (pendingHealth > 0)
            {
                if (defender.isPlayerUnit)
                {
                    yield return StartCoroutine(WaitForPlayerSelectTower((Tower selectedTower) =>
                    {
                        if (selectedTower != null)
                        {
                            selectedTower.Healt(pendingHealth);
                            Debug.Log("Curaste la torre: " + selectedTower.name + " con " + pendingHealth + " de vida.");
                        }
                    }));
                }
                else
                {
                    AddHealthTower(isPlayer: false, pendingHealth);
                }
            }
            pendingHealth = 0;
            diceRoll.PrepareForRoll();
            yield return new WaitForSeconds(0.3f);
        }
        onColectedHealth = false;
    }
    public void ChangePendingHealth(int amount)
    {
        pendingHealth += amount;
    }
    public IEnumerator WaitForPlayerSelectTower(System.Action<Tower> callback)
    {
        Tower selected = null;
        while (selected == null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Tower t = hit.collider.GetComponent<Tower>();
                    if (t != null && t.faction == Faction.Player)
                    {
                        selected = t;
                    }
                }
            }
            yield return null;
        }
        callback?.Invoke(selected);
    }
}
