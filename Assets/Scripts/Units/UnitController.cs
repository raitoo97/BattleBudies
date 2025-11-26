using UnityEngine;
public class UnitController : MonoBehaviour
{
    public Units selectedUnit;
    [SerializeField]private Node selectedEndNode;
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Units unit = hit.collider.GetComponent<Units>();
                Node node = hit.collider.GetComponent<Node>();
                if (unit != null) selectedUnit = unit;
                if (node != null) selectedEndNode = node;
            }
        }
        if (Input.GetKeyDown(KeyCode.Space) && selectedUnit != null && selectedEndNode != null)
        {
            var path = PathFinding.CalculateAstart(selectedUnit.currentNode, selectedEndNode);
            selectedUnit.SetPath(path);
            selectedEndNode = null;
        }
    }
}
