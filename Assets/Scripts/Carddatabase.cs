using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Card Database")]
public class Carddatabase : ScriptableObject
{
    public List<CardDefinition> cards;
}
