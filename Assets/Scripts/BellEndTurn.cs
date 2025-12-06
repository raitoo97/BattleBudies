using UnityEngine;
public class BellEndTurn : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (IsBusy())
            return;
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("DingSong"), 1f, false);
        GameManager.instance.PlayerRequestsEndTurn();
    }
    public bool IsBusy()
    {
        return !GameManager.instance.isPlayerTurn || GameManager.instance.IsAnyPlayerUnitMoving() || CombatManager.instance.GetCombatActive || CardPlayManager.instance.placingMode || SalvationManager.instance.GetOnSavingThrow || ResourcesManager.instance.onColectedResources || HealthTowerManager.instance.onColectedHealth;
    }
}