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
    private ParticleSystem _healtParticles;
    private ParticleSystem _damageParticles;
    private void Awake()
    {
        glow = GetComponent<GlowTower>();
        if (glow == null)
        {
            Debug.LogWarning("GlowTower no asignado en " + name);
        }
        _healtParticles = GetComponentInChildren<ParticleSystem>();
        if (_healtParticles == null)
        {
            Debug.LogWarning("ParticleSystem de curación no asignado en " + name);
        }
        _damageParticles = transform.GetChild(1).GetComponent<ParticleSystem>();
        if (_damageParticles == null)
        {
            Debug.LogWarning("ParticleSystem de destruccion no asignado en " + name);
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
        if (_damageParticles != null)
        {
            _damageParticles.Play();
        }
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("TowerImpact"), 1f, false);
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
        if (_healtParticles != null)
        {
            _healtParticles.Play();
        }
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("HealtTower"), 1.0f, false);
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
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