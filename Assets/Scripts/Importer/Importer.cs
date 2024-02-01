using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityCTVisualizer
{
    public interface IUVDS
    {
        public string DatasetPath { get; set; }
        public ushort ImageWidth { get; set; }
        public ushort ImageHeight { get; set; }
        public ushort NbrSlices { get; set; }
        public Vector3 Scale { get; set; }
        public Vector3 EulerRotation { get; set; }
        public float MinDensity { get; set; }
        public float MaxDensity { get; set; }
        public float[] Densities { get; set; }
    }

    public static class Importer
    {
        // from https://docs.unity3d.com/Manual/class-Texture3D.html
        const ushort MAX_TEXTURE3D_DIM = 2048;

        const ushort DIMENSION_DEFAULT = 65535;
        const float SCALE_DEFAULT = -1f;

        /// <summary>
        /// Importers a volumetric dataset from a Unity Volumetric DataSet (UVDS) binary file
        /// </summary>
        /// <param name="datasetPath">absolute path to a UVDS binary file</param>
        /// <param name="volumetricDataset"></param>
        public static void ImportUVDS(string datasetPath, IUVDS volumetricDataset)
        {
            using (var stream = File.Open(datasetPath, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream))
                {
                    ushort imageWidth = reader.ReadUInt16();
                    ushort imageHeight = reader.ReadUInt16();
                    ushort nbrSlices = reader.ReadUInt16();
                    if (
                        imageWidth == DIMENSION_DEFAULT
                        || imageHeight == DIMENSION_DEFAULT
                        || nbrSlices == DIMENSION_DEFAULT
                    )
                    {
                        Debug.LogError(
                            $"Provided dataset has invalid default value(s) for its dimensions. Aborting ..."
                        );
                        return;
                    }
                    int dimension = imageWidth * imageHeight * nbrSlices;
                    if (Mathf.Max(imageWidth, imageHeight, nbrSlices) > MAX_TEXTURE3D_DIM)
                    {
                        Debug.LogError(
                            $"Exceeding maximum Texture3D dimension: {MAX_TEXTURE3D_DIM}. Aborting ..."
                        );
                        return;
                    }
                    float voxelWidth = reader.ReadSingle();
                    float voxelHeight = reader.ReadSingle();
                    float voxelDepth = reader.ReadSingle();
                    Vector3 scale = new(1, 1, 1);
                    if (
                        voxelWidth == SCALE_DEFAULT
                        || voxelHeight == SCALE_DEFAULT
                        || voxelDepth == SCALE_DEFAULT
                    )
                    {
                        Debug.LogWarning(
                            "Voxel dimension has invalid default value(s)."
                                + "Default scale (1, 1, 1) is used for the volume. The dimensions of the "
                                + "volume no longer reflect its dimensions in reality!"
                        );
                    }
                    else
                    {
                        scale.x = (voxelWidth / 1000.0f) * imageWidth; // mm to meters
                        scale.y = (voxelHeight / 1000.0f) * imageHeight;
                        scale.z = (voxelDepth / 1000.0f) * nbrSlices;
                    }
                    Vector3 eulerRotation = new(270.0f, 0.0f, 0.0f); // TODO: don't hard-code this
                    int i = 0;
                    float[] densities = new float[dimension];
                    for (; reader.BaseStream.Position != reader.BaseStream.Length; ++i)
                    {
                        densities[i] = Mathf.HalfToFloat(reader.ReadUInt16());
                    }
                    if (i != dimension)
                    {
                        Debug.LogError("Less elements in provided UVDS dataset than expected.");
                        return;
                    }
                    float minDensity = Mathf.HalfToFloat(reader.ReadUInt16());
                    float maxDensity = Mathf.HalfToFloat(reader.ReadUInt16());
                    if (minDensity > maxDensity)
                    {
                        Debug.LogWarning(
                            "Densities maximum and input values have invalid defaults. Recalculating ..."
                        );
                        minDensity = densities.Min();
                        maxDensity = densities.Max();
                    }
                    volumetricDataset.DatasetPath = datasetPath;
                    volumetricDataset.ImageWidth = imageWidth;
                    volumetricDataset.ImageHeight = imageHeight;
                    volumetricDataset.NbrSlices = nbrSlices;
                    volumetricDataset.Scale = scale;
                    volumetricDataset.EulerRotation = eulerRotation;
                    volumetricDataset.MinDensity = minDensity;
                    volumetricDataset.MaxDensity = maxDensity;
                    volumetricDataset.Densities = densities;
                }
            }
        }
    }
}
