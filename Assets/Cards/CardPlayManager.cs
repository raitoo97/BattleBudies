using UnityEngine;
public class CardPlayManager : MonoBehaviour
{
    public static CardPlayManager instance;
    public bool placingMode = false;
    private CardInteraction currentUIcard;
    private CardData currentCardData;
    private Node selectedNode;
    [SerializeField]private Transform _playerHand;
    [SerializeField]private Transform _dragZone;
    private bool handsVisible = true;
    [Header("Dados")]
    public DiceRoll playerDice;
    public DiceRoll enemyDice;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    private void Update()
    {
        if (CombatManager.instance != null && CombatManager.instance.GetCombatActive)
        {
            HideAllHands();
            return;
        }
        if (!placingMode && GameManager.instance.isPlayerTurn)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleHandsVisibility();
            }
        }
    }
    private void ToggleHandsVisibility()
    {
        handsVisible = !handsVisible;
        SetHandActive(_playerHand, handsVisible);
        SetHandActive(_dragZone, handsVisible);
        if (DeckManager.instance != null && DeckManager.instance.enemyHand != null)
        {
            SetHandActive(DeckManager.instance.enemyHand, handsVisible);
        }
    }
    private void SetHandActive(Transform hand, bool active)
    {
        foreach (Transform card in hand)
        {
            card.gameObject.SetActive(active);
        }
    }
    public void ShowAllHandsAtPlayerTurn()
    {
        SetHandActive(_playerHand, true);
        SetHandActive(_dragZone, true);
        if (DeckManager.instance != null && DeckManager.instance.enemyHand != null)
        {
            SetHandActive(DeckManager.instance.enemyHand, true);
        }
        handsVisible = true;
    }
    public void HideAllHandsAtAITurn()
    {
        SetHandActive(_playerHand, false);
        SetHandActive(_dragZone, false);
        if (DeckManager.instance != null && DeckManager.instance.enemyHand != null)
        {
            SetHandActive(DeckManager.instance.enemyHand, false);
        }
        handsVisible = false;
    }
    public void TryPlayCard(CardInteraction uiCard, CardData cardData)
    {
        if (CombatManager.instance != null && CombatManager.instance.GetCombatActive)
        {
            Debug.Log("No se puede jugar cartas durante el combate.");
            return;
        }
        currentUIcard = uiCard;
        currentCardData = cardData;
        placingMode = true;
        GridVisualizer.instance.placingMode = true;
        HideAllHands();
        if (UnitController.instance != null && UnitController.instance.selectedUnit != null)
        {
            var glow = UnitController.instance.selectedUnit.GetComponent<GlowUnit>();
            if (glow != null) glow.SetGlowOff();
            UnitController.instance.selectedUnit = null;
        }
    }
    public void NodeClicked(Node node)
    {
        if (!placingMode) return;
        if (node.gridIndex.x != 0) return;
        if (node.unitOnNode != null) return;
        selectedNode = node;
        PlaceUnitAtNode();
    }
    public void PlaceUnitAtNode()
    {
        if (selectedNode == null || currentCardData == null)
        {
            Debug.LogWarning("No hay nodo o cardData disponible.");
            return;
        }
        bool isPlayerTurn = GameManager.instance.isPlayerTurn;
        if (isPlayerTurn)
        {
            EnergyManager.instance.currentEnergy -= currentCardData.cost;
        }
        else
        {
            EnergyManager.instance.enemyCurrentEnergy -= currentCardData.cost;
        }
        Vector3 spawnPos = selectedNode.transform.position + Vector3.up * 5f;
        GameObject unit = Instantiate(currentCardData.unitPrefab, spawnPos, Quaternion.identity);
        Units unitScript = unit.GetComponent<Units>();
        if (unitScript != null)
        {
            unitScript.SetCurrentNode(selectedNode);
            unitScript.isPlayerUnit = isPlayerTurn;
            unitScript.diceInstance = isPlayerTurn ? playerDice : enemyDice;
        }
        Destroy(currentUIcard.gameObject);
        placingMode = false;
        selectedNode = null;
        currentUIcard = null;
        currentCardData = null;
        GridVisualizer.instance.placingMode = false;
        if (isPlayerTurn)
        {
            handsVisible = true;
            ShowAllHandsAtPlayerTurn();
        }
    }
    public void HideAllHands()
    {
        SetHandActive(_playerHand, false);
        SetHandActive(_dragZone, false);
        if (DeckManager.instance != null && DeckManager.instance.enemyHand != null)
            SetHandActive(DeckManager.instance.enemyHand, false);
        handsVisible = false;
    }
    public CardInteraction GetcurrentUIcard { get => currentUIcard; set => currentUIcard = value; }
    public CardData GetcurrentCardData { get => currentCardData; set => currentCardData = value; }
    public Node GetselectedNode { get => selectedNode; set => selectedNode = value; }
}
