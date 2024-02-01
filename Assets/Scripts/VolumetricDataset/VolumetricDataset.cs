using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityCTVisualizer
{
    [CreateAssetMenu(
        fileName = "volumetric_dataset",
        menuName = "UnityCTVisualizer/VolumetricDataset"
    )]
    public class VolumetricDataset : ScriptableObject, IUVDS
    {
        [SerializeField]
        string m_DatasetPath;
        public string DatasetPath
        {
            get => m_DatasetPath;
            set { m_DatasetPath = value; }
        }

        [SerializeField]
        ushort m_ImageWidth;
        public ushort ImageWidth
        {
            get => m_ImageWidth;
            set { m_ImageWidth = value; }
        }

        [SerializeField]
        ushort m_ImageHeight;
        public ushort ImageHeight
        {
            get => m_ImageHeight;
            set { m_ImageHeight = value; }
        }

        [SerializeField]
        ushort m_NbrSlices;
        public ushort NbrSlices
        {
            get => m_NbrSlices;
            set { m_NbrSlices = value; }
        }

        [SerializeField]
        Vector3 m_Scale;
        public Vector3 Scale
        {
            get => m_Scale;
            set { m_Scale = value; }
        }

        [SerializeField]
        Vector3 m_EulerRotation;
        public Vector3 EulerRotation
        {
            get => m_EulerRotation;
            set { m_EulerRotation = value; }
        }

        [SerializeField]
        float m_MinDensity = float.NegativeInfinity;
        public float MinDensity
        {
            get => m_MinDensity;
            set
            {
                m_MinDensity = value;
                m_MinDensity = Mathf.Max(m_MinDensity, m_ClampMinDensity);
            }
        }

        [SerializeField]
        float m_MaxDensity = float.PositiveInfinity;
        public float MaxDensity
        {
            get => m_MaxDensity;
            set
            {
                m_MaxDensity = value;
                m_MaxDensity = Mathf.Min(m_MaxDensity, m_ClampMaxDensity);
            }
        }

        [SerializeField]
        float[] m_Densities;
        public float[] Densities
        {
            get => m_Densities;
            set { m_Densities = value; }
        }

        [SerializeField]
        float m_ClampMaxDensity = float.PositiveInfinity;
        public float ClampMaxDensity
        {
            get => m_ClampMaxDensity;
            set
            {
                m_ClampMaxDensity = value;
                m_MaxDensity = Mathf.Min(m_MaxDensity, m_ClampMaxDensity);
            }
        }

        [SerializeField]
        float m_ClampMinDensity = float.NegativeInfinity;
        public float ClampMinDensity
        {
            get => m_ClampMinDensity;
            set
            {
                m_ClampMinDensity = value;
                m_MinDensity = Mathf.Max(m_MinDensity, m_ClampMinDensity);
            }
        }

        [SerializeField]
        Texture3D m_DensitiesSampler = null;
        public Texture3D DensitiesSamplerTexture
        {
            get
            {
                if (m_DensitiesSampler == null)
                    GenerateDensitiesTexture();
                return m_DensitiesSampler;
            }
            private set { m_DensitiesSampler = value; }
        }

        [SerializeField]
        Texture3D m_DensitiesGradientSampler = null;
        public Texture3D GetDensitiesGradientSampler
        {
            get
            {
                if (m_DensitiesGradientSampler == null)
                    GenerateGradientMagnitudeVolume();
                return m_DensitiesGradientSampler;
            }
            private set { m_DensitiesGradientSampler = value; }
        }

        public const int HISTOGRAM_BINS = 1024;

        public bool IsVolumeGenerated()
        {
            return m_DensitiesSampler != null;
        }

        /// <summary>
        /// Divides density range (after optional clamping) into HISTOGRAM_BINS bins.
        /// </summary>
        /// <returns>Array of frequencies that has the size: HISTOGRAM_BINS</returns>
        public float[] GenerateBinFrequencies()
        {
            if (Densities == null)
            {
                Debug.LogError("Densities have to be initialized before calling this function.");
                return null;
            }
            int[] frequencies = new int[HISTOGRAM_BINS];
            float BIN_WIDTH = (m_MaxDensity - m_MinDensity) / HISTOGRAM_BINS;
            for (int i = 0; i < Densities.Length; ++i)
            {
                int freqIdx = (int)
                    Mathf.Floor(
                        Mathf.Clamp(Densities[i], m_ClampMinDensity, m_ClampMaxDensity) / BIN_WIDTH
                    );
                ++frequencies[freqIdx];
            }
            float[] normalizedFrequencies = new float[HISTOGRAM_BINS];
            int freqMin = frequencies.Min();
            float range = (float)(frequencies.Max() - freqMin);
            for (int j = 0; j < HISTOGRAM_BINS; ++j)
            {
                normalizedFrequencies[j] = (frequencies[j] - freqMin) / range;
            }
            return normalizedFrequencies;
        }

        /// <summary>
        /// Generates volume densities (Texture3D) according to volume generation settings
        /// </summary>
        public void GenerateDensitiesTexture()
        {
            if (Densities == null)
            {
                Debug.LogError(
                    "densities array has to be initialized before generating the Texture3D"
                );
                return;
            }
            if (SystemInfo.SupportsTextureFormat(TextureFormat.RHalf))
            {
                m_DensitiesSampler = new Texture3D(
                    ImageWidth,
                    ImageHeight,
                    NbrSlices,
                    TextureFormat.RHalf,
                    false
                );
                ushort[] pixelData = new ushort[Densities.Length];
                for (int i = 0; i < Densities.Length; ++i)
                {
                    pixelData[i] = Mathf.FloatToHalf(
                        (
                            Mathf.Clamp(Densities[i], m_ClampMinDensity, m_ClampMaxDensity)
                            - m_MinDensity
                        ) / (m_MaxDensity - m_MinDensity)
                    );
                }
                m_DensitiesSampler.SetPixelData(pixelData, 0);
            }
            else
            {
                m_DensitiesSampler = new Texture3D(
                    ImageWidth,
                    ImageHeight,
                    NbrSlices,
                    TextureFormat.RFloat,
                    false
                );
                float[] pixelData = new float[Densities.Length];
                for (int i = 0; i < Densities.Length; ++i)
                {
                    pixelData[i] =
                        (
                            Mathf.Clamp(Densities[i], m_ClampMinDensity, m_ClampMaxDensity)
                            - m_MinDensity
                        ) / (m_MaxDensity - m_MinDensity);
                }
                m_DensitiesSampler.SetPixelData(pixelData, 0);
            }
            m_DensitiesSampler.wrapMode = TextureWrapMode.Clamp;
            m_DensitiesSampler.Apply();
        }

        /// <summary>
        /// Exports generated volume densities texture
        /// </summary>
        public string ExportDensitiesTexture(string exportPath = null)
        {
            if (m_DensitiesSampler == null)
            {
                this.GenerateDensitiesTexture();
            }
            if (exportPath == null)
            {
                System.DateTime centuryBegin = new System.DateTime(2024, 1, 1);
                System.DateTime currentDate = System.DateTime.Now;
                long elapsedTicks = (long)((currentDate.Ticks - centuryBegin.Ticks) / 10000.0f);
                exportPath =
                    "Assets/GeneratedTextures/densities_texture_"
                    + $"{Path.GetFileNameWithoutExtension(m_DatasetPath)}_{elapsedTicks}";
                AssetDatabase.CreateAsset(m_DensitiesSampler, exportPath);
                return exportPath;
            }
            AssetDatabase.CreateAsset(m_DensitiesSampler, exportPath);
            return exportPath;
        }

        /// <summary>
        /// Exports generated gradient magnitude volume texture
        /// </summary>
        /// <param name="exportPath">if not provided will export to Assets/GeneratedTextures/. Uses
        /// this provided path otherwise</param>
        public void ExportGradientMagnitudeTexture(string exportPath = null)
        {
            // TODO: [Adrienne] can you try to implement this? It is very similar to
            // ExportDensitiesTexture method.
        }

        /// <summary>
        /// Accesses the flattened densities array in a volumetric-coordinate-system manner
        /// </summary>
        public float AccessVolumeXYZ(int x, int y, int z)
        {
            return Densities[z * ImageWidth * ImageHeight + (ImageWidth * y) + x];
        }

        /// <summary>
        /// Generates volume gradient magnitude (Texture3D) from flattened densities array
        /// </summary>
        public void GenerateGradientMagnitudeVolume()
        {
            if (Densities == null)
            {
                Debug.LogError(
                    "densities array has to be initialized before generating the Texture3D"
                );
                return;
            }
            // TODO:    [Adrienne] can you implement this function to generate a Texture3D of gradient
            //          magnitudes?
            if (SystemInfo.SupportsTextureFormat(TextureFormat.RHalf))
            {
                m_DensitiesSampler = new Texture3D(
                    ImageWidth,
                    ImageHeight,
                    NbrSlices,
                    TextureFormat.RHalf,
                    false
                );
            }
            else
            {
                m_DensitiesSampler = new Texture3D(
                    ImageWidth,
                    ImageHeight,
                    NbrSlices,
                    TextureFormat.RFloat,
                    false
                );
            }
        }
    }
}
