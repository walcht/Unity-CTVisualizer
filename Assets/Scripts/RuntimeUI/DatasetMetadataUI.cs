using TMPro;
using UnityEngine;

namespace UnityCTVisualizer {
    public class DatasetMetadataUI : MonoBehaviour {
        [SerializeField] TMP_InputField m_OriginalVolumeWidth;
        [SerializeField] TMP_InputField m_OriginalVolumeHeight;
        [SerializeField] TMP_InputField m_OriginalNumberSlices;

        [SerializeField] TMP_InputField m_BrickSize;
        [SerializeField] TMP_InputField m_NbrbricksX;
        [SerializeField] TMP_InputField m_NbrbricksY;
        [SerializeField] TMP_InputField m_NbrbricksZ;

        [SerializeField] TMP_InputField m_ColorDepth;

        [SerializeField] TMP_InputField m_VolumeScaleX;
        [SerializeField] TMP_InputField m_VolumeScaleY;
        [SerializeField] TMP_InputField m_VolumeScaleZ;

        [SerializeField] TMP_InputField m_DensityMin;
        [SerializeField] TMP_InputField m_DensityMax;

        void Awake() {
            m_OriginalVolumeWidth.readOnly = true;
            m_OriginalVolumeHeight.readOnly = true;
            m_VolumeScaleX.readOnly = true;
            m_VolumeScaleY.readOnly = true;
            m_VolumeScaleZ.readOnly = true;
            m_DensityMin.readOnly = true;
            m_DensityMax.readOnly = true;
        }

        /// <summary>
        ///     Initializes read-only input fields from a given UVDS dataset's metadata.
        ///     Call this before enabling this UI or during its lifetime to update it.
        /// </summary>
        ///
        /// <param name="metadata">
        ///     An imported UVDS dataset's metadata
        /// </param>
        public void Init(UVDSMetadata metadata) {
            m_OriginalVolumeWidth.text = metadata.OriginalImageWidth.ToString();
            m_OriginalVolumeHeight.text = metadata.OriginalImageHeight.ToString();
            m_OriginalNumberSlices.text = metadata.OriginalNbrSlices.ToString();

            m_BrickSize.text = metadata.BrickSize.ToString();
            m_NbrbricksX.text = metadata.NbrBricksX.ToString();
            m_NbrbricksY.text = metadata.NbrBricksY.ToString();
            m_NbrbricksZ.text = metadata.NbrBricksZ.ToString();

            m_ColorDepth.text = $"{metadata.ColourDepth} BPP";

            m_VolumeScaleX.text = metadata.Scale.x.ToString("0.00");
            m_VolumeScaleY.text = metadata.Scale.y.ToString("0.00");
            m_VolumeScaleZ.text = metadata.Scale.z.ToString("0.00");

            m_DensityMin.text = metadata.DensityMin.ToString("0.00");
            m_DensityMax.text = metadata.DensityMax.ToString("0.00");
        }
    }
}
