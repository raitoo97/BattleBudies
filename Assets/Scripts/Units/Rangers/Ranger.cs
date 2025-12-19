using UnityEngine;
public abstract class Ranger : Units
{
    [Header("RangerAbility")]
    public int resourcesDice;
    public bool hasCollectedThisTurn = false;
    override protected void Start()
    {
        base.Start();
    }
    override protected void Update()
    {
        base.Update();
    }
}
