using UnityEngine;

namespace UnityCTVisualizer {
    public interface IVolumetricVisualizer {
        VolumetricDataset VolumetricDataset { set; }
        ITransferFunction TransferFunction { set; }
        float AlphaCutoff { get; set; }
    }

    [RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshFilter))]
    public class VolumetricObject : MonoBehaviour, IVolumetricVisualizer {
        [SerializeField]
        MeshRenderer m_attached_mesh_renderer;

        readonly int SHADER_BRICK_CACHE_TEX_ID = Shader.PropertyToID("_BrickCache");
        readonly int SHADER_BRICK_CACHE_TEX_SIZE_ID = Shader.PropertyToID("_BrickCacheTexSize");
        readonly int SHADER_TFTEX_ID = Shader.PropertyToID("_TFColors");
        readonly int SHADER_ALPHA_CUTOFF_ID = Shader.PropertyToID("_AlphaCutoff");

        private VolumetricDataset m_volume_dataset = null;
        private ITransferFunction m_transfer_function = null;

        // visualization parameters
        private float m_AlphaCutoff = (254.0f) / 255.0f;

        public VolumetricDataset VolumetricDataset {
            set {
                m_volume_dataset = value;
                // request densities 3D texture generation
                m_attached_mesh_renderer.sharedMaterial.SetVector(
                    SHADER_BRICK_CACHE_TEX_SIZE_ID,
                    new Vector4(
                        m_volume_dataset.BrickCacheSize.x,
                        m_volume_dataset.BrickCacheSize.y,
                        m_volume_dataset.BrickCacheSize.z,
                        0.0f
                    )
                );
                m_attached_mesh_renderer.sharedMaterial.SetTexture(
                    SHADER_BRICK_CACHE_TEX_ID,
                    m_volume_dataset.BrickCacheTex
                );
                // scale mesh to match correct dimensions of the original volumetric data
                GetComponent<Transform>().localScale = m_volume_dataset.Metadata.Scale;
                // rotate mesh according to provided rotation
                GetComponent<Transform>().localRotation = Quaternion.Euler(
                    m_volume_dataset.Metadata.EulerRotation
                );
            }
        }

        /// <summary>
        ///     Sets transfer function for this volumetric object to use (for requesting color and alpha
        ///     lookup texture).
        /// </summary>
        public ITransferFunction TransferFunction {
            set {
                if (value != m_transfer_function) {
                    if (m_transfer_function != null)
                        m_transfer_function.TFColorsLookupTexChange -= OnTFColorsLookupTexChange;
                    m_transfer_function = value;
                    m_transfer_function.TFColorsLookupTexChange += OnTFColorsLookupTexChange;
                    m_transfer_function.ForceUpdateColorLookupTexture();
                }
            }
        }

        public float AlphaCutoff {
            get => m_AlphaCutoff;
            set {
                m_AlphaCutoff = Mathf.Clamp01(value);
                m_attached_mesh_renderer.sharedMaterial.SetFloat(
                    SHADER_ALPHA_CUTOFF_ID,
                    m_AlphaCutoff
                );
            }
        }

        INTERPOLATION m_InterpolationMethod;
        public INTERPOLATION InterpolationMethod {
            // https://docs.unity3d.com/ScriptReference/Shader-keywordSpace.html
            set {
                m_InterpolationMethod = value;
                switch (m_InterpolationMethod) {
                    case INTERPOLATION.NEAREST_NEIGHBOR:
                    break;

                    case INTERPOLATION.POST_TRILLINEAR_INTERPOLATIVE_CLASSIFICATION:
                    break;

                    default:
                    break;
                }
#if DEBUG
                Debug.Log($"New interpolation method: {m_InterpolationMethod}");
#endif
            }
        }

        void OnDisable() {
            if (m_transfer_function != null)
                m_transfer_function.TFColorsLookupTexChange -= OnTFColorsLookupTexChange;
        }

        void OnTFColorsLookupTexChange(Texture2D new_colors_lookup_tex) {
            m_attached_mesh_renderer.sharedMaterial.SetTexture(SHADER_TFTEX_ID, new_colors_lookup_tex);
        }
    }
}
