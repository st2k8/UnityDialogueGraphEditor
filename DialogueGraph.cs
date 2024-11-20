using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

public class DialogueGraph : EditorWindow {

    private DialogueGraphView _graphView;
    public string currentFileName;
    public string currentPath;

    [MenuItem("Graph/DialogueGraph")]
    public static void OpenDialogueGraphWindow(){
        EditorWindow window = GetWindow<DialogueGraph>();
        window.titleContent = new GUIContent("Dialogue Graph");
    }

    private void ConstructGraphView() {
        _graphView = new DialogueGraphView(this) {
            name = "Dialogue Graph"
        };

        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolbar() {
        Toolbar toolbar = new Toolbar();

        toolbar.Add(new Button(() => RequestDataOperation(true, false)) { text = "Save" });
        toolbar.Add(new Button(() => RequestDataOperation(true, true)) { text = "Save As" });
        toolbar.Add(new Button(() => RequestDataOperation(false)) { text = "Load" });

        rootVisualElement.Add(toolbar);
    }

    private void RequestDataOperation(bool save, bool saveAs = false) {

        var saveUtility = GraphSaveUtility.GetInstance(_graphView);
        if (save) {
            saveUtility.SaveGraph(this, saveAs);
        }
        else {
            saveUtility.LoadGraph(this);
        }
    }

    private void OnEnable() {
        ConstructGraphView();
        GenerateToolbar();
    }

    private void OnDisable() {
        rootVisualElement.Remove(_graphView);
    }
}
