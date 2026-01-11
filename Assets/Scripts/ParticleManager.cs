using System;
using System.Collections.Generic;
using UnityEngine;
public enum ParticleType
{
    DeathUnit,
    Explosion,
    HealNode,
    ExplosionTower
}
public class ParticleManager : MonoBehaviour
{
    public static ParticleManager instance;
    public List<ParticleSystemEntry> particleSystems;
    public Dictionary<ParticleType, ParticleSystemEntry> particleSystemDict = new Dictionary<ParticleType, ParticleSystemEntry>();
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        foreach(var particle in particleSystems)
        {
            particleSystemDict[particle.type] = particle;
        }
    }
    public void PlayParticleEffect(ParticleType type, Transform pos,Vector3 offset = default)
    {
        var entry = particleSystemDict[type];
        if (entry != null && entry.particleSystemPrefab != null)
        {
            ParticleSystem ps = Instantiate(entry.particleSystemPrefab, pos.position + offset, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, ps.main.duration);
        }
        else
        {
            Debug.LogWarning("Particle system for type " + type + " not found or prefab is null.");
        }
    }
    public ParticleSystem SpawnPersistentParticle(ParticleType type,Transform pos,Vector3 offset = default)
    {
        if (!particleSystemDict.ContainsKey(type))
        {
            Debug.LogWarning("Particle type not found: " + type);
            return null;
        }
        var entry = particleSystemDict[type];
        if (entry.particleSystemPrefab == null)
            return null;
        ParticleSystem ps = Instantiate(entry.particleSystemPrefab,pos.position + offset,Quaternion.identity);
        ps.Play();
        return ps;
    }
    public void StopPersistentParticle(ParticleSystem ps)
    {
        if (ps == null) return;
        Destroy(ps.gameObject);
    }

}
[Serializable]
public class ParticleSystemEntry
{
    public ParticleType type;
    public ParticleSystem particleSystemPrefab;
}
