using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class DiceRoll : MonoBehaviour
{
    [Header("Config")]
    public float thresholdStill = 0.05f;
    public float resetTime = 1f;
    public Transform resetPoint;
    private Rigidbody rb;
    private bool isOnTable = false;
    private float stillTimer = 0f;
    public bool hasBeenCounted = false;
    public bool hasBeenThrown = false;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        CheckIfDiceIsStill();
    }
    private void CheckIfDiceIsStill()
    {
        if (!isOnTable)
        {
            stillTimer = 0f;
            return;
        }
        if (rb.velocity.magnitude > thresholdStill || rb.angularVelocity.magnitude > thresholdStill)
        {
            stillTimer = 0f;
            return;
        }
        stillTimer += Time.deltaTime;
    }
    public void PrepareForRoll()
    {
        hasBeenThrown = false;
        hasBeenCounted = false;
        ResetDicePosition();
    }
    public void RollDice()
    {
        hasBeenThrown = true;
        hasBeenCounted = false;
        stillTimer = 0f;
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
            stillTimer = 0f;
        }
    }
    public bool IsDiceStill()
    {
        return rb.velocity.magnitude < thresholdStill && rb.angularVelocity.magnitude < thresholdStill;
    }
    public void ResetDicePosition()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = resetPoint.position;
        transform.rotation = resetPoint.rotation;
        hasBeenThrown = false;
        hasBeenCounted = false;
    }
}
