using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class TowerManager : MonoBehaviour
{
    public static TowerManager instance;
    public List<Tower> playerTowers = new();
    public List<Tower> enemyTowers = new();
    private Dictionary<Tower, HashSet<string>> attackNodes = new();
    private bool nodesRegistered = false;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    private void Start()
    {
        StartCoroutine(DelayedInit());
    }
    public void InitializeAttackNodes()
    {
        if (nodesRegistered) return;
        foreach (Tower t in playerTowers)
            attackNodes[t] = new HashSet<string>();
        foreach (Tower t in enemyTowers)
            attackNodes[t] = new HashSet<string>();
        AddNodes(playerTowers[0], "0_0");
        AddNodes(playerTowers[1], "0_7", "0_6", "0_8");
        AddNodes(playerTowers[2], "0_14");
        AddNodes(enemyTowers[0], "14_14");
        AddNodes(enemyTowers[1], "14_6", "14_7", "14_8");
        AddNodes(enemyTowers[2], "14_0");
        nodesRegistered = true;
    }
    public void RegisterTower(Tower tower)
    {
        if (tower.faction == Faction.Player)
            playerTowers.Add(tower);
        else
            enemyTowers.Add(tower);
    }
    private void AddNodes(Tower t, params string[] nodes)
    {
        foreach (var n in nodes)
            attackNodes[t].Add(n);
    }
    public bool CanUnitAttackTower(Units unit, Tower tower)
    {
        if (!attackNodes.ContainsKey(tower)) return false;

        string currentNode = "_" + unit.currentNode.gridIndex.x + "_" + unit.currentNode.gridIndex.y;

        return attackNodes[tower].Contains(currentNode);
    }
    public void NotifyTowerDestroyed(Tower tower)
    {
        Debug.Log($"{tower.faction} tower destroyed!");
        playerTowers.Remove(tower);
        enemyTowers.Remove(tower);
        CheckWinCondition();
    }
    private void CheckWinCondition()
    {
        bool allPlayerTowersDown = playerTowers.Count == 0;
        bool allEnemyTowersDown = enemyTowers.Count == 0;

        if (allEnemyTowersDown)
        {
            Debug.Log("PLAYER WINS!");
            GameManager.instance.SetEndGame(true);
        }
        else if (allPlayerTowersDown)
        {
            Debug.Log("ENEMY WINS!");
            GameManager.instance.SetEndGame(false);
        }
    }
    private IEnumerator DelayedInit()
    {
        yield return null;
        InitializeAttackNodes();
    }
}
