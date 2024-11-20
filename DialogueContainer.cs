using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueContainer : ScriptableObject
{
    public string fileName;
    public List<string> characters = new List<string>();
    public List<DialogueNodeData> dialogueNodeData = new List<DialogueNodeData>();
    public List<NodeLinkData> nodeLinks = new List<NodeLinkData>();
}
