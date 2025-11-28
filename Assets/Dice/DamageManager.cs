using UnityEngine;
using UnityEngine.UI;
public class DamageManager : MonoBehaviour
{
    public static DamageManager instance;
    public DiceRoll _diceRoll;
    public Text scoreText;
    public Button rollDiceButton;
    private int totalDamage = 0;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    void Start()
    {
        if (_diceRoll == null)
            _diceRoll = FindObjectOfType<DiceRoll>();
        if (rollDiceButton != null)
            rollDiceButton.onClick.AddListener(RollDice);
        else
            Debug.LogError("DamageManager: falta asignar el botón.");

        UpdateScoreDisplay();
    }
    public void AddDamage(int damageValue)
    {
        totalDamage += damageValue;
        UpdateScoreDisplay();

        Debug.Log($"Daño acumulado: {totalDamage}");
    }
    private void UpdateScoreDisplay()
    {
        scoreText.text = "Damage = " + totalDamage.ToString();
    }
    private void RollDice()
    {
        if (_diceRoll != null)
            _diceRoll.RollDice();
        else
            Debug.LogError("DiceRoll no encontrado en la escena.");
    }
    public void ResetScore()
    {
        totalDamage = 0;
        UpdateScoreDisplay();
    }
}
