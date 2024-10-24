using System;
using Unity.VisualScripting;
using UnityEngine;

namespace UnityCTVisualizer {
    public enum TF {
        TF1D,
        // TF2D - currently not added
    }

    public class ManagerUI : MonoBehaviour {
        public GameObject m_VolumetricObjectPrefab;

        public ImporterUI m_ImporterUI;
        public DatasetMetadataUI m_MetadataUI;
        public VisualizationParametersUI m_VisualizationParamsUI;
        public TransferFunction1DUI m_TransferFunction1DUI;
        public ProgressHandler m_ProgressHandlerUI;

        private VolumetricObject m_VolumetricObject;
        public Vector3 m_VolumetricObjectPosition;

        void Awake() {
            m_ImporterUI.gameObject.SetActive(true);
        }

        private void OnEnable() {
            VisualizationParametersEvents.ModelTFChange += OnModelTFChange;

            m_ImporterUI.OnDatasetLoad += OnDatasetLoad;
        }

        private void OnDisable() {
            VisualizationParametersEvents.ModelTFChange += OnModelTFChange;

            m_ImporterUI.OnDatasetLoad -= OnDatasetLoad;
        }

        private void OnModelTFChange(TF new_tf, ITransferFunction tf_so) {
            switch (new_tf) {
                case TF.TF1D:
                // disable other TFUIs here
                Debug.Log($"OnModelTFChange{new_tf}");
                m_TransferFunction1DUI.Init((TransferFunction1D)tf_so);
                m_TransferFunction1DUI.gameObject.SetActive(true);
                break;

                default:
                throw new UnexpectedEnumValueException<TF>(new_tf);
            }
        }

        void OnDatasetLoad(VolumetricDataset volumetricDataset) {
            // TODO: improve this. It's not UI's job to instantiate this ...
            m_VolumetricObject = Instantiate<GameObject>(
                    m_VolumetricObjectPrefab,
                    position: m_VolumetricObjectPosition,
                    rotation: Quaternion.identity
                )
                .GetComponent<VolumetricObject>();
            m_VolumetricObject.Init(volumetricDataset, m_ProgressHandlerUI);
            m_VolumetricObject.enabled = true;

            m_MetadataUI.Init(volumetricDataset.Metadata);
            m_MetadataUI.gameObject.SetActive(true);

            // initial state for these objects is "undefined" but they are listening for change
            m_VisualizationParamsUI.gameObject.SetActive(true);

            // make the volumetric model dispatch its default visualization params state so that
            // listeners get the default visualization parameters
            volumetricDataset.DispatchVisualizationParamsChangeEvents();
        }
    }
}
