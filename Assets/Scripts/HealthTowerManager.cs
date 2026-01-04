using System.Collections;
using UnityEngine;
public class HealthTowerManager : MonoBehaviour
{
    public static HealthTowerManager instance;
    [HideInInspector]public bool onColectedHealth;
    private DiceRoll diceRoll;
    private int pendingHealth;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this.gameObject);
    }
    public void AddHealthTower(bool isPlayer, int amount)
    {
        if (isPlayer)
        {
            Debug.Log("Se han añadido " + amount + " de salud a la torre del jugador");
        }
        else
        {
            Debug.Log("Se han añadido " + amount + " de salud a la torre del enemigo");
            var GetLowestHealthTower = TowerManager.instance.GetEnemyTower();
            if(GetLowestHealthTower == null)return;
            GetLowestHealthTower.Healt(amount);
        }
    }
    public void StartRecolectedHealth(Defenders defender)
    {
        if (onColectedHealth) return;
        if (defender == null) return;
        bool hasDamagedTower = false;
        if (defender.isPlayerUnit && GameManager.instance.isPlayerTurn)
        {
            foreach (var t in TowerManager.instance.playerTowers)
            {
                if (t.currentHealth < t.maxHealth)
                {
                    Debug.Log("Hay torres heridas, se inicia curación del player");
                    hasDamagedTower = true;
                    break;
                }
            }
        }
        else if(!defender.isPlayerUnit && !GameManager.instance.isPlayerTurn)
        {
            foreach (var t in TowerManager.instance.enemyTowers)
            {
                if (t.currentHealth < t.maxHealth)
                {
                    hasDamagedTower = true;
                    Debug.Log("Hay torres heridas, se inicia curación del enemigo");
                    break;
                }
            }
        }
        if (!hasDamagedTower)
        {
            Debug.Log("No hay torres heridas, no se inicia curación");
            return; // Salimos si no hay torres heridas
        }
        onColectedHealth = true;
        diceRoll = defender.diceInstance;
        pendingHealth = 0;
        if (CameraFocusManager.instance != null)
        {
            CameraFocusManager.instance.FocusOnUnit(defender);
        }
        StartCoroutine(DefenderRollDiceHealth(defender));
    }
    IEnumerator DefenderRollDiceHealth(Defenders defender)
    {
        CanvasManager.instance.ChangeCameraText(true, "Health Tower", Color.green);
        yield return new WaitForSeconds(2.5f);
        CanvasManager.instance.ChangeCameraText(false);
        if (defender == null)
        {
            onColectedHealth = false;
            yield break;
        }
        int numberOfDiceToRoll = defender.healthTowerDice;
        for(int i = 0; i < numberOfDiceToRoll; i++)
        {
            diceRoll.PrepareForRoll();
            int dicesLeft = numberOfDiceToRoll - i;
            if (defender.isPlayerUnit)
            {
                bool anyDamagedTower = TowerManager.instance.playerTowers.Exists(t => t.currentHealth < t.maxHealth);
                if (!anyDamagedTower)
                {
                    Debug.Log("Todas las torres ya están curadas, saliendo de la corutina");
                    break; // Sale del for, no muestra botón
                }
                CanvasManager.instance.rollClicked = false;
                CanvasManager.instance.HealingTowerUI(true, defender, playerCanRoll: true, result: -1, dicesLeft: dicesLeft);
                yield return new WaitUntil(() => CanvasManager.instance.rollClicked);
                CanvasManager.instance.HealingTowerUI(true, defender, playerCanRoll: false, result: -1, dicesLeft: dicesLeft);
            }
            else
            {
                CanvasManager.instance.HealingTowerUI(true, defender, playerCanRoll: false, result: -1, dicesLeft: dicesLeft);
                yield return new WaitForSeconds(0.5f);
            }
            diceRoll.RollDice();
            yield return new WaitUntil(() => diceRoll.hasBeenThrown && diceRoll.hasBeenCounted && diceRoll.IsDiceStill());
            CanvasManager.instance.HealingTowerUI(true, defender, playerCanRoll: false, result: pendingHealth, dicesLeft: dicesLeft);
            if (pendingHealth > 0)
            {
                if (defender.isPlayerUnit)
                {
                    // Re-verificacion por si alguna torre se curó antes
                    bool anyDamagedTower = TowerManager.instance.playerTowers.Exists(t => t.currentHealth < t.maxHealth);
                    if (!anyDamagedTower)
                    {
                        Debug.Log("Todas las torres ya están curadas, se salta la selección");
                        pendingHealth = 0;
                        break; // salimos del for
                    }

                    yield return StartCoroutine(WaitForPlayerSelectTower((Tower selectedTower) =>
                    {
                        if (selectedTower != null)
                        {
                            selectedTower.Healt(pendingHealth);
                        }
                    }));
                    pendingHealth = 0;
                }
                else
                {
                    AddHealthTower(isPlayer: false, pendingHealth);
                }
            }
            pendingHealth = 0;
            diceRoll.PrepareForRoll();
            yield return new WaitForSeconds(1f);
        }
        onColectedHealth = false;
        CanvasManager.instance.HealingTowerUI(false, defender);
    }
    public void ChangePendingHealth(int amount)
    {
        pendingHealth += amount;
    }
    public IEnumerator WaitForPlayerSelectTower(System.Action<Tower> callback)
    {
        Tower selected = null;
        while (selected == null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Tower t = hit.collider.GetComponent<Tower>();
                    if (t != null && t.faction == Faction.Player && t.currentHealth < t.maxHealth)
                    {
                        selected = t;
                    }
                }
            }
            yield return null;
        }
        callback?.Invoke(selected);
    }
}
