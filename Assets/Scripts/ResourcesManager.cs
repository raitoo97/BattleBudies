using System.Collections;
using UnityEngine;
public class ResourcesManager : MonoBehaviour
{
    public static ResourcesManager instance;
    [HideInInspector]public int resourcesEnemy;
    [HideInInspector]public int resourcesPlayer;
    [HideInInspector]public bool onColectedResources;
    private DiceRoll diceRoll;
    private int pendingResources;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this.gameObject);
    }
    public void AddResources(bool isPlayer, int amount)
    {
        if (isPlayer)
        {
            Debug.Log("Se han añadido " + amount + " recursos al jugador");
            resourcesPlayer += amount;
        }
        else
        {
            Debug.Log("Se han añadido " + amount + " recursos al enemigo");
            resourcesEnemy += amount;
        }
    }
    public void StartRecolectedResources(Ranger ranger)
    {
        if(onColectedResources) return;
        if(ranger == null) return;
        onColectedResources = true;
        diceRoll = ranger.diceInstance;
        StartCoroutine(RangerRollDiceResources(ranger));
        Debug.Log("Iniciando tirada de recolección");
    }
    IEnumerator RangerRollDiceResources(Ranger ranger)
    {
        if(ranger == null)
        {
            onColectedResources = false;
            yield break;
        }
        int numberOfDiceToRoll = ranger.resourcesDice;
        for (int i = 0; i < numberOfDiceToRoll; i++)
        {
            diceRoll.PrepareForRoll();
            if (ranger.isPlayerUnit)
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
            print("Tirada de recurso " + (i + 1) + " de " + ranger.resourcesDice + ": " + pendingResources + " recursos pendientes.");
            if (pendingResources > 0)
            {
                if (ranger.isPlayerUnit)
                    AddResources(true, pendingResources);
                else
                    AddResources(false, pendingResources);
            }
            pendingResources = 0;
            diceRoll.ResetDicePosition();
            yield return new WaitForSeconds(0.3f);
        }
        onColectedResources = false;
    }
    public void ChangePendingResources(int changeAmount)
    {
        pendingResources += changeAmount;
    }
}
