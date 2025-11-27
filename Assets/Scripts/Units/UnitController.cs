using UnityEngine;
public class UnitController : MonoBehaviour
{
    public Units selectedUnit;
    [SerializeField]private Node selectedEndNode;
    [SerializeField]private PathDrawer pathDrawer;
    private GlowUnit hoverGlow;
    private Units hoverUnit;
    void Update()
    {
        HandleMouseHover();
        HandleMouseClick();
        HandleMoveCommand();
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
                if (hoverGlow != null && unit != selectedUnit)
                    hoverGlow.SetGlowHover();
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
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Units unit = hit.collider.GetComponent<Units>();
                Node node = hit.collider.GetComponent<Node>();
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
                    selectedEndNode = node;
                    pathDrawer.DrawPath(selectedUnit.currentNode, selectedEndNode);
                }
                else
                {
                    if (selectedUnit != null)
                    {
                        var glow = selectedUnit.GetComponent<GlowUnit>();
                        if (glow != null)
                            glow.SetGlowOff();
                        selectedUnit = null;
                    }

                    pathDrawer.ClearPath();
                    selectedEndNode = null;
                }
            }
        }
    }
    private void HandleMoveCommand()
    {
        if (Input.GetKeyDown(KeyCode.Space) && selectedUnit != null && selectedEndNode != null)
        {
            var path = PathFinding.CalculateAstart(selectedUnit.currentNode, selectedEndNode);
            selectedUnit.SetPath(path);
            selectedEndNode = null;
            pathDrawer.ClearPath();
        }
    }
}
