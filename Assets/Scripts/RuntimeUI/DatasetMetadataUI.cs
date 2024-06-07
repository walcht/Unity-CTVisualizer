using TMPro;
using UnityEngine;

namespace UnityCTVisualizer
{
    public class DatasetMetadataUI : MonoBehaviour
    {
        [SerializeField]
        TMP_InputField m_VolumeWidth;

        [SerializeField]
        TMP_InputField m_VolumeHeight;

        [SerializeField]
        TMP_InputField m_NumberSlices;

        [SerializeField]
        TMP_InputField m_VolumeScaleX;

        [SerializeField]
        TMP_InputField m_VolumeScaleY;

        [SerializeField]
        TMP_InputField m_VolumeScaleZ;

        [SerializeField]
        TMP_InputField m_DensityMin;

        [SerializeField]
        TMP_InputField m_DensityMax;

        void Awake()
        {
            m_VolumeWidth.readOnly = true;
            m_VolumeHeight.readOnly = true;
            m_VolumeScaleX.readOnly = true;
            m_VolumeScaleY.readOnly = true;
            m_VolumeScaleZ.readOnly = true;
            m_DensityMin.readOnly = true;
            m_DensityMax.readOnly = true;
        }

        /// <summary>
        /// Initializes read-only input fields from a given UVDS dataset.
        /// Call this before enabling this UI or during its lifetime to update it.
        /// </summary>
        /// <param name="uvdsDataset">An imported UVDS dataset</param>
        public void Init(IUVDSMetadata metadata)
        {
            m_VolumeWidth.text = metadata.ImageWidth.ToString();
            m_VolumeHeight.text = metadata.ImageHeight.ToString();
            m_NumberSlices.text = metadata.NbrSlices.ToString();
            m_VolumeScaleX.text = metadata.Scale.x.ToString("0.00");
            m_VolumeScaleY.text = metadata.Scale.y.ToString("0.00");
            m_VolumeScaleZ.text = metadata.Scale.z.ToString("0.00");
            m_DensityMin.text = metadata.DensityMin.ToString("0.00");
            m_DensityMax.text = metadata.DensityMax.ToString("0.00");
        }
    }
}
