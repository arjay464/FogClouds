using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI descriptionText;

    public Image cardArtImage;

    public CardData cardData;

    public void SetCardData(CardData data)
    {
        cardData = data;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (cardData == null) return;

        cardNameText.text = cardData.cardName;
        descriptionText.text = cardData.description;

        if(cardData.cardArt != null)
        {
            cardArtImage.sprite = cardData.cardArt;
        }
    }
}
