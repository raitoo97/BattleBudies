using System.Collections;
using UnityEngine;
public class SalvationManager : MonoBehaviour
{
    public static SalvationManager instance;
    private bool OnSalvingThrow = false;
    private DiceRoll diceRoll;
    private int pendingSalvation;
    private void Awake()
    {
        if(instance == null)
            instance = this;
        else
            Destroy(this.gameObject);
    }
    public void StartSavingThrow(Units unit)
    {
        if(OnSalvingThrow) return;
        if(unit == null) return;
        OnSalvingThrow = true;
        diceRoll = unit.diceInstance;
        Debug.Log("Iniciando tirada de salvación");
        StartCoroutine(SavingThrowCoroutine(unit));
    }
    IEnumerator SavingThrowCoroutine(Units unit)
    {
        while (OnSalvingThrow)
        {
            yield return StartCoroutine(UnitRollDiceSalvation(unit, salvationTreshold: 3));
            if(!OnSalvingThrow) yield break;
        }
    }
    IEnumerator UnitRollDiceSalvation(Units unit , int salvationTreshold)
    {
        diceRoll.PrepareForRoll();
        if (unit.isPlayerUnit)
        {
            CanvasManager.instance.rollClicked = false;
            CanvasManager.instance.ShowCombatUI(true, playerCanRoll: true);
            yield return new WaitUntil(() => CanvasManager.instance.rollClicked);
            CanvasManager.instance.ShowCombatUI(true, playerCanRoll: false);
        }
        else
        {
            CanvasManager.instance.ShowCombatUI(true, playerCanRoll: true);
            yield return new WaitForSeconds(0.5f);
        }
        diceRoll.RollDice();
        yield return new WaitUntil(() => diceRoll.hasBeenCounted && diceRoll.hasBeenThrown && diceRoll.IsDiceStill());
        yield return new WaitForSeconds(0.5f);
        if (pendingSalvation >= salvationTreshold)
        {
            Debug.Log("Tirada de salvación exitosa");
            Node forwardSafeNode = NodeManager.GetForwardSafeNode(unit.lastSafeNode, unit.currentNode);
            if (forwardSafeNode != null)
            {
                unit.SetCurrentNode(forwardSafeNode);
                unit.transform.position = unit.GetSnappedPosition(forwardSafeNode);
            }
            else
            {
                if (unit.lastSafeNode != null)
                {
                    unit.SetCurrentNode(unit.lastSafeNode);
                    unit.transform.position = unit.GetSnappedPosition(unit.lastSafeNode);
                }
            }
            OnSalvingThrow = false;
            CanvasManager.instance.ShowCombatUI(false);
        }
        else
        {
            Debug.Log("Tirada de salvacion fallida");
            if (unit.lastSafeNode != null)
            {
                unit.SetCurrentNode(unit.lastSafeNode);
                unit.transform.position = unit.GetSnappedPosition(unit.lastSafeNode);
                unit.TakeDamage(3);
                CanvasManager.instance.ShowCombatUI(false);
            }
        }
        pendingSalvation = 0;
        diceRoll.ResetDicePosition();
        yield return new WaitForSeconds(0.3f);
    }
    public bool GetOnSavingThrow { get => OnSalvingThrow; }
    public void ChangePendingSalvation(int changeAmount)
    {
        pendingSalvation += changeAmount;
    }
}
