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
        float m_MinDensity;
        public float MinDensity
        {
            get => m_MinDensity;
            set { m_MinDensity = value; }
        }

        [SerializeField]
        float m_MaxDensity;
        public float MaxDensity
        {
            get => m_MaxDensity;
            set { m_MaxDensity = value; }
        }

        [SerializeField]
        float[] m_Densities;
        public float[] Densities
        {
            get => m_Densities;
            set { m_Densities = value; }
        }

        public float m_ClampMaxDensity;
        public float m_ClampMinDensity;

        [SerializeField]
        Texture3D m_DensitiesSampler = null;
        public Texture3D GetDensitiesSampler
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

        public bool IsVolumeGenerated()
        {
            return m_DensitiesSampler != null;
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
                        (Densities[i] - MinDensity) / (MaxDensity - MinDensity)
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
                    pixelData[i] = (Densities[i] - MinDensity) / (MaxDensity - MinDensity);
                }
                m_DensitiesSampler.SetPixelData(pixelData, 0);
            }
            m_DensitiesSampler.wrapMode = TextureWrapMode.Clamp;
            m_DensitiesSampler.Apply();
        }

        /// <summary>
        /// Exports generated volume densities texture
        /// </summary>
        public void ExportDensitiesTexture(string exportPath = null)
        {
            if (m_DensitiesSampler == null)
            {
                Debug.LogError("Canno't export an uninitialized Texture3D");
                return;
            }
            if (exportPath == null)
            {
                System.DateTime centuryBegin = new System.DateTime(2024, 1, 1);
                System.DateTime currentDate = System.DateTime.Now;
                long elapsedTicks = (long)((currentDate.Ticks - centuryBegin.Ticks) / 10000.0f);
                AssetDatabase.CreateAsset(
                    m_DensitiesSampler,
                    $"Assets/GeneratedTextures/texture3d_{elapsedTicks}"
                );
                return;
            }
            // TODO: [Adrienne] can you add a check for export path before executing this code?
            AssetDatabase.CreateAsset(m_DensitiesSampler, exportPath);
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
