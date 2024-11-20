using UnityEngine;

[CreateAssetMenu(fileName = "New PortraitData", menuName = "Dialogue/PortraitData")]
public class CharacterPortrait : ScriptableObject
{
    public string characterName;
    public Sprite portraitSprite;
}
