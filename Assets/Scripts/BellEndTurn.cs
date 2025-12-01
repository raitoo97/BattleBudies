using UnityEngine;
public class BellEndTurn : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (!GameManager.instance.isPlayerTurn) return;
        GameManager.instance.PlayerRequestsEndTurn();
    }
}
