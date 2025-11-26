using UnityEngine;
public static class LineOfSight
{
    public static bool IsOnSight(Vector3 start, Vector3 end)
    {
        var dir = end - start;
        return !Physics.Raycast(start, dir, dir.magnitude, LayerMask.GetMask("Ground"));
    }
}
