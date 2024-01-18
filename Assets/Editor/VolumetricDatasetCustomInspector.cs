using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityCTVisualizer;

[CustomEditor(typeof(VolumetricDataset))]
public class VolumetricDatasetCustomInspector : Editor
{
    public VisualTreeAsset m_VolumetricDatasetInspectorXML;

    VolumetricDataset volumetricDataset;
    SerializedProperty datasetPath;

    private void OnEnable()
    {
        volumetricDataset = (VolumetricDataset)target;
        datasetPath = serializedObject.FindProperty("_datasetPath");
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
    }

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = m_VolumetricDatasetInspectorXML.Instantiate();
        return root;
    }
}
