using UnityEngine;
public abstract class Defenders : Units
{
    [Header("DefenderAbility")]
    public int healthTowerDice;
    private ParticleSystem healParticles;
    override protected void Start()
    {
        base.Start();
    }
    override protected void Update()
    {
        base.Update();
    }
    public void OnEnterHealNode()
    {
        if (healParticles != null) return;
        healParticles = ParticleManager.instance.SpawnPersistentParticle(ParticleType.HealNode,transform);
    }
    public void OnExitHealNode()
    {
        if (healParticles == null) return;
        ParticleManager.instance.StopPersistentParticle(healParticles);
        healParticles = null;
    }
}
