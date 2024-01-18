using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityCTVisualizer;
using System;

[CustomEditor(typeof(VolumetricDataset))]
public class VolumetricDatasetCustomInspector : Editor
{
    public VisualTreeAsset m_VolumetricDatasetInspectorXML;
    

    VolumetricDataset m_VolumetricDataset;

    // serialized properties
    SerializedProperty m_DatasetPath;
    SerializedProperty m_ImageWidth;
    SerializedProperty m_ImageHeight;
    SerializedProperty m_NbrSlices;

    private void OnEnable()
    {
        m_VolumetricDataset = (VolumetricDataset)target;
        m_DatasetPath = serializedObject.FindProperty("m_DatasetPath");
        m_ImageWidth = serializedObject.FindProperty("imageWidth");
        m_ImageHeight = serializedObject.FindProperty("imageHeight");
        m_NbrSlices = serializedObject.FindProperty("nbrSlices");

        SceneView.duringSceneGui += OnSceneViewGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneViewGUI;
    }

    private void OnSceneViewGUI(SceneView sv)
    {
        if (m_VolumetricDataset.volumetricTex != null) {
            Handles.DrawTexture3DVolume(m_VolumetricDataset.volumetricTex, opacity: 0.05f);
        }
    }

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = m_VolumetricDatasetInspectorXML.Instantiate();
        IntegerField volumeWidth = root.Q<IntegerField>(name: "volume-width");
        IntegerField volumeHeight = root.Q<IntegerField>(name: "volume-height");
        IntegerField volumeSlices = root.Q<IntegerField>(name: "volume-slices");
        volumeWidth.BindProperty(m_ImageWidth);
        volumeHeight.BindProperty(m_ImageHeight);
        volumeSlices.BindProperty(m_NbrSlices);
        TextField ds_path_tf = root.Q<TextField>(name: "dataset-path");
        if (ds_path_tf == null) {
            Debug.LogError("dataset path text field was not found: dataset-path");
            return new VisualElement();
        }
        ds_path_tf.BindProperty(m_DatasetPath);
        Button select_ds_path_btn = root.Q<Button>(name: "select-dataset-path");
            if (select_ds_path_btn == null) {
                Debug.LogError("dataset path selection button was not found: select-dataset-path");
                return new VisualElement();
            }
        select_ds_path_btn.RegisterCallback<ClickEvent>(
            (ClickEvent evt) => {
                string path = EditorUtility.OpenFilePanel("Select a Unity Volumetric DataSet (UVDS) file", "", "uvds");
                if (path.Length != 0) {
                    m_DatasetPath.stringValue = path;
                    serializedObject.ApplyModifiedProperties(); 
                }
                evt.StopPropagation();
            }
        );
        Button generate_btn = root.Q<Button>(name: "generate-volume");
        if (generate_btn == null) {
                Debug.LogError("generate volume button was not found: generate-volume");
                return new VisualElement();
        }
        generate_btn.RegisterCallback<ClickEvent>(
            (ClickEvent evt) => {
                if (m_DatasetPath.stringValue.Length != 0) {
                    m_VolumetricDataset.LoadUVDS();
                }
                evt.StopPropagation();
            }
        );

        return root;
    }
}
