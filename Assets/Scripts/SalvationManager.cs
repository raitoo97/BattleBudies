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
        pendingSalvation = 0;
        if (CameraFocusManager.instance != null)
        {
            CameraFocusManager.instance.FocusOnUnit(unit);
        }
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
        CanvasManager.instance.ChangeCameraText(true,"Salvation Throw", new Vector4(1.0f, 0.5f, 0.0f, 1.0f));
        yield return new WaitForSeconds(2.5f);
        CanvasManager.instance.ChangeCameraText(false);
        Node safeNodeBeforeRoll = unit.lastSafeNode;
        diceRoll.PrepareForRoll();
        if (unit.isPlayerUnit)
        {
            CanvasManager.instance.rollClicked = false;
            CanvasManager.instance.ShowSalvationUI(true, unit, playerCanRoll: true);
            yield return new WaitUntil(() => CanvasManager.instance.rollClicked);
            CanvasManager.instance.ShowSalvationUI(true, unit, playerCanRoll: false);
        }
        else
        {
            CanvasManager.instance.ShowSalvationUI(true, unit, playerCanRoll: false);
            yield return new WaitForSeconds(0.5f);
        }
        diceRoll.RollDice();
        yield return new WaitUntil(() => diceRoll.hasBeenCounted && diceRoll.hasBeenThrown && diceRoll.IsDiceStill());
        yield return new WaitForSeconds(0.5f);
        if (pendingSalvation >= salvationTreshold)
        {
            Node safeBeforeRoll = unit.lastSafeNode; // nodo seguro antes de la tirada
            Node forwardNode = NodeManager.GetForwardSafeNode(safeBeforeRoll, unit.currentNode);
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
                    }
                }
            }
            else
            {
                nodeToMove = safeBeforeRoll;
            }
            unit.SetCurrentNode(nodeToMove);
            unit.transform.position = unit.GetSnappedPosition(nodeToMove);
            if (nodeToMove != safeBeforeRoll)
                unit.lastSafeNode = nodeToMove;
        }
        else
        {
            if (safeNodeBeforeRoll != null)
            {
                SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("ExplosionImpact"), 1f, false);
                unit.SetCurrentNode(safeNodeBeforeRoll);
                unit.transform.position = unit.GetSnappedPosition(safeNodeBeforeRoll);
                unit.TakeDamage(3);
            }
        }
        OnSalvingThrow = false;
        pendingSalvation = 0;
        diceRoll.ResetDicePosition();
        yield return new WaitForSeconds(0.3f);
        CanvasManager.instance.ShowSalvationUI(false, unit);
        yield return new WaitForSeconds(0.3f);
    }
    public bool GetOnSavingThrow { get => OnSalvingThrow; }
    public void ChangePendingSalvation(int changeAmount)
    {
        pendingSalvation += changeAmount;
    }
}
