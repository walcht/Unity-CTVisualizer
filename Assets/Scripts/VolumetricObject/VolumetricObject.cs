using UnityEngine;

namespace UnityCTVisualizer
{
    public interface IVolumetricVisualizer
    {
        VolumetricDataset VolumetricDataset { set; }
        ITransferFunction TransferFunction { set; }
        float AlphaCutoff { get; set; }
    }

    [RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshFilter))]
    public class VolumetricObject : MonoBehaviour, IVolumetricVisualizer
    {
        [SerializeField]
        MeshRenderer m_AttachedMeshRenderer;

        int SHADER_DENSITIES_TEX_ID = Shader.PropertyToID("_Densities");
        int SHADER_TFTEX_ID = Shader.PropertyToID("_TFColors");
        int SHADER_ALPHA_CUTOFF_ID = Shader.PropertyToID("_AlphaCutoff");

        VolumetricDataset m_VolumeDataset = null;
        public VolumetricDataset VolumetricDataset
        {
            set
            {
                if (m_VolumeDataset != null)
                {
                    m_VolumeDataset.OnDensitiesChange -= OnDenstitiesTexChange;
                } // TODO: do we need a cleanup here?
                m_VolumeDataset = value;
                m_VolumeDataset.OnDensitiesChange += OnDenstitiesTexChange;
                // request densities 3D texture generation
                m_VolumeDataset.TryGenerateDensitiesTexture();
                // scale mesh to match correct dimensions of the original volumetric data
                this.GetComponent<Transform>().localScale = m_VolumeDataset.Scale;
            }
        }
        ITransferFunction m_TransferFunction = null;

        /// <summary>
        /// Sets transfer function for this volumetric object to use (for requesting color and alpha
        /// lookup texture).
        /// </summary>
        public ITransferFunction TransferFunction
        {
            set
            {
                if (value != m_TransferFunction)
                {
                    if (m_TransferFunction != null)
                        m_TransferFunction.TransferFunctionTexChange -= OnTransferFunctionTexChange;
                    m_TransferFunction = value;
                    m_TransferFunction.TransferFunctionTexChange += OnTransferFunctionTexChange;
                    m_TransferFunction.ForceUpdateColorLookupTexture();
                }
            }
        }

        float m_AlphaCutoff = (254.0f) / 255.0f;
        public float AlphaCutoff
        {
            get => m_AlphaCutoff;
            set
            {
                m_AlphaCutoff = Mathf.Clamp01(value);
                m_AttachedMeshRenderer.sharedMaterial.SetFloat(
                    SHADER_ALPHA_CUTOFF_ID,
                    m_AlphaCutoff
                );
            }
        }

        void OnDisable()
        {
            if (m_VolumeDataset != null)
                m_VolumeDataset.OnDensitiesChange -= OnDenstitiesTexChange;
            if (m_TransferFunction != null)
                m_TransferFunction.TransferFunctionTexChange -= OnTransferFunctionTexChange;
        }

        void OnTransferFunctionTexChange(Texture2D newTex)
        {
            m_AttachedMeshRenderer.sharedMaterial.SetTexture(SHADER_TFTEX_ID, newTex);
        }

        void OnDenstitiesTexChange(Texture3D newDensitiesTex)
        {
            m_AttachedMeshRenderer.sharedMaterial.SetTexture(
                SHADER_DENSITIES_TEX_ID,
                newDensitiesTex
            );
        }
    }
}
