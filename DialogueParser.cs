using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueParser : MonoBehaviour {

    DialogueContainer _container;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private Transform choiceButtonContainer;
    [SerializeField] private Transform dialoguePanel;

    private string currentNodeGuid;
    private bool isChoosing;
    private bool isParsing;

    private string leftCharacter;
    private string rightCharacter;
    private string prevCharacter;
    private bool leftRight;
    [SerializeField] Image leftPortrait;
    [SerializeField] Image rightPortrait;

    private Color deemphasizeColor = new Color(.5f, .5f, .5f, 1);
    private Color highlightColor = new Color(1, 1, 1, 1);

    //Dataset to be moved later
    [SerializeField] List<CharacterPortrait> portraits = new List<CharacterPortrait>();

    public static DialogueParser Instance { get; private set; }

    private void Awake() {
        if (Instance != null)
            Destroy(Instance.gameObject);
        Instance = this;
    }

    public void StartDialogue(DialogueContainer container) {
        if (isParsing) return;

        ClearPortraits();
        _container = container;

        dialoguePanel.gameObject.SetActive(true);
        var entryNodeLink = _container.nodeLinks.Find(x => x.portName == "Next");
        currentNodeGuid = entryNodeLink.targetNodeGuid;
        ProceedNarrative(currentNodeGuid);
        isParsing = true;
    }

    private void Update() {

        //Listen to proceed input only when dialogue is playing and not choosing
        //Maybe should move to input managing script in the future?
        if (Input.GetKeyDown(KeyCode.E) && isParsing && !isChoosing) {
            ProceedNarrative(currentNodeGuid);
        }
    }

    private void ProceedNarrative(string nodeGuid) {

        currentNodeGuid = nodeGuid;

        //Clear buttons
        var buttons = choiceButtonContainer.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++) {
            Destroy(buttons[i].gameObject);
        }

        //End of dialogue
        if (string.IsNullOrEmpty(nodeGuid)) {
            isParsing = false;
            dialoguePanel.gameObject.SetActive(false);
            PlayableDirectorController.Instance.Resume();
            return;
        }

        var nodeData = _container.dialogueNodeData.Find(x => x.Guid == nodeGuid);

        //Process node data to UI
        nameText.SetText(nodeData.NameText);
        dialogueText.SetText(nodeData.DialogueText);
        SetPortrait(nodeData.NameText);

        if (nodeData.IsChoiceNode) {
            isChoosing = true;

            var choices = _container.nodeLinks.Where(x => x.baseNodeGuid == nodeGuid);
            foreach(var choice in choices) {
                var button = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                button.GetComponentInChildren<TextMeshProUGUI>().SetText(choice.portName);
                button.onClick.AddListener(() => ProceedNarrative(choice.targetNodeGuid));
            }
        }
        else {
            string nextNodeGuid = _container.nodeLinks.Find(x => x.baseNodeGuid == currentNodeGuid)?.targetNodeGuid;
            currentNodeGuid = nextNodeGuid;
            isChoosing = false;
        }
    }

    private void SetPortrait(string characterName) {

        if (characterName == prevCharacter)
            return;

        

        if(leftCharacter != null && rightCharacter != null) {

            leftPortrait.color = rightPortrait.color = deemphasizeColor;

            if (characterName == leftCharacter || characterName == rightCharacter) {
                //Swap side if narrative is between current two charcters
                leftRight = !leftRight;
            }
            else {
                //Swap sprite on the side opposite to previous speaking
                if (leftRight) {
                    rightPortrait.sprite = portraits.Find(x => x.characterName == characterName).portraitSprite;
                }
                else {
                    leftPortrait.sprite = portraits.Find(x => x.characterName == characterName).portraitSprite;
                }
                leftRight = !leftRight;
            }
        }
        else if (leftCharacter == null) {
            leftCharacter = characterName;
            leftPortrait.sprite = portraits.Find(x => x.characterName == characterName).portraitSprite;
            leftRight = true;
        }
        else {
            rightCharacter = characterName;
            rightPortrait.sprite = portraits.Find(x => x.characterName == characterName).portraitSprite;
            leftRight = false;

            leftPortrait.color = deemphasizeColor;
        }

        //Highlight
        if (leftRight) {
            leftPortrait.color = highlightColor;
        }
        else {
            rightPortrait.color = highlightColor;
        }

        prevCharacter = characterName;
    }

    private void ClearPortraits() {
        leftCharacter = null;
        rightCharacter = null;
        leftPortrait.color = rightPortrait.color = new Color(0, 0, 0, 0);
    }
}
