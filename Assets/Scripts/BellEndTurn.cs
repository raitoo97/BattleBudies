using UnityEngine;
public class BellEndTurn : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (!GameManager.instance.isPlayerTurn ||GameManager.instance.IsAnyPlayerUnitMoving() ||CombatManager.instance.GetCombatActive ||CardPlayManager.instance.placingMode)
            return;
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("DingSong"), 1f, false);
        GameManager.instance.PlayerRequestsEndTurn();
    }
}