using Cysharp.Threading.Tasks;
using SimpleFileBrowser;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCTVisualizer {
    public class ImporterUI : MonoBehaviour {

        /// <summary>
        ///     Invoked when a UVDS dataset is successfully imported. The VolumetricDataset
        ///     ScriptableObject instance is passed to the handler(s).
        /// </summary>
        public event Action<VolumetricDataset> OnDatasetLoad;

        [SerializeField]
        Button m_file_dialog;

        [SerializeField]
        TMP_InputField m_fp;

        [SerializeField]
        ProgressHandler m_progress_handler;

        void Awake() {
            // make sure that initially the progress handler is disabled
            m_progress_handler.gameObject.SetActive(false);
            FileBrowser.SetFilters(
                true,
                new FileBrowser.Filter("UnityVolumetricDataSet", ".uvds", ".uvds.zip")
            );
            FileBrowser.SetDefaultFilter(".uvds");
            m_file_dialog.onClick.AddListener(() => ShowLoadDialogCoroutine());
        }

        async void ShowLoadDialogCoroutine() {
            // disable import button
            m_file_dialog.interactable = false;
            await FileBrowser.WaitForLoadDialog(
                FileBrowser.PickMode.Folders,
                title: "Select a Unity Volumetric DataSet (UVDS) dataset directory",
                loadButtonText: "Import Dataset"
            );
            if (FileBrowser.Success) {
                string dir_path = FileBrowser.Result[0];
                m_fp.text = dir_path;
                VolumetricDataset volumetric_dataset;
                try {
                    volumetric_dataset = Importer.ImportUVDSDataset(dir_path, m_progress_handler);
                } catch (FileLoadException) {
                    return;
                } catch (Exception e) {
                    Debug.LogException(e);
                    return;
                }
                // TODO: make dataset importer work on bytes array for multiplatform support
                // byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(datasetPath);
                OnDatasetLoad?.Invoke(volumetric_dataset);
#if DEBUG_UI
                Debug.Log("Dataset loaded successfully");
#endif
                // enable import button again
                m_file_dialog.interactable = true;
                return;
            }
        }
    }
}
