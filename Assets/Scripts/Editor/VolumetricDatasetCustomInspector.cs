using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityCTVisualizer;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityCTVisualizer
{
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
        SerializedProperty m_Scale;
        SerializedProperty m_EulerRotation;
        SerializedProperty m_MinDensity;
        SerializedProperty m_MaxDensity;

        private void OnEnable()
        {
            m_VolumetricDataset = (VolumetricDataset)target;
            m_DatasetPath = serializedObject.FindProperty("m_DatasetPath");
            m_ImageWidth = serializedObject.FindProperty("m_ImageWidth");
            m_ImageHeight = serializedObject.FindProperty("m_ImageHeight");
            m_NbrSlices = serializedObject.FindProperty("m_NbrSlices");
            m_Scale = serializedObject.FindProperty("m_Scale");
            m_EulerRotation = serializedObject.FindProperty("m_EulerRotation");
            m_MinDensity = serializedObject.FindProperty("m_MinDensity");
            m_MaxDensity = serializedObject.FindProperty("m_MaxDensity");

            SceneView.duringSceneGui += OnSceneViewGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneViewGUI;
        }

        private void OnSceneViewGUI(SceneView sv) { }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = m_VolumetricDatasetInspectorXML.Instantiate();
            // UXML element queries
            Button select_ds_path_btn = root.Q<Button>(name: "select-dataset-path");
            TextField datasetPath = root.Q<TextField>(name: "dataset-path");
            IntegerField volumeWidth = root.Q<IntegerField>(name: "volume-width");
            IntegerField volumeHeight = root.Q<IntegerField>(name: "volume-height");
            IntegerField volumeSlices = root.Q<IntegerField>(name: "volume-slices");
            Vector3Field volumeScale = root.Q<Vector3Field>(name: "volume-scale");
            Vector3Field volumeRotation = root.Q<Vector3Field>(name: "volume-rotation");
            FloatField volumeMinDensity = root.Q<FloatField>(name: "min-density");
            FloatField volumeMaxDensity = root.Q<FloatField>(name: "max-density");
            Button generate_btn = root.Q<Button>(name: "generate-volume");
            if (
                select_ds_path_btn == null
                || datasetPath == null
                || volumeWidth == null
                || volumeHeight == null
                || volumeSlices == null
                || volumeScale == null
                || volumeRotation == null
                || volumeMinDensity == null
                || volumeMaxDensity == null
                || generate_btn == null
            )
            {
                Debug.LogError(
                    "at least one crucial UXML element was not found. Aborting custom editor ..."
                );
                return new VisualElement();
            }
            // binding UI elements to properties
            datasetPath.BindProperty(m_DatasetPath);
            volumeWidth.BindProperty(m_ImageWidth);
            volumeHeight.BindProperty(m_ImageHeight);
            volumeSlices.BindProperty(m_NbrSlices);
            volumeScale.BindProperty(m_Scale);
            volumeRotation.BindProperty(m_EulerRotation);
            volumeMinDensity.BindProperty(m_MinDensity);
            volumeMaxDensity.BindProperty(m_MaxDensity);
            // callbacks
            select_ds_path_btn.RegisterCallback<ClickEvent>(
                (ClickEvent evt) =>
                {
                    string path = EditorUtility.OpenFilePanel(
                        "Select a Unity Volumetric DataSet (UVDS) file",
                        "",
                        "uvds"
                    );
                    if (File.Exists(path))
                    {
                        Importer.ImportUVDS(path, m_VolumetricDataset);
                    }
                    evt.StopPropagation();
                }
            );
            generate_btn.RegisterCallback<ClickEvent>(
                (ClickEvent evt) =>
                {
                    m_VolumetricDataset.GenerateDensitiesTexture();
                    evt.StopPropagation();
                }
            );

            return root;
        }
    }
}
