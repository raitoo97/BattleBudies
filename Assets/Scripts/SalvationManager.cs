using System.Collections;
using System.Collections.Generic;
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
    IEnumerator UnitRollDiceSalvation(Units unit, int salvationTreshold)
    {
        Node safeNodeBeforeRoll = unit.lastSafeNode; // <-- nodo seguro antes de tirar

        diceRoll.PrepareForRoll();
        if (unit.isPlayerUnit)
        {
            CanvasManager.instance.rollClicked = false;
            CanvasManager.instance.TryShowCombatUI(playerCanRoll: true);
            yield return new WaitUntil(() => CanvasManager.instance.rollClicked);
            CanvasManager.instance.TryShowCombatUI(playerCanRoll: false);
        }
        else
        {
            CanvasManager.instance.TryShowCombatUI(playerCanRoll: true);
            yield return new WaitForSeconds(0.5f);
        }

        diceRoll.RollDice();
        yield return new WaitUntil(() => diceRoll.hasBeenCounted && diceRoll.hasBeenThrown && diceRoll.IsDiceStill());
        yield return new WaitForSeconds(0.5f);

        if (pendingSalvation >= salvationTreshold)
        {
            Debug.Log("Tirada de salvación exitosa");

            Node forwardSafeNode = NodeManager.GetForwardSafeNode(safeNodeBeforeRoll, unit.currentNode);

            if (forwardSafeNode != null)
            {
                if (!forwardSafeNode.IsEmpty())
                {
                    List<Node> neighborNodes = NodeManager.GetNeighborsInRow(forwardSafeNode);
                    Node freeNeighbor = neighborNodes.Find(n => n.IsEmpty());

                    if (freeNeighbor != null)
                    {
                        forwardSafeNode = freeNeighbor;
                        unit.lastSafeNode = forwardSafeNode; // actualizamos porque avanzó a un nodo libre
                    }
                    else
                    {
                        // No hay nodo libre adelante ni vecinos ? retrocede
                        forwardSafeNode = safeNodeBeforeRoll;
                        OnSalvingThrow = false;
                        Debug.Log("No hay nodo libre adelante ni vecinos, retrocede al safeNodeBeforeRoll");
                    }
                }
                else
                {
                    // Nodo directo libre ? actualizamos lastSafeNode
                    unit.lastSafeNode = forwardSafeNode;
                }

                unit.SetCurrentNode(forwardSafeNode);
                unit.transform.position = unit.GetSnappedPosition(forwardSafeNode);
            }
            else
            {
                Debug.Log("No hay forwardSafeNode, retrocede al safeNodeBeforeRoll");
                if (safeNodeBeforeRoll != null)
                {
                    unit.SetCurrentNode(safeNodeBeforeRoll);
                    unit.transform.position = unit.GetSnappedPosition(safeNodeBeforeRoll);
                    unit.TakeDamage(3);
                }
            }
        }
        else
        {
            Debug.Log("Tirada de salvación fallida");
            if (safeNodeBeforeRoll != null)
            {
                unit.SetCurrentNode(safeNodeBeforeRoll);
                unit.transform.position = unit.GetSnappedPosition(safeNodeBeforeRoll);
                unit.TakeDamage(3);
            }
        }

        OnSalvingThrow = false;
        CanvasManager.instance.TryShowCombatUI(playerCanRoll: false);
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
