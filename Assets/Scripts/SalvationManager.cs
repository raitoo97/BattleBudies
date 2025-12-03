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
            Node safeBeforeRoll = unit.lastSafeNode; // nodo seguro antes de la tirada
            Node forwardNode = NodeManager.GetForwardSafeNode(safeBeforeRoll, unit.currentNode);
            print("Nodo seguro antes de la tirada: " + safeBeforeRoll);
            print("Nodo siguiente antes de la tirada: " + forwardNode);
            Node nodeToMove = null;
            if (forwardNode != null)
            {
                if (forwardNode.IsEmpty())
                {
                    nodeToMove = forwardNode;
                }
                else
                {
                    List<Node> neighbors = NodeManager.GetNeighborsInRow(forwardNode);
                    Node freeNeighbor = neighbors.Find(n => n.IsEmpty());
                    if (freeNeighbor != null)
                    {
                        nodeToMove = freeNeighbor;
                    }
                    else
                    {
                        nodeToMove = safeBeforeRoll;
                        Debug.Log("No hay nodo libre adelante ni vecinos, retrocede al nodo seguro anterior");
                    }
                }
            }
            else
            {
                nodeToMove = safeBeforeRoll;
                Debug.Log("No hay forwardSafeNode, retrocede al nodo seguro anterior");
            }
            unit.SetCurrentNode(nodeToMove);
            unit.transform.position = unit.GetSnappedPosition(nodeToMove);
            if (nodeToMove != safeBeforeRoll)
                unit.lastSafeNode = nodeToMove;
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
