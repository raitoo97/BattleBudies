using System.Collections;
using UnityEngine;
public class CameraManager : MonoBehaviour
{
    [SerializeField] private Transform[] _cameraPositions;
    private int positions;
    private bool CanTransposed;
    private Coroutine coroutine;
    private void Awake()
    {
        CanTransposed = true;
        positions = 0 ;
        coroutine = null;
    }
    public void ChangeCameras()
    {
        if (CanTransposed)
        {
            positions++;
            if (positions < _cameraPositions.Length)
            {
                if(coroutine == null)
                    coroutine = StartCoroutine(AnimationMove());

            }
            else
            {
                positions = 0;
                if (coroutine == null)
                    coroutine = StartCoroutine(AnimationMove());
            }
        }
    }
    private IEnumerator AnimationMove()
    {
        CanTransposed = false;
        float t = 0;
        float duration = 0.3f;
        while(t < duration)
        {
            this.transform.position = Vector3.Lerp(this.transform.position, _cameraPositions[positions].position, t / duration);
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, _cameraPositions[positions].rotation, t / duration);
            yield return null;
            t += Time.deltaTime;
        }
        this.transform.position = _cameraPositions[positions].position;
        this.transform.rotation = _cameraPositions[positions].rotation;
        CanTransposed = true;
        yield return null;
        coroutine = null;
    }
}
