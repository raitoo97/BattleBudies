using UnityEngine;
public class FaceDetector : MonoBehaviour
{
    [SerializeField] private int faceValueNumber;
    private bool hasBeenCounted = false;
    private DiceRoll _diceRoll;
    void Start()
    {
        _diceRoll = GetComponentInParent<DiceRoll>();
        if (_diceRoll == null)
            Debug.LogError("FaceDetector: DiceRoll no encontrado en el padre.");
    }
    public void ResetFaceDetector()
    {
        hasBeenCounted = false;
    }
    private void OnTriggerStay(Collider other)
    {
        if (hasBeenCounted) return;
        if (_diceRoll == null || ScoreManager.instance == null) return;
        if (!_diceRoll.hasBeenThrown) return;
        if (_diceRoll.IsDiceStill())
        {
            if (!_diceRoll.hasBeenCounted)
            {
                ScoreManager.instance.AddDamage(faceValueNumber);
                hasBeenCounted = true;
                _diceRoll.hasBeenCounted = true;
                Debug.Log($"Cara detectada: {faceValueNumber}");
            }
        }
    }
}
