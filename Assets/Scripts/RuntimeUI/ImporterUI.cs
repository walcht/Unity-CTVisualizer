using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    public class ImporterUI : MonoBehaviour
    {
        /// <summary>
        /// Invoked when a UVDS dataset is successfully imported. The VolumetricDataset
        /// ScriptableObject instance is passed to the handler(s).
        /// </summary>
        public event Action<VolumetricDataset> OnDatasetLoad;

        [SerializeField]
        Button m_FileDialog;

        [SerializeField]
        TMP_InputField m_FilePath;

        void Awake()
        {
            FileBrowser.SetFilters(true, new FileBrowser.Filter("UnityVolumetricDataSet", ".uvds"));
            FileBrowser.SetDefaultFilter(".uvds");
            m_FileDialog.onClick.AddListener(() => ShowLoadDialogCoroutine());
        }

        async void ShowLoadDialogCoroutine()
        {
            await FileBrowser.WaitForLoadDialog(
                FileBrowser.PickMode.Files,
                title: "Select a Unity Volumetric DataSet (UVDS) File",
                loadButtonText: "Import Dataset"
            );
            if (FileBrowser.Success)
            {
                string datasetPath = FileBrowser.Result[0];
                m_FilePath.text = datasetPath;
                // TODO: make dataset importer work on bytes array for multiplatform support
                // byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(datasetPath);
                VolumetricDataset volumetricDataset =
                    ScriptableObject.CreateInstance<VolumetricDataset>();
                await Task.Run(() => Importer.ImportUVDS(datasetPath, volumetricDataset));
                OnDatasetLoad?.Invoke(volumetricDataset);
                Debug.Log("Dataset loaded successfully");
                return;
            }
        }
    }
}
