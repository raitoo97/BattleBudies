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
            OnSalvingThrow = false;
            CanvasManager.instance.ShowCombatUI(false);
        }
        else
        {
            if (unit.lastSafeNode != null)
            {
                unit.SetCurrentNode(unit.lastSafeNode);
                unit.transform.position = unit.GetSnappedPosition(unit.lastSafeNode);
                unit.TakeDamage(3);
                CanvasManager.instance.ShowCombatUI(false);
                Debug.Log("Unidad retrocede al último nodo seguro: " + unit.lastSafeNode.name);
            }
            Debug.Log("Tirada de salvación fallida");
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
