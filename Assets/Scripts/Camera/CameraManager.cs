using System.Collections;
using UnityEngine;
public class CameraManager : MonoBehaviour
{
    [SerializeField] private Transform[] _cameraPositions;
    private int positions;
    [SerializeField]private bool CanTransposed;
    private Coroutine coroutine;
    public static CameraManager instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this.gameObject);
        CanTransposed = true;
        positions = 0 ;
        coroutine = null;
    }
    private void Start()
    {
        this.transform.position = _cameraPositions[positions].position;
        this.transform.rotation = _cameraPositions[positions].rotation;
    }
    public void ChangeCameras()
    {
        if (CanTransposed)
        {
            SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("CameraMove"), 0.2f, false);
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
        float duration = 0.5f;
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
    public bool GetCanTransposed { get => CanTransposed; set => CanTransposed = value; }
}
