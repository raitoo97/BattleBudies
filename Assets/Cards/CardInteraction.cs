using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class CardInteraction : MonoBehaviour , IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Scale Settings")]
    public float normalScale = 1f;
    public float hoverScale = 1.2f;
    public float scaleSpeed = 10f;
    private Vector3 targetScale;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private Canvas canvas;
    private Vector3 pointerOffset;
    private bool isDragging = false;
    private RectTransform dragLayer;
    private RectTransform playerHand;
    private UICard uiCard;
    [HideInInspector] public bool isPlayerCard = false;
    public static bool isOnDraging = false;
    private int originalSiblingIndex;
    private bool isHovering = false;
    public static CardInteraction hoveredCard;
    private LayoutElement layoutElement;
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
        uiCard = GetComponent<UICard>();
        canvas = GetComponentInParent<Canvas>();
        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();
        if (canvas == null)
        {
            Debug.LogError("No se encontró un Canvas en los padres!");
        }
        else
        {
            Transform dz = canvas.transform.Find("DragZone");
            if (dz != null)
                dragLayer = dz.GetComponent<RectTransform>();
            else
                Debug.LogWarning("No se encontró DragZone dentro del Canvas");
            Transform ph = canvas.transform.Find("PlayerHand");
            if (ph != null)
                playerHand = ph.GetComponent<RectTransform>();
            else
                Debug.LogWarning("No se encontró PlayerHand dentro del Canvas");
        }
    }
    private void Start()
    {
        targetScale = Vector3.one * normalScale;
        rectTransform.localScale = targetScale;
        originalSiblingIndex = transform.GetSiblingIndex();
    }
    private void Update()
    {
        if (!isDragging)
        {
            bool mouseOver = IsMouseOverThisCard();
            if (mouseOver && hoveredCard != null && hoveredCard != this)
                return;
            if (mouseOver && !isHovering)
            {
                isHovering = true;
                hoveredCard = this;
                targetScale = Vector3.one * hoverScale;
                transform.SetAsLastSibling();
                if (layoutElement != null)
                    layoutElement.ignoreLayout = true;
            }
            else if (!mouseOver && isHovering)
            {
                isHovering = false;
                if (hoveredCard == this)
                    hoveredCard = null;
                targetScale = Vector3.one * normalScale;
                transform.SetSiblingIndex(originalSiblingIndex);
                if (layoutElement != null)
                    layoutElement.ignoreLayout = false;
            }
        }
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale,targetScale,Time.deltaTime * scaleSpeed);
        if (isPlayerCard)
            UpdateTint();
    }
    private bool IsMouseOverThisCard()
    {
        if (!isPlayerCard) return false;
        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, hits);
        if (hits.Count == 0)
            return false;
        return hits[0].gameObject == gameObject;
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isPlayerCard || dragLayer == null) return;
        isDragging = true;
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;
        transform.SetParent(dragLayer, true);
        Vector3 globalMousePos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null, out globalMousePos);
        pointerOffset = rectTransform.position - globalMousePos;
        rectTransform.SetAsLastSibling();
        isOnDraging = true;
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (!isPlayerCard || !isDragging) return;
        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(dragLayer, eventData.position, canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null, out globalMousePos))
        {
            rectTransform.position = globalMousePos + pointerOffset;
        }
        isOnDraging = true;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isPlayerCard || !isDragging) return;
        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        targetScale = Vector3.one * normalScale;
        bool insideHand = false;
        if (playerHand != null)
        {
            insideHand = RectTransformUtility.RectangleContainsScreenPoint(playerHand, Input.mousePosition, canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null);
        }
        bool isPlayerTurn = GameManager.instance.isPlayerTurn;
        float currentEnergy = isPlayerTurn ? EnergyManager.instance.currentEnergy : EnergyManager.instance.enemyCurrentEnergy;
        if (insideHand || currentEnergy < uiCard.cardData.cost)
        {
            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = originalPosition;
        }
        else
        {
            CardPlayManager.instance.TryPlayCard(this, uiCard.cardData);
            SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("CardPlay"), 1f, false);
        }
        isOnDraging = false;
    }
    public void UpdateTint()
    {
        if (!isPlayerCard || uiCard == null || uiCard.artworkImage == null) return;
        float currentEnergy = EnergyManager.instance.currentEnergy;
        if (currentEnergy < uiCard.cardData.cost)
            SetGrayTint();
        else
            ResetTint();
    }
    private void SetGrayTint()
    {
        if (uiCard.artworkImage != null)
        {
            Color c = uiCard.artworkImage.color;
            c.r = 0.5f;
            c.g = 0.5f;
            c.b = 0.5f;
            uiCard.artworkImage.color = c;
        }
    }
    private void ResetTint()
    {
        if (uiCard.artworkImage != null)
        {
            uiCard.artworkImage.color = Color.white;
        }
    }
}
