using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform boardParent;

    void Start()
    {
        SpawnOneCard();
    }

    void SpawnOneCard()
    {
        CardView card = Instantiate(cardPrefab, boardParent);

        string instanceId = "0";
        string faceId = "apple";
        Sprite faceSprite = Resources.Load<Sprite>("apple"); 

        card.Init(instanceId, faceId, faceSprite);

        card.Clicked += OnCardClicked;
    }

    void OnCardClicked(CardView card)
    {
        if (!card.CanInteract) return;

        card.Reveal();
    }
}
