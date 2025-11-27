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
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleHandsVisibility();
        }
    }
    private void ToggleHandsVisibility()
    {
        foreach (Transform card in _playerHand)
        {
            card.gameObject.SetActive(!card.gameObject.activeSelf);
        }
        foreach (Transform card in _dragZone)
        {
            card.gameObject.SetActive(!card.gameObject.activeSelf);
        }
        if (DeckManager.instance != null && DeckManager.instance.enemyHand != null)
        {
            foreach (Transform card in DeckManager.instance.enemyHand)
            {
                card.gameObject.SetActive(!card.gameObject.activeSelf);
            }
        }
    }
    private void HidePlayerHand()
    {
        foreach (Transform card in _playerHand)
        {
            card.gameObject.SetActive(false);
        }
        foreach (Transform card in _dragZone)
        {
            card.gameObject.SetActive(false);
        }
    }
    private void ShowPlayerHand()
    {
        foreach (Transform card in _playerHand)
        {
            card.gameObject.SetActive(true);
        }
        foreach (Transform card in _dragZone)
        {
            card.gameObject.SetActive(false);
        }
    }
    public void TryPlayCard(CardInteraction uiCard, CardData cardData)
    {
        currentUIcard = uiCard;
        currentCardData = cardData;
        placingMode = true;
        GridVisualizer.instance.placingMode = true;
        HidePlayerHand();
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
        if (node.gridIndex.x != 0)
        {
            Debug.LogWarning("Solo se pueden colocar unidades en la fila 0.");
            return;
        }
        selectedNode = node;
        PlaceUnitAtNode();
    }
    private void PlaceUnitAtNode()
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
        Instantiate(currentCardData.unitPrefab, spawnPos, Quaternion.identity);
        Destroy(currentUIcard.gameObject);
        placingMode = false;
        selectedNode = null;
        currentUIcard = null;
        currentCardData = null;
        GridVisualizer.instance.placingMode = false;
        ShowPlayerHand();
    }
}
