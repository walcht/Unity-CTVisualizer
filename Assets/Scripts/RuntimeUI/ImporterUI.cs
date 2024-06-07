using Cysharp.Threading.Tasks;
using SimpleFileBrowser;
using System;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

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
                FileBrowser.PickMode.Folders,
                title: "Select a Unity Volumetric DataSet (UVDS) dataset directory",
                loadButtonText: "Import Dataset"
            );
            if (FileBrowser.Success)
            {
                string dirPath = FileBrowser.Result[0];
                m_FilePath.text = dirPath;
                VolumetricDataset volumetricDataset;
                try
                {
                    volumetricDataset = Importer.ImporterUVDSDataset(dirPath, m_ProgressHandler);
                }
                catch (FileLoadException)
                {
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return;
                }
                // TODO: make dataset importer work on bytes array for multiplatform support
                // byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(datasetPath);
                OnDatasetLoad?.Invoke(volumetricDataset);
#if DEBUG_UI
                Debug.Log("Dataset loaded successfully");
#endif
                // enable import button again
                m_FileDialog.interactable = true;
                return;
            }
        }
    }
}
