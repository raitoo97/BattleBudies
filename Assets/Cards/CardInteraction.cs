using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class CardInteraction : MonoBehaviour , IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private float hoverScale = 0.55f;
    private float hoverDuration = 0.15f;
    private float hoverDelay = 0.25f;
    private float undoHoverDelay = 0.1f;
    private float undoHoverTimer = 0f;
    private float hoverTimer = 0f;
    private bool hoverConfirmed = false;
    private Vector3 baseVisualScale;
    private Vector3 targetScale;
    private Vector3 scaleVelocity;
    private bool scaleInitialized = false;
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
    public static CardInteraction hoveredCard;
    private LayoutElement layoutElement;
    private RectTransform visualRoot;
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
        Graphic graphic = GetComponentInChildren<Graphic>();
        if (graphic != null)
        {
            visualRoot = graphic.rectTransform;
        }
        else
        {
            Debug.LogError("UIPrefabCard no tiene ningún Graphic hijo");
        }
    }
    private void Start()
    {
        originalSiblingIndex = transform.GetSiblingIndex();
        if (visualRoot != null)
        {
            baseVisualScale = visualRoot.localScale;
            targetScale = baseVisualScale;
            scaleInitialized = true;
        }
    }
    private void Update()
    {
        if (!isDragging)
        {
            bool mouseOver = IsMouseOverThisCard();
            if (mouseOver && hoveredCard != null && hoveredCard != this)
                return;
            if (mouseOver)
            {
                hoverTimer += Time.deltaTime;
                undoHoverTimer = 0f;
                if (!hoverConfirmed && hoverTimer >= hoverDelay)
                {
                    hoverConfirmed = true;
                    hoveredCard = this;
                    scaleVelocity = Vector3.zero;
                    targetScale = baseVisualScale * (1f + hoverScale);
                    transform.SetAsLastSibling();
                    if (layoutElement != null)
                        layoutElement.ignoreLayout = true;
                    SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("CardHover"), 1f, false);
                }
            }
            else
            {
                undoHoverTimer += Time.deltaTime;
                hoverTimer = 0f;
                if (hoverConfirmed && undoHoverTimer >= undoHoverDelay)
                {
                    hoverConfirmed = false;
                    if (hoveredCard == this)
                        hoveredCard = null;
                    scaleVelocity = Vector3.zero;
                    targetScale = baseVisualScale;
                    transform.SetSiblingIndex(originalSiblingIndex);
                    if (layoutElement != null)
                        layoutElement.ignoreLayout = false;
                }
            }
        }
        if (scaleInitialized)
        {
            visualRoot.localScale = Vector3.SmoothDamp(visualRoot.localScale,targetScale,ref scaleVelocity,hoverDuration);
        }
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
        targetScale = baseVisualScale;
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
