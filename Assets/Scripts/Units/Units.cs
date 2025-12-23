using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(GlowUnit))]
public abstract class Units : MonoBehaviour
{
    public static bool anyUnitMoving = false;
    [Header("Stats")]
    public int currentHealth;
    public int maxHealth;
    public int diceCount = 1;
    [Header("Ownership")]
    public bool isPlayerUnit = true;
    [Header("Movement")]
    public Node currentNode;
    private Node targetNode;
    public Node lastSafeNode;
    private List<Node> path = new List<Node>();
    public float moveSpeed = 5f;
    private float arriveThreshold = 0.1f;
    private GlowUnit _glow;
    private float originalY;
    [Header("Dice")]
    public DiceRoll diceInstance;
    [HideInInspector]public bool hasAttackedTowerThisTurn = false;
    [HideInInspector]public bool hasHealthedTowerThisTurn = false;
    public bool pendingTask = false;
    protected virtual void Start()
    {
        currentNode = NodeManager.GetClosetNode(transform.position);
        targetNode = null;
        _glow = GetComponent<GlowUnit>();
        originalY = transform.position.y;
        if (currentNode != null)
        {
            Vector3 snap = currentNode.transform.position;
            snap.y = originalY;
            transform.position = snap;
        }
        currentHealth = maxHealth;
    }
    protected virtual void Update()
    {
        FollowPath();
    }
    public void SetPath(List<Node> newPath)
    {
        if (newPath == null || newPath.Count == 0) return;
        path = new List<Node>(newPath);
        targetNode = path[0];
    }
    private void FollowPath()
    {
        if (path == null || path.Count == 0)
        {
            Node closest = NodeManager.GetClosetNode(transform.position);
            if (closest != null && closest != currentNode)
                SetCurrentNode(closest);
            anyUnitMoving = false;
            return;
        }
        anyUnitMoving = true;
        Node nextNode = path[0];
        Vector3 targetPos = nextNode.transform.position;
        if (targetPos.y < originalY)
            targetPos.y = originalY;
        Vector3 moveDelta = (targetPos - transform.position);
        float step = moveSpeed * Time.deltaTime;
        if (moveDelta.magnitude <= step)
        {
            transform.position = GetSnappedPosition(nextNode);
        }
        else
        {
            transform.position += moveDelta.normalized * step;
            if (transform.position.y < originalY)
                transform.position = new Vector3(transform.position.x, originalY, transform.position.z);
        }
        Vector2 posXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 targetXZ = new Vector2(targetPos.x, targetPos.z);
        if (Vector2.Distance(posXZ, targetXZ) <= arriveThreshold)
        {
            int requiredEnergy = 1;
            bool isPlayerTurn = GameManager.instance.isPlayerTurn;
            if (EnergyManager.instance.TryConsumeEnergy(requiredEnergy, isPlayerTurn))
            {
                SetCurrentNode(nextNode);
                path.RemoveAt(0);
                targetNode = path.Count > 0 ? path[0] : null;
                transform.position = GetSnappedPosition(currentNode);
                CanvasManager.instance.UpdateEnergyUI();
                if (path.Count == 0)
                {
                    Node closest = NodeManager.GetClosetNode(transform.position);
                    if (closest != null && closest != currentNode)
                        SetCurrentNode(closest);
                    anyUnitMoving = false;
                }
            }
            else
            {
                path.Clear();
                Node closest = NodeManager.GetClosetNode(transform.position);
                if (closest != null && closest != currentNode)
                    SetCurrentNode(closest);
                anyUnitMoving = false;
            }
        }
    }
    public Vector3 GetSnappedPosition(Node node)
    {
        Vector3 pos = node.transform.position;
        if (pos.y < originalY)
            pos.y = originalY;
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("Step"), 1f, false);
        return pos;
    }
    public void SetCurrentNode(Node newNode)
    {
        if (currentNode != null && currentNode.unitOnNode == this.gameObject)
            currentNode.unitOnNode = null;
        currentNode = newNode;
        if (currentNode != null)
        {
            currentNode.unitOnNode = this.gameObject;
        }
    }
    public bool PathEmpty()
    {
        return path == null || path.Count == 0;
    }
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }
    private void Die()
    {
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("UnitDeath"), 0.5f, false);
        if (currentNode != null && currentNode.unitOnNode == this.gameObject)
            currentNode.unitOnNode = null;
        Destroy(gameObject);
    }
    public void ResetTurnFlags()
    {
        hasAttackedTowerThisTurn = false;
        hasHealthedTowerThisTurn = false;
        pendingTask = false;
        if (this is Ranger ranger)
            ranger.hasCollectedThisTurn = false;
    }
    public void ClearPath()
    {
        path.Clear();         
        targetNode = null;
    }
    public static bool IsAnyUnitMoving() => anyUnitMoving;
}