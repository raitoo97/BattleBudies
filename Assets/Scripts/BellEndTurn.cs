using UnityEngine;
public class BellEndTurn : MonoBehaviour
{
    private Animator _animator;
    public static BellEndTurn instance;
    [SerializeField] private LayerMask bellLayer;
    [SerializeField] private Material outline;
    private bool isHovered = false;
    [SerializeField] private float glowThickness = 3.15f;
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
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, bellLayer))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    HandleClick();
                }
            }
        }
        HandleHover();
    }
    private void HandleClick()
    {
        if (IsBusy()) return;
        _animator.SetTrigger("Ring");
    }
    public bool IsBusy()
    {
        return PauseManager.instance.on_pause ||!GameManager.instance.isPlayerTurn || GameManager.instance.IsAnyPlayerUnitMoving() || CombatManager.instance.GetCombatActive || CardPlayManager.instance.placingMode || SalvationManager.instance.GetOnSavingThrow || ResourcesManager.instance.onColectedResources || HealthTowerManager.instance.onColectedHealth;
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
    private void HandleHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool currentlyHovered = Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, bellLayer);
        if (currentlyHovered != isHovered)
        {
            isHovered = currentlyHovered;
            if (isHovered)
            {
                if (outline == null) return;
                outline.SetInt("_ActivateGlow", 1);
                outline.SetFloat("_GlowIntensity", 1f);
                bool canPlayerBell = !IsBusy() && GameManager.instance.isPlayerTurn;
                outline.SetColor("_GlowColor", canPlayerBell? Color.green : Color.red);
                outline.SetFloat("_OutLineThickness", glowThickness);
            }
            else
            {
                if (outline == null) return;
                outline.SetFloat("_GlowIntensity", 0f);
                outline.SetInt("_ActivateGlow", 0);
            }
        }
    }
}