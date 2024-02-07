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

        /// <summary>
        /// Sets transfer function for this volumetric object to use (for requesting color and alpha
        /// lookup texture).
        /// </summary>
        public ITransferFunction TransferFunction
        {
            private get => m_TransferFunction;
            set
            {
                if (value != m_TransferFunction)
                {
                    if (m_TransferFunction != null)
                        m_TransferFunction.TransferFunctionTexChange -= OnTransferFunctionTexChange;
                    m_TransferFunction = value;
                    m_TransferFunction.TransferFunctionTexChange += OnTransferFunctionTexChange;
                    m_TransferFunction.TryUpdateColorLookupTexture();
                }
            }
        }

        MeshRenderer m_AttachedMeshRenderer;

        void Awake()
        {
            m_AttachedMeshRenderer = gameObject.GetComponent<MeshRenderer>();
        }

        void OnTransferFunctionTexChange(Texture2D newTex)
        {
            m_AttachedMeshRenderer.sharedMaterial.SetTexture(SHADER_TFTEX_ID, newTex);
        }
    }
}
