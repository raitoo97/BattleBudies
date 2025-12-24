using UnityEngine;
public enum Faction
{
    Player,
    Enemy
}
[RequireComponent(typeof(GlowTower))]
public class Tower : MonoBehaviour
{
    public Faction faction;
    public int maxHealth;
    public int currentHealth;
    public bool isDestroyed = false;
    private GlowTower glow;
    private bool isHovered = false;
    private void Awake()
    {
        glow = GetComponent<GlowTower>();
        if (glow == null)
        {
            Debug.LogWarning("GlowTower no asignado en " + name);
        }
    }
    private void Update()
    {
        HandleHover();
    }
    private void Start()
    {
        currentHealth = maxHealth;
    }
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0 && !isDestroyed)
        {
            SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("TowerDestroyed"), 1.0f, false);
            isDestroyed = true;
            Debug.Log($"{name} ha sido destruida");
            TowerManager.instance.NotifyTowerDestroyed(this);
            Destroy(gameObject);
        }
    }
    public void Healt(int amount)
    {
        currentHealth += amount;
    }
    private void HandleHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool currentlyHovered = Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject;
        if (currentlyHovered != isHovered)
        {
            isHovered = currentlyHovered;
            if (isHovered)
            {
                glow?.SetGlowHover(faction);
                Cursor.visible = false;
            }
            else
            {
                glow?.SetGlowOff();
                Cursor.visible = true;
            }
        }
    }
}