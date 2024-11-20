using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

public class DialogueNode : Node
{
    public string GUID;

    public bool IsChoiceNode;

    public string NameText;

    public string DialogueText;

    public bool EntryPoint = false;
}
