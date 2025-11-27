using UnityEngine;
public class CardPlayManager : MonoBehaviour
{
    public static CardPlayManager instance;
    private bool placingMode = false;
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
        HidePlayerHand();
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
        Vector3 spawnPos = selectedNode.transform.position + Vector3.up * 5f;
        Instantiate(currentCardData.unitPrefab, spawnPos, Quaternion.identity);
        Destroy(currentUIcard.gameObject);
        placingMode = false;
        selectedNode = null;
        currentUIcard = null;
        currentCardData = null;
        ShowPlayerHand();
    }
}
