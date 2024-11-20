using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using System.Linq;

public class DialogueGraphView : GraphView {
    
    public readonly Vector2 defaultNodeSize = new Vector2(150, 200);
    private NodeSearchWindow _searchWindow;

    public DialogueGraphView(EditorWindow window) {

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        AddElement(GenerateEntryPointNode());
        AddSearchWindow(window);
    }

    private void AddSearchWindow(EditorWindow window) {
        _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        _searchWindow.Init(this, window);
        nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
    }

    //Sets rules for compatible port
    //In this case, only connection info is needed, data doesn't passthrough nodes so no type restriction?
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
        List<Port> compatiblePorts = new List<Port>();

        ports.ForEach(port => {
            //Prevents ports connecting to self/same node/same direction
            if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                compatiblePorts.Add(port);
        });

        return compatiblePorts;
    }

    private DialogueNode GenerateEntryPointNode() {
        DialogueNode node = new DialogueNode {
            title = "START",
            GUID = Guid.NewGuid().ToString(),
            DialogueText = "ENTRYPOINT",
            EntryPoint = true
        };

        Port generatedPort = GeneratePort(node, Direction.Output);
        generatedPort.portName = "Next";
        node.outputContainer.Add(generatedPort);

        node.capabilities &= ~Capabilities.Movable;
        node.capabilities &= ~Capabilities.Deletable;

        node.RefreshExpandedState();
        node.RefreshPorts();

        node.SetPosition(new Rect(100, 200, 100, 150));
        return node;
    }

    public void CreateNode(Vector2 position, bool choiceNode) {
        AddElement(CreateDialogueNode(position, choiceNode));
    }

    public DialogueNode CreateDialogueNode(Vector2 position, bool choiceNode, DialogueNodeData loadData = null) {
        DialogueNode dialogueNode = new DialogueNode {
            title = (choiceNode) ? "Choice Node" : "Dialogue Node",
            NameText = (loadData == null) ? "Name" : loadData.NameText,
            DialogueText = (loadData == null) ? "Dialogue text..." : loadData.DialogueText,
            GUID = Guid.NewGuid().ToString(),
            IsChoiceNode = choiceNode
        };

        //Generate input port
        Port inputPort = GeneratePort(dialogueNode, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        dialogueNode.inputContainer.Add(inputPort);

        if (choiceNode) {
            //Creates output port generate button
            Button button = new Button(() => { AddChoicePort(dialogueNode); });
            button.text = "New Choice";
            dialogueNode.titleContainer.Add(button);
        }
        else {
            //Creates single output port
            Port outputPort = GeneratePort(dialogueNode, Direction.Output, Port.Capacity.Single);
            outputPort.portName = "Output";
            dialogueNode.outputContainer.Add(outputPort);
        }

        var nameField = new TextField(string.Empty);
        nameField.RegisterValueChangedCallback(evt => {
            dialogueNode.NameText = evt.newValue;
        });
        nameField.SetValueWithoutNotify(dialogueNode.NameText);
        dialogueNode.mainContainer.Add(nameField);

        var textField = new TextField(string.Empty, 300, true, false, '*');
        textField.RegisterValueChangedCallback(evt => {
            dialogueNode.DialogueText = evt.newValue;
        });
        textField.SetValueWithoutNotify(dialogueNode.DialogueText);
        dialogueNode.mainContainer.Add(textField);

        dialogueNode.RefreshExpandedState();
        dialogueNode.RefreshPorts();
        dialogueNode.SetPosition(new Rect(position, defaultNodeSize));


        return dialogueNode;
    }

    public void AddChoicePort(DialogueNode dialogueNode, string overriddenPortName = "") {
        Port generatedPort = GeneratePort(dialogueNode, Direction.Output);

        var defaultLable = generatedPort.contentContainer.Q<Label>("type");
        generatedPort.contentContainer.Remove(defaultLable);

        int outputPortCount = dialogueNode.outputContainer.Query("connector").ToList().Count;
        var choicePortName = string.IsNullOrEmpty(overriddenPortName) ? $"Choice {outputPortCount + 1}" : overriddenPortName;
        generatedPort.portName = choicePortName;

        var textField = new TextField {
            name = string.Empty,
            value = choicePortName
        };
        textField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);
        generatedPort.contentContainer.Add(new Label(""));
        generatedPort.contentContainer.Add(textField);

        var deleteButton = new Button(() => RemovePort(dialogueNode, generatedPort)) {
            text = "Delete"
        };
        generatedPort.contentContainer.Add(deleteButton);

        dialogueNode.outputContainer.Add(generatedPort);
        dialogueNode.RefreshExpandedState();
        dialogueNode.RefreshPorts();
    }

    private Port GeneratePort(DialogueNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single) {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
    }

    private void RemovePort(DialogueNode dialogueNode, Port generatePort) {
        var targetEdge = edges.ToList().Where(x => x.output.portName == generatePort.portName && x.output.node == generatePort.node);

        if (targetEdge.Any()) {
            var edge = targetEdge.First();
            edge.input.Disconnect(edge);
            RemoveElement(targetEdge.First());
        }

        dialogueNode.outputContainer.Remove(generatePort);
        dialogueNode.RefreshExpandedState();
        dialogueNode.RefreshPorts();
    }
}
