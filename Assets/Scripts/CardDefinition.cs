using UnityEngine;

[CreateAssetMenu(menuName = "Game/Card Definition")]
public class CardDefinition : ScriptableObject
{
    public string id;          // unique stable id, e.g. "apple"
    public Sprite faceSprite;
}
