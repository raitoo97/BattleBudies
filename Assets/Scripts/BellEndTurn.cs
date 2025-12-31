using UnityEngine;
public class BellEndTurn : MonoBehaviour
{
    private Animator _animator;
    public static BellEndTurn instance;
    private void Awake()
    {
        if(instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        _animator = GetComponent<Animator>();
    }
    private void OnMouseDown()
    {
        if (IsBusy()) return;
        _animator.SetTrigger("Ring");
    }
    public bool IsBusy()
    {
        return !GameManager.instance.isPlayerTurn || GameManager.instance.IsAnyPlayerUnitMoving() || CombatManager.instance.GetCombatActive || CardPlayManager.instance.placingMode || SalvationManager.instance.GetOnSavingThrow || ResourcesManager.instance.onColectedResources || HealthTowerManager.instance.onColectedHealth;
    }
    public void ChangeTurn()
    {
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("DingSong"), 1f, false);
        if (GameManager.instance.isPlayerTurn)
            GameManager.instance.PlayerRequestsEndTurn();
        else
            GameManager.instance.StartPlayerTurn();
        _animator.ResetTrigger("Ring");
    }
    public void RingFromIA()
    {
        _animator.SetTrigger("Ring");
    }
}