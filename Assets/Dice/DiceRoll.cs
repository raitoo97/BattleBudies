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
    [Header("Roll Settings")]
    public float upwardForce;          // Fuerza vertical para lanzar
    public Vector2 torqueRange;// Torques aleatorios para giro
    public Vector2 horizontalForceRange;// Impulso horizontal
    public float extraGravity;     // Gravedad extra para simular caída rápida
    [Header("Sound")]
    public float minImpactVelocity = 0.5f;
    public float soundCooldown = 0.1f;
    private float lastSoundTime = 0f;
    private bool resetSoundPlayed = false;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.None;
    }
    void Update()
    {
        CheckIfDiceIsStill();
    }
    void FixedUpdate()
    {
        if (hasBeenThrown)
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
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
        if (!resetSoundPlayed)
        {
            SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("DiceReset"),1f,false);
            resetSoundPlayed = true;
        }
    }
    public void RollDice()
    {
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.WakeUp();
        hasBeenThrown = true;
        hasBeenCounted = false;
        stillTimer = 0f;
        resetSoundPlayed = false;
        foreach (FaceDetector fd in GetComponentsInChildren<FaceDetector>())
            fd.ResetFaceDetector();
        Vector3 force = new Vector3(Random.Range(horizontalForceRange.x, horizontalForceRange.y), upwardForce, Random.Range(horizontalForceRange.x, horizontalForceRange.y));
        rb.AddForce(force, ForceMode.Impulse);
        Vector3 randomTorque = new Vector3(
            Random.Range(torqueRange.x, torqueRange.y),
            Random.Range(torqueRange.x, torqueRange.y),
            Random.Range(torqueRange.x, torqueRange.y)
        );
        rb.AddTorque(randomTorque, ForceMode.Impulse);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.isTrigger && collision.gameObject.CompareTag("Mesa"))
        {
            isOnTable = true;
        }

        float impactStrength = collision.relativeVelocity.magnitude;
        if (impactStrength >= minImpactVelocity && Time.time - lastSoundTime > soundCooldown)
        {
            PlayDiceImpactSound(impactStrength);
            lastSoundTime = Time.time;
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("WallDice"))
        {
            // Solo si el dado ya fue lanzado
            if (hasBeenThrown && rb.velocity.magnitude < 4f)
            {
                Debug.Log("Dado golpeó la pared y está casi quieto, aplicando impulso.");
                // Dirección física real (alejarse de la pared)
                Vector3 kickDir = collision.contacts[0].normal;
                // Pequeño impulso
                rb.AddForce(kickDir * 5f, ForceMode.Impulse);
                // Torque para romper apoyos raros
                Vector3 randomTorque = Random.insideUnitSphere * 1f;
                rb.AddTorque(randomTorque, ForceMode.Impulse);
            }
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
        rb.interpolation = RigidbodyInterpolation.None;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = resetPoint.position;
        rb.rotation = resetPoint.rotation;
        rb.Sleep();
        isOnTable = false;
        stillTimer = 0f;
        hasBeenThrown = false;
        hasBeenCounted = false;
    }
    private void PlayDiceImpactSound(float impact)
    {
        float volume = Mathf.Clamp01(impact / 5f);
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("DiceHit"),volume,false);
    }
}
