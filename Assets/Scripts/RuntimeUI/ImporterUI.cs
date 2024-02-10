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

        [SerializeField]
        ProgressHandler m_ProgressHandler;

        void Awake()
        {
            // make sure that initially the progress handler is disabled
            m_ProgressHandler.gameObject.SetActive(false);
            FileBrowser.SetFilters(
                true,
                new FileBrowser.Filter("UnityVolumetricDataSet", ".uvds", ".uvds.zip")
            );
            FileBrowser.SetDefaultFilter(".uvds");
            m_FileDialog.onClick.AddListener(() => ShowLoadDialogCoroutine());
        }

        async void ShowLoadDialogCoroutine()
        {
            // disable import button
            m_FileDialog.interactable = false;
            await FileBrowser.WaitForLoadDialog(
                FileBrowser.PickMode.Files,
                title: "Select a Unity Volumetric DataSet (UVDS) File",
                loadButtonText: "Import Dataset"
            );
            if (FileBrowser.Success)
            {
                // enable progress handler
                m_ProgressHandler.gameObject.SetActive(true);
                string datasetPath = FileBrowser.Result[0];
                m_FilePath.text = datasetPath;
                // TODO: make dataset importer work on bytes array for multiplatform support
                // byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(datasetPath);
                VolumetricDataset volumetricDataset =
                    ScriptableObject.CreateInstance<VolumetricDataset>();
                await Task.Run(
                    () => Importer.ImportUVDS(datasetPath, volumetricDataset, m_ProgressHandler)
                );
                OnDatasetLoad?.Invoke(volumetricDataset);
#if DEBUG_UI
                Debug.Log("Dataset loaded successfully");
#endif
                // disable progress handler
                m_ProgressHandler.gameObject.SetActive(false);
                // enable import button again
                m_FileDialog.interactable = true;
                return;
            }
        }
    }
}
