using UnityEngine;

namespace UnityCTVisualizer
{
    public enum TF
    {
        TF1D,
    }

    public class ManagerUI : MonoBehaviour
    {
        public GameObject m_VolumetricObjectPrefab;

        public ImporterUI m_ImporterUI;
        public DatasetMetadataUI m_MetadataUI;
        public VisualizationParametersUI m_VisualizationParamsUI;
        public TransferFunction1DUI m_TransferFunction1DUI;

        public Vector3 m_VolumetricObjectPosition;

        public TF m_DefaultTF = TF.TF1D;
        public INTERPOLATION m_DefaultInterpolation = INTERPOLATION.NEAREST_NEIGHBOR;

        VolumetricObject m_VolumetricObject = null;
        VolumetricDataset m_VolumetricDataset = null;

        void Awake()
        {
            m_ImporterUI.gameObject.SetActive(true);
            m_ImporterUI.OnDatasetLoad += OnDatasetLoad;
            m_VisualizationParamsUI.OnTransferFunctionChange += OnTransferFunctionChange;
            m_VisualizationParamsUI.OnInterpolationChange += OnInterpolationChange;
        }

        void OnDatasetLoad(VolumetricDataset volumetricDataset)
        {
            m_VolumetricDataset = volumetricDataset;
            m_VolumetricObject = Instantiate<GameObject>(
                    m_VolumetricObjectPrefab,
                    position: m_VolumetricObjectPosition,
                    rotation: Quaternion.identity
                )
                .GetComponent<VolumetricObject>();
            m_VolumetricObject.VolumetricDataset = volumetricDataset;
            // TODO: add the defaults in VisualizationParametersUI's Init method
            // create default transfer function ScriptableObject
            OnTransferFunctionChange(m_DefaultTF);
            // set default interpolation method
            OnInterpolationChange(m_DefaultInterpolation);
            m_MetadataUI.gameObject.SetActive(true);
            m_MetadataUI.Init(volumetricDataset);
            m_VisualizationParamsUI.Init(volumetricDataset, m_VolumetricObject);
            m_VisualizationParamsUI.gameObject.SetActive(true);
        }

        void OnTransferFunctionChange(TF newTF)
        {
            switch (newTF)
            {
                case TF.TF1D:
                    var tfSO = ScriptableObject.CreateInstance<TransferFunction1D>();
                    tfSO.Init();
                    // disable other TFUIs here
                    m_TransferFunction1DUI.Init(tfSO, m_VolumetricDataset);
                    m_TransferFunction1DUI.gameObject.SetActive(true);
                    m_VolumetricObject.TransferFunction = tfSO;
                    break;

                default:
                    Debug.LogError($"Transfer function: {newTF} is not yet supported");
                    break;
            }
        }

        void OnInterpolationChange(INTERPOLATION newInter)
        {
            m_VolumetricObject.InterpolationMethod = newInter;
        }
    }
}
