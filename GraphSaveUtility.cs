using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using System.Linq;

public class GraphSaveUtility
{
    private DialogueGraphView _targetGraphView;
    private DialogueContainer _containerCache;

    private List<Edge> Edges => _targetGraphView.edges.ToList();
    //Cast as DialogueNode class to access node Guid
    private List<DialogueNode> Nodes => _targetGraphView.nodes.ToList().Cast<DialogueNode>().ToList();

    public static GraphSaveUtility GetInstance(DialogueGraphView targetGraphView) {
        return new GraphSaveUtility {
            _targetGraphView = targetGraphView
        };
    }

    public void SaveGraph(DialogueGraph window, bool saveAs) {
        
        //Do nothing if no any edges
        if (!Edges.Any()) return;

        var dialogueContainer = ScriptableObject.CreateInstance<DialogueContainer>();

        //add link data
        var connectedPorts = Edges.Where(x => x.input.node != null).ToArray();
        for(int i = 0; i < connectedPorts.Length; i++) {
            //Cast into DialogueNode for accessing guid
            var outputNode = connectedPorts[i].output.node as DialogueNode;
            var inputNode = connectedPorts[i].input.node as DialogueNode;

            dialogueContainer.nodeLinks.Add(new NodeLinkData {
                baseNodeGuid = outputNode.GUID,
                portName = connectedPorts[i].output.portName,
                targetNodeGuid = inputNode.GUID
            });
        }

        //Add node data
        //Except entrypoint which will always be present
        dialogueContainer.characters.Clear();

        foreach(var dialogueNode in Nodes.Where(node => !node.EntryPoint)) {
            dialogueContainer.dialogueNodeData.Add(new DialogueNodeData {
                Guid = dialogueNode.GUID,
                IsChoiceNode = dialogueNode.IsChoiceNode,
                NameText = dialogueNode.NameText,
                DialogueText = dialogueNode.DialogueText,
                Position = dialogueNode.GetPosition().position
            });

            if (!dialogueContainer.characters.Contains(dialogueNode.NameText)) {
                dialogueContainer.characters.Add(dialogueNode.NameText);
            }
        }

        var fileName = window.currentFileName;
        var path = window.currentPath;

        if(saveAs || path == null) {
            path = EditorUtility.SaveFilePanel("Save Dialogue Graph", Application.dataPath, "New Dialogue Graph", "asset");
            if (string.IsNullOrEmpty(path))
                return;

            //Trim file name
            fileName = TrimFileName(path);

            //Refresh editor window title
            window.titleContent = new GUIContent(fileName);

            window.currentFileName = fileName;
            window.currentPath = path;
        }

        dialogueContainer.fileName = fileName;

        AssetDatabase.CreateAsset(dialogueContainer, TrimRelativePath(path));
        AssetDatabase.SaveAssets();
    }

    public void LoadGraph(DialogueGraph window) {

        var path = EditorUtility.OpenFilePanel("Open Dialogue Graph", Application.dataPath, "asset");
        _containerCache = (DialogueContainer)AssetDatabase.LoadAssetAtPath(TrimRelativePath(path), typeof(DialogueContainer));

        if (_containerCache == null) {
            EditorUtility.DisplayDialog("File Not Found", "Target dialogue graph file does not exists.", "OK");
            return;
        }

        //Update container fileName if data is renamed elsewhere
        var loadedFileName = TrimFileName(path);
        if (_containerCache.fileName != loadedFileName)
            _containerCache.fileName = loadedFileName;

        window.currentFileName = _containerCache.fileName;
        window.currentPath = path;

        //Refresh editor window title
        window.titleContent = new GUIContent(_containerCache.fileName);

        ClearGraph();
        CreateNodes();
        ConnectNodes();
    }

    private void ClearGraph() {

        //Set entry points guid back from the save. Discard existing guid.
        Nodes.Find(x => x.EntryPoint).GUID = _containerCache.nodeLinks.Find(x => x.portName == "Next").baseNodeGuid;

        foreach(var node in Nodes) {
            if (node.EntryPoint) continue;

            //Remove edges connected to this node's output(if edge.input.node is this node)
            Edges.Where(x => x.input.node == node).ToList().ForEach(edge => _targetGraphView.RemoveElement(edge));

            //Then remove the node
            _targetGraphView.RemoveElement(node);
        }
    }

    private void CreateNodes() {
        foreach(var nodeData in _containerCache.dialogueNodeData) {
            var tempNode = _targetGraphView.CreateDialogueNode(Vector2.zero, nodeData.IsChoiceNode, nodeData);
            tempNode.NameText = nodeData.NameText;
            tempNode.DialogueText = nodeData.DialogueText;
            tempNode.GUID = nodeData.Guid;
            _targetGraphView.AddElement(tempNode);

            if (nodeData.IsChoiceNode) {
                var nodePorts = _containerCache.nodeLinks.Where(x => x.baseNodeGuid == tempNode.GUID).ToList();
                nodePorts.ForEach(x => _targetGraphView.AddChoicePort(tempNode, x.portName));
            }
        }
    }

    private void ConnectNodes() {
        for(int i = 0; i < Nodes.Count; i++) {
            var connections = _containerCache.nodeLinks.Where(x => x.baseNodeGuid == Nodes[i].GUID).ToList();
            for(int j = 0; j < connections.Count; j++) {
                var targetNodeGuid = connections[j].targetNodeGuid;
                var targetNode = Nodes.First(x => x.GUID == targetNodeGuid);
                LinkNodes(Nodes[i].outputContainer[j].Q<Port>(), (Port)targetNode.inputContainer[0]);

                targetNode.SetPosition(new Rect(_containerCache.dialogueNodeData.First(x => x.Guid == targetNode.GUID).Position, _targetGraphView.defaultNodeSize));
            }
        }
    }

    private void LinkNodes(Port output, Port input) {
        var tempEdge = new Edge {
            input = input,
            output = output,
        };

        tempEdge?.input.Connect(tempEdge);
        tempEdge?.output.Connect(tempEdge);
        _targetGraphView.Add(tempEdge);
    }

    private string TrimRelativePath(string absolutePath) {
        if (!absolutePath.StartsWith(Application.dataPath))
            return null;
        return "Assets" + absolutePath.Substring(Application.dataPath.Length);
    }

    private string TrimFileName(string absolutePath) {
        var fileName = absolutePath.Substring(absolutePath.LastIndexOf("/") + 1);
        fileName = fileName.Substring(0, fileName.LastIndexOf("."));
        return fileName;
    }
}
