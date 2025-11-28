using UnityEngine;
public class UnitController : MonoBehaviour
{
    public Units selectedUnit;
    [SerializeField]private Node selectedEndNode;
    [SerializeField]private PathDrawer pathDrawer;
    private GlowUnit hoverGlow;
    private Units hoverUnit;
    public static UnitController instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    void Update()
    {
        if (CardPlayManager.instance != null && CardPlayManager.instance.placingMode)
        {
            DeselectUnit();
            pathDrawer.ClearPath();
            selectedEndNode = null;
            return;
        }
        if (!GameManager.instance.isPlayerTurn)
        {
            DeselectUnit();
            pathDrawer.ClearPath();
            selectedEndNode = null;
            HandleMouseHover();
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
                    pathDrawer.DrawPath(selectedUnit.currentNode, selectedEndNode);
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
        if (Input.GetKeyDown(KeyCode.Space) && selectedUnit != null && selectedEndNode != null)
        {
            var path = PathFinding.CalculateAstart(selectedUnit.currentNode, selectedEndNode);
            selectedUnit.SetPath(path);
            selectedEndNode = null;
            pathDrawer.ClearPath();
        }
    }
}
