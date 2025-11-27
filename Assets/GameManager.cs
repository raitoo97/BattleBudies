using System.Collections;
using UnityEngine;
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool isPlayerTurn = true;
    public float turnDelay = 10f;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        StartCoroutine(TurnLoop());
    }
    private IEnumerator TurnLoop()
    {
        while (true)
        {
            if (isPlayerTurn)
            {
                Debug.Log("TURN — Player");
                EnergyManager.instance.RefillPlayerEnergy();
                yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.K));
                isPlayerTurn = false;
            }
            else
            {
                Debug.Log("TURN — Enemy");
                EnergyManager.instance.RefillEnemyEnergy();
                yield return new WaitForSeconds(turnDelay);
                isPlayerTurn = true;
            }

            yield return null;
        }
    }
}
