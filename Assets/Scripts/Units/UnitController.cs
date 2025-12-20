using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class UnitController : MonoBehaviour
{
    public Units selectedUnit;
    [SerializeField]private Node selectedEndNode;
    [SerializeField]private PathDrawer pathDrawer;
    private GlowUnit hoverGlow;
    private Units hoverUnit;
    public static UnitController instance;
    private bool previousIsPlayerTurn = false;
    public bool IsSelectingUpgradeUnit = false;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    void Update()
    {
        if (!previousIsPlayerTurn && GameManager.instance.isPlayerTurn)
        {
            ResetPlayerUnitsTurnFlags();
        }
        previousIsPlayerTurn = GameManager.instance.isPlayerTurn;
        if (IsBusy())
        {
            DeselectUnit();
            pathDrawer.ClearPath();
            selectedEndNode = null;
            return;
        }
        HandleMouseHover();
        HandleMouseClick();
        HandleMoveCommand();
    }
    private void DeselectUnit()
    {
        if (selectedUnit != null)
        {
            var glow = selectedUnit.GetComponent<GlowUnit>();
            if (glow != null) glow.SetGlowOff();
            selectedUnit = null;
        }
    }
    private bool IsUnitSelectable(Units unit)
    {
        if (unit == null) return false;
        return (GameManager.instance.isPlayerTurn && unit.isPlayerUnit) || (!GameManager.instance.isPlayerTurn && !unit.isPlayerUnit);
    }
    private void HandleMouseHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Units unit = hit.collider.GetComponent<Units>();
            if (unit != hoverUnit)
            {
                if (hoverGlow != null && hoverUnit != selectedUnit)
                    hoverGlow.SetGlowOff();

                hoverUnit = unit;
                hoverGlow = unit != null ? unit.GetComponent<GlowUnit>() : null;
            }
            if (hoverGlow != null)
            {
                if (GameManager.instance.isPlayerTurn)
                {
                    if (unit.isPlayerUnit && unit != selectedUnit)
                    {
                        hoverGlow.SetGlowHover();
                    }
                    else if (!unit.isPlayerUnit)
                    {
                        hoverGlow.SetGlowEnemyHover();
                    }
                }
                else
                {
                    if (!unit.isPlayerUnit)
                        hoverGlow.SetGlowEnemyHover();
                    else
                        hoverGlow.SetGlowOff();
                }
            }
        }
        else
        {
            if (hoverGlow != null && hoverUnit != selectedUnit)
                hoverGlow.SetGlowOff();
            hoverUnit = null;
            hoverGlow = null;
        }
    }
    private void HandleMouseClick()
    {
        if (!GameManager.instance.isPlayerTurn) return;
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                UpgradeUnits(hit);
                Units unit = hit.collider.GetComponent<Units>();
                Node node = hit.collider.GetComponent<Node>();
                if (!IsUnitSelectable(unit))
                    unit = null;
                if (unit != null)
                {
                    if (selectedUnit != null)
                    {
                        var oldGlow = selectedUnit.GetComponent<GlowUnit>();
                        if (oldGlow != null)
                            oldGlow.SetGlowOff();
                    }
                    selectedUnit = unit;
                    var newGlow = selectedUnit.GetComponent<GlowUnit>();
                    if (newGlow != null)
                        newGlow.SetGlowSelected();
                    pathDrawer.ClearPath();
                    selectedEndNode = null;
                }
                else if (node != null && selectedUnit != null)
                {
                    if (!node.IsEmpty())
                    {
                        Debug.Log("Nodo ocupado, no se puede mover allí.");
                        return;
                    }
                    selectedEndNode = node;
                    pathDrawer.DrawPath(selectedUnit.currentNode, selectedEndNode, selectedUnit);
                }
                else
                {
                    DeselectUnit();
                    pathDrawer.ClearPath();
                    selectedEndNode = null;
                }
            }
        }
    }
    private void HandleMoveCommand()
    {
        if (!GameManager.instance.isPlayerTurn) return;
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (selectedUnit == null || selectedEndNode == null) return;
        List<Node> path = PathFinding.CalculateAstart(selectedUnit.currentNode, selectedEndNode);
        if (path == null || path.Count == 0) return;
        if (NodeManager.PathTouchesUnitNeighbor(path, out List<Node> dangerNodes))
        {
            Node firstDangerNode = null;
            foreach (Node node in path)
            {
                if (dangerNodes.Contains(node))
                {
                    firstDangerNode = node;
                    break;
                }
            }
            if (firstDangerNode != null)
            {
                int index = path.IndexOf(firstDangerNode);
                path = path.GetRange(0, index + 1);
            }
        }
        Node firstSalvationNode = NodeManager.FindFirstDangerousNode(path);
        if (firstSalvationNode != null)
        {
            int index = path.IndexOf(firstSalvationNode);
            if (index - 1 >= 0)
                selectedUnit.lastSafeNode = path[index - 1];
            path = path.GetRange(0, index + 1);
        }
        if (path.Count > 0)
        {
            selectedUnit.SetPath(path);
            selectedEndNode = null;
            pathDrawer.ClearPath();
            StartCoroutine(StartCombatAfterMove(selectedUnit));
        }
    }
    private IEnumerator StartCombatAfterMove(Units unit)
    {
        yield return new WaitUntil(() => unit.PathEmpty());
        if (unit.currentNode == null) yield break;
        if (unit.currentNode.IsDangerous)
        {
            SalvationManager.instance.StartSavingThrow(unit);
            yield return new WaitUntil(() => !SalvationManager.instance.GetOnSavingThrow);
        }
        if (unit == null || unit.currentNode == null)
            yield break;
        yield return StartCoroutine(RecolectResources(unit));
        yield return StartCoroutine(HealthTower(unit));
        if (TryGetEnemyNeighbor(unit, out Units enemy))
        {
            CombatManager.instance.StartCombat(unit, enemy, true);
        }
        if (TowerManager.instance.TryGetTowerAtNode(unit.currentNode, out Tower tower))
        {
            if (unit.hasAttackedTowerThisTurn)
            {
                yield break;
            }
            if (TowerManager.instance.CanUnitAttackTower(unit, tower))
            {
                CombatManager.instance.StartCombatWithTower(unit, tower);
            }
        }
    }
    private IEnumerator RecolectResources(Units unit)
    {
        if (unit is not Ranger ranger) yield break;
        if (ranger.hasCollectedThisTurn) yield break;
        if (!GetValidResourcesNodes(unit).Contains(unit.currentNode)) yield break;
        if (ResourcesManager.instance.onColectedResources) yield break;
        ranger.hasCollectedThisTurn = true;
        ResourcesManager.instance.StartRecolectedResources(ranger);
        yield return new WaitUntil(() => !ResourcesManager.instance.onColectedResources);
    }
    private IEnumerator HealthTower(Units unit)
    {
        if (unit.hasHealthedTowerThisTurn)yield break;
        if (unit is Defenders && GetValidHealthNodes(unit).Contains(unit.currentNode))
        {
            if (!HealthTowerManager.instance.onColectedHealth)
            {
                HealthTowerManager.instance.StartRecolectedHealth(unit as Defenders);
                yield return new WaitUntil(() => !HealthTowerManager.instance.onColectedHealth);
                unit.hasHealthedTowerThisTurn = true;
            }
        }
        yield return null;
    }
    private bool TryGetEnemyNeighbor(Units unit, out Units enemy)
    {
        enemy = null;
        if (unit.currentNode == null) return false;
        foreach (var neighbor in unit.currentNode.Neighbors)
        {
            if (neighbor.unitOnNode != null)
            {
                Units other = neighbor.unitOnNode.GetComponent<Units>();
                if (other != null && other.isPlayerUnit != unit.isPlayerUnit)
                {
                    enemy = other;
                    return true;
                }
            }
        }
        return false;
    }
    private void ResetPlayerUnitsTurnFlags()
    {
        Units[] allUnits = FindObjectsOfType<Units>();
        foreach (var u in allUnits)
        {
            if (u.isPlayerUnit)
                u.ResetTurnFlags();
        }
    }
    private List<Node> GetValidResourcesNodes(Units unit)
    {
        return NodeManager.GetResourcesNode().FindAll(n =>n.IsEmpty() || n == unit.currentNode);
    }
    private List<Node> GetValidHealthNodes(Units unit)
    {
        return NodeManager.GetHealthNodes().FindAll(n => n.IsEmpty() || n == unit.currentNode);
    }
    public void StartUpgradeSelection()
    {
        IsSelectingUpgradeUnit = true;
        Debug.Log("UnitController escuchando selección de unidad para upgrade.");
    }
    public void UpgradeUnits(RaycastHit hit)
    {
        if (IsSelectingUpgradeUnit)
        {
            Units clickedUnit = hit.collider.GetComponent<Units>();
            if (clickedUnit != null && clickedUnit.isPlayerUnit)
            {
                UpgradeManager.instance.ApplyUpgradeToPlayerUnit(clickedUnit);
                IsSelectingUpgradeUnit = false;
                Debug.Log($"Unidad mejorada: {clickedUnit.name}");
            }
            return;
        }
    }
    public bool IsBusy()
    {
        if (CardPlayManager.instance != null && CardPlayManager.instance.placingMode)
            return true;
        if (GameManager.instance != null && !GameManager.instance.isPlayerTurn)
            return true;
        if (CombatManager.instance != null && CombatManager.instance.GetCombatActive)
            return true;
        if (ResourcesManager.instance != null && ResourcesManager.instance.onColectedResources)
            return true;
        if (HealthTowerManager.instance != null && HealthTowerManager.instance.onColectedHealth)
            return true;
        if (Units.anyUnitMoving)
            return true;
        return false;
    }
}