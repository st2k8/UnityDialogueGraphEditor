using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public DialogueContainer dialogueContainer;
    [SerializeField] bool triggerOnEnabled;

    private void OnEnable() {
        if (triggerOnEnabled) {
            DialogueParser.Instance.StartDialogue(dialogueContainer);
            PlayableDirectorController.Instance.Pause();
        }
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.H)) {
            DialogueParser.Instance.StartDialogue(dialogueContainer);
        }
    }
}
