using System;
using System.IO;
using UnityEngine;

namespace UnityCTVisualizer
{
    public interface IUVDSMetadata
    {
        public int OriginalImageWidth { get; set; }
        public int OriginalImageHeight { get; set; }
        public int OriginalNbrSlices { get; set; }
        public int ImageWidth { get; set; }  // multiple of bricksize
        public int ImageHeight { get; set; }  // multiple of bricksize
        public int NbrSlices { get; set; }  // multiple of bricksize
        public int BrickSize { get; set; }
        public int BrickSizeBytes { get; set; }
        public int NbrBricksX { get; set; }
        public int NbrBricksY { get; set; }
        public int NbrBricksZ { get; set; }
        public int TotalNbrBricks { get; set; }
        public int ResolutionLevels { get; set; }
        public ColorDepth ColourDepth { get; set; }
        public bool Lz4Compressed { get; set; }
        public Vector3 Scale { get; set; }
        public Vector3 EulerRotation { get; set; }
        public float DensityMin { get; set; }
        public float DensityMax { get; set; }
    }
    public interface IProgressHandler
    {
        float Progress { set; }
        string Message { set; }
    }

    public enum ColorDepth
    {
        UINT8, UINT16, FLOAT16
    }

    public static class Importer
    {
        const float SCALE_DEFAULT = -1.0f;

        /// <summary>
        /// Creates a VolumetricData ScriptableObject instance, imports metadata attributes,
        /// and stores filepath references to volume bricks.
        /// </summary>
        /// <param name="directoryPath">absolute path to a UVDS dataset directory containing a metadata.txt file
        /// and .uvds volume chunk binary files.</param>
        public static VolumetricDataset ImporterUVDSDataset(string directoryPath, IProgressHandler progressHandler = null)
        {
            string metadataFilePath;
            string[] volumeChunksFilePaths;

            if (progressHandler != null)
            {
                progressHandler.Progress = 0.0f;
                progressHandler.Message = "Importing UVDS dataset ...";
            }

            try
            {
                metadataFilePath = Directory.GetFiles(directoryPath, "metadata.txt")[0];
                volumeChunksFilePaths = Directory.GetFiles(directoryPath, "*.uvds");
                Array.Sort(volumeChunksFilePaths);

                if (progressHandler != null)
                {
                    progressHandler.Progress = 0.5f;
                    progressHandler.Message = $"Found {volumeChunksFilePaths.Length} volume chunks. Importing metadata attributes ...";
                }
            }
            catch
            {
                throw new FileLoadException("Failed to extract UVDS dataset from provided directory");
            }
            VolumetricDataset volumetricDataset =
                ScriptableObject.CreateInstance<VolumetricDataset>();
            using (StreamReader sr = new(metadataFilePath))
            {
                string line;
                Vector3 scale = new(1, 1, 1);
                Vector3 eulerRotation = new(1, 1, 1);
                while ((line = sr.ReadLine()) != null)
                {
                    string[] split = line.Split("=");
                    switch (split[0])
                    {
                        case "originalimagewidth":
                            volumetricDataset.OriginalImageWidth = int.Parse(split[1]);
                            break;
                        case "originalimageheight":
                            volumetricDataset.OriginalImageHeight = int.Parse(split[1]);
                            break;
                        case "originalnbrslices":
                            volumetricDataset.OriginalNbrSlices = int.Parse(split[1]);
                            break;
                        case "imagewidth":
                            volumetricDataset.ImageWidth = int.Parse(split[1]);
                            break;
                        case "imageheight":
                            volumetricDataset.ImageHeight = int.Parse(split[1]);
                            break;
                        case "nbrslices":
                            volumetricDataset.NbrSlices = int.Parse(split[1]);
                            break;
                        case "bricksize":
                            volumetricDataset.BrickSize = int.Parse(split[1]);
                            break;
                        case "bricksizebytes":
                            volumetricDataset.BrickSizeBytes = int.Parse(split[1]);
                            break;
                        case "nbrbricksX":
                            volumetricDataset.NbrBricksX = int.Parse(split[1]);
                            break;
                        case "nbrbricksY":
                            volumetricDataset.NbrBricksY = int.Parse(split[1]);
                            break;
                        case "nbrbricksZ":
                            volumetricDataset.NbrBricksZ = int.Parse(split[1]);
                            break;
                        case "totalnbrbricks":
                            volumetricDataset.TotalNbrBricks = int.Parse(split[1]);
                            break;
                        case "resolutionlevels":
                            volumetricDataset.ResolutionLevels = int.Parse(split[1]);
                            break;
                        case "colordepth":
                            switch (split[1])
                            {
                                case "uint8":
                                    volumetricDataset.ColourDepth = ColorDepth.UINT8;
                                    break;
                                case "uint16":
                                    volumetricDataset.ColourDepth = ColorDepth.UINT16;
                                    break;
                                case "float16":
                                    volumetricDataset.ColourDepth = ColorDepth.FLOAT16;
                                    break;
                                default:
                                    break;
                            }

                            break;
                        case "lz4compressed":
                            volumetricDataset.Lz4Compressed = int.Parse(split[1]) == 1;
                            break;
                        case "voxeldimX":
                            float voxelDimX = float.Parse(split[1]);
                            if (voxelDimX == SCALE_DEFAULT)
                            {
                                scale.x = 1.0f;
                                Debug.LogWarning(
                                    "Voxel dimension X has invalid default value."
                                        + "Default scale 1 is used for the volume along the X axis. The dimensions of the "
                                        + "volume no longer reflect its dimensions in reality!"
                                );
                                break;
                            }
                            scale.x = (voxelDimX / 1000.0f) * volumetricDataset.ImageWidth;
                            break;
                        case "voxeldimY":
                            float voxelDimY = float.Parse(split[1]);
                            if (voxelDimY == SCALE_DEFAULT)
                            {
                                scale.y = 1.0f;
                                Debug.LogWarning(
                                    "Voxel dimension Y has invalid default value."
                                        + "Default scale 1 is used for the volume along the Y axis. The dimensions of the "
                                        + "volume no longer reflect its dimensions in reality!"
                                );
                                break;
                            }
                            scale.y = (voxelDimY / 1000.0f) * volumetricDataset.ImageHeight;
                            break;
                        case "voxeldimZ":
                            float voxelDimZ = float.Parse(split[1]);
                            if (voxelDimZ == SCALE_DEFAULT)
                            {
                                scale.z = 1.0f;
                                Debug.LogWarning(
                                    "Voxel dimension Z has invalid default value."
                                        + "Default scale 1 is used for the volume along the Z axis. The dimensions of the "
                                        + "volume no longer reflect its dimensions in reality!"
                                );
                                break;
                            }
                            scale.z = (voxelDimZ / 1000.0f) * volumetricDataset.NbrSlices;
                            break;
                        case "eulerrotX":
                            eulerRotation.x = float.Parse(split[1]);
                            break;
                        case "eulerrotY":
                            eulerRotation.y = float.Parse(split[1]);
                            break;
                        case "eulerrotZ":
                            eulerRotation.z = float.Parse(split[1]);
                            break;
                        case "densitymin":
                            volumetricDataset.DensityMin = float.Parse(split[1]);
                            break;
                        case "densitymax":
                            volumetricDataset.DensityMax = float.Parse(split[1]);
                            break;
                        default:
                            break;
                    }
                }
                volumetricDataset.Scale = scale;
                volumetricDataset.EulerRotation = eulerRotation;
                if (progressHandler != null)
                {
                    progressHandler.Progress = 1f;
                    progressHandler.Message = $"UVDS dataset imported successfully";
                }
            }
            return volumetricDataset;
        }

    }
}
