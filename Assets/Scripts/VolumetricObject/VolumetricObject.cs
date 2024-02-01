using UnityEngine;

namespace UnityCTVisualizer
{
    public interface IVolumetricVisualizer
    {
        VolumetricDataset VolumetricDataset { get; set; }
        ITransferFunction TransferFunction { get; set; }
    }

    [RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshFilter))]
    public class VolumetricObject : MonoBehaviour
    {
        int SHADER_DENSITIES_TEX_ID = Shader.PropertyToID("_Densities");
        int SHADER_TFTEX_ID = Shader.PropertyToID("_TFColors");

        VolumetricDataset m_VolumeDataset = null;
        public VolumetricDataset VolumetricDataset
        {
            private get => m_VolumeDataset;
            set
            {
                if (m_VolumeDataset != null) { } // TODO: do we need a cleanup here?
                m_VolumeDataset = value;
                m_AttachedMeshRenderer.sharedMaterial.SetTexture(
                    SHADER_DENSITIES_TEX_ID,
                    m_VolumeDataset.DensitiesSamplerTexture
                );
            }
        }
        ITransferFunction m_TransferFunction = null;
        public ITransferFunction TransferFunction
        {
            private get => m_TransferFunction;
            set
            {
                if (value != m_TransferFunction)
                {
                    m_TransferFunction.TransferFunctionTexChange -= OnTransferFunctionTexChange;
                    m_TransferFunction = value;
                    m_TransferFunction.TransferFunctionTexChange += OnTransferFunctionTexChange;
                }
            }
        }

        MeshRenderer m_AttachedMeshRenderer;

        void Awake()
        {
            m_AttachedMeshRenderer = gameObject.GetComponent<MeshRenderer>();
        }

        public void Init(VolumetricDataset volumetricDataset, ITransferFunction transferFunction)
        {
            VolumetricDataset = volumetricDataset;
            TransferFunction = transferFunction;
        }

        void OnTransferFunctionTexChange(Texture2D newTex)
        {
            m_AttachedMeshRenderer.sharedMaterial.SetTexture(SHADER_TFTEX_ID, newTex);
        }
    }
}
