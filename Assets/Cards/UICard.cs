using UnityEngine;
using UnityEngine.UI;
public class UICard : MonoBehaviour
{
    public Image artworkImage;
    public CardData cardData;
    void Awake()
    {
        artworkImage = GetComponent<Image>();
        if (artworkImage == null)
            Debug.LogWarning("No hay Image en el GameObject del prefab!");
        if (cardData != null)
            Setup(cardData);
    }
    public void Setup(CardData data)
    {
        cardData = data;
        if (artworkImage != null && cardData != null)
            artworkImage.sprite = cardData.artwork;
    }
}
