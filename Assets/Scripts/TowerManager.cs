using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class TowerManager : MonoBehaviour
{
    public static TowerManager instance;
    [Header("Towers (arrastrar por inspector)")]
    public List<Tower> playerTowers = new();
    public List<Tower> enemyTowers = new();
    private Dictionary<Tower, HashSet<string>> attackNodes = new();
    private bool nodesRegistered = false;
    private int towerAttackNodeLayer = 14;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    private void Start()
    {
        StartCoroutine(DelayedInit());
    }
    private IEnumerator DelayedInit()
    {
        yield return null;
        InitializeAttackNodes();
    }
    public void InitializeAttackNodes()
    {
        if (nodesRegistered) return;
        attackNodes.Clear();
        foreach (Tower t in playerTowers)
            attackNodes[t] = new HashSet<string>();
        foreach (Tower t in enemyTowers)
            attackNodes[t] = new HashSet<string>();
        for (int i = 0; i < playerTowers.Count; i++)
        {
            switch (i)
            {
                case 0: AddNodes(playerTowers[i], "_0_0"); break;
                case 1: AddNodes(playerTowers[i], "_0_6", "_0_7", "_0_8"); break;
                case 2: AddNodes(playerTowers[i], "_0_14"); break;
            }
        }
        for (int i = 0; i < enemyTowers.Count; i++)
        {
            switch (i)
            {
                case 0: AddNodes(enemyTowers[i], "_14_14"); break;
                case 1: AddNodes(enemyTowers[i], "_14_6", "_14_7", "_14_8"); break;
                case 2: AddNodes(enemyTowers[i], "_14_0"); break;
            }
        }
        nodesRegistered = true;
    }
    private void AddNodes(Tower t, params string[] nodes)
    {
        if (!attackNodes.ContainsKey(t)) attackNodes[t] = new HashSet<string>();
        bool applyLayer = t.faction == Faction.Enemy;
        foreach (var n in nodes)
        {
            attackNodes[t].Add(n);
            // Parse "_x_y"
            string[] parts = n.Split('_');
            if (parts.Length != 3) continue;
            if (!int.TryParse(parts[1], out int x)) continue;
            if (!int.TryParse(parts[2], out int y)) continue;
            Node node = NodeManager.GetAllNodes().Find(nd => nd.gridIndex.x == x && nd.gridIndex.y == y);
            if (node != null && applyLayer)
            {
                node.gameObject.layer = towerAttackNodeLayer;
            }
        }
    }
    public bool CanUnitAttackTower(Units unit, Tower tower)
    {
        if (unit == null || tower == null) return false;
        if (!attackNodes.ContainsKey(tower)) return false;
        if (unit.currentNode == null) return false;
        string key = $"_{unit.currentNode.gridIndex.x}_{unit.currentNode.gridIndex.y}";
        return attackNodes[tower].Contains(key);
    }
    public bool TryGetTowerAtNode(Node node, out Tower found)
    {
        found = null;
        if (node == null) return false;
        string key = $"_{node.gridIndex.x}_{node.gridIndex.y}";
        foreach (var kv in attackNodes)
        {
            if (kv.Value.Contains(key))
            {
                found = kv.Key;
                return true;
            }
        }
        return false;
    }
    public void NotifyTowerDestroyed(Tower tower)
    {
        Debug.Log($"{tower.faction} tower destroyed!");
        playerTowers.Remove(tower);
        enemyTowers.Remove(tower);
        if (attackNodes.ContainsKey(tower))
            attackNodes.Remove(tower);
        CheckWinCondition();
    }
    private void CheckWinCondition()
    {
        bool allPlayerTowersDown = playerTowers.Count == 0;
        bool allEnemyTowersDown = enemyTowers.Count == 0;
        if (allEnemyTowersDown)
            GameManager.instance.SetEndGame(true);
        else if (allPlayerTowersDown)
            GameManager.instance.SetEndGame(false);
    }
    public IEnumerable<string> GetAttackNodes(Tower tower)
    {
        if (attackNodes.ContainsKey(tower))
            return attackNodes[tower];
        return new List<string>();
    }
    public Tower GetEnemyTower()
    {
        var allTowers = FindObjectsOfType<Tower>();
        List<Tower> EnemyTowers = new List<Tower>();
        foreach (Tower t in allTowers)
        {
            if (!EnemyTowers.Contains(t) && t.faction == Faction.Enemy)
            {
                EnemyTowers.Add(t);
            }
        }
        if (EnemyTowers.Count == 0) return null;
        EnemyTowers.Sort((a, b) => a.currentHealth.CompareTo(b.currentHealth));
        return EnemyTowers[0];
    }
}
