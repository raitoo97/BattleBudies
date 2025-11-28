using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class DiceRoll : MonoBehaviour
{
    [Header("Configuración")]
    public float thresholdStill = 0.05f;
    public float resetTime = 1f;
    public Transform resetPoint;
    private Rigidbody rb;
    private bool isOnTable = false;
    private bool canRoll = true;
    private float stillTimer = 0f;
    public bool hasBeenCounted = false;
    public bool hasBeenThrown = false;
    public bool CanRoll => canRoll;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        CheckIfDiceIsStill();
        if (hasBeenThrown && hasBeenCounted && IsDiceStill())
        {
            ResetDicePosition();
        }
    }
    private void CheckIfDiceIsStill()
    {
        if (!isOnTable)
        {
            canRoll = false;
            stillTimer = 0f;
            return;
        }
        if (rb.velocity.magnitude > thresholdStill || rb.angularVelocity.magnitude > thresholdStill)
        {
            stillTimer = 0f;
            canRoll = false;
            return;
        }
        stillTimer += Time.deltaTime;
        if (stillTimer >= resetTime)
        {
            canRoll = true;
        }
    }
    public void RollDice()
    {
        if (!canRoll)
        {
            Debug.Log("No puedo tirar todavía.");
            return;
        }
        hasBeenThrown = true;
        canRoll = false;
        stillTimer = 0f;
        hasBeenCounted = false;
        foreach (FaceDetector fd in GetComponentsInChildren<FaceDetector>())
            fd.ResetFaceDetector();
        rb.AddForce(new Vector3(0, 12f, 0), ForceMode.Impulse);
        float torqueX = Random.Range(30f, 80f);
        float torqueY = Random.Range(30f, 80f);
        float torqueZ = Random.Range(30f, 80f);
        Vector3 randomTorque = new Vector3(torqueX, torqueY, torqueZ);
        rb.AddTorque(randomTorque, ForceMode.Impulse);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.isTrigger && collision.gameObject.CompareTag("Mesa"))
        {
            isOnTable = true;
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (!collision.collider.isTrigger && collision.gameObject.CompareTag("Mesa"))
        {
            isOnTable = false;
            canRoll = false;
            stillTimer = 0f;
        }
    }
    public bool IsDiceStill()
    {
        return rb.velocity.magnitude < thresholdStill &&
               rb.angularVelocity.magnitude < thresholdStill;
    }
    private void ResetDicePosition()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = resetPoint.position;
        transform.rotation = resetPoint.rotation;
        hasBeenThrown = false;
        hasBeenCounted = false;
        canRoll = true;
    }
}
