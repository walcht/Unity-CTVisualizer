using System;
using System.IO;
using UnityEngine;

namespace UnityCTVisualizer {
    public enum ColorDepth {
        UINT8, UINT16
    }

    public class UVDSMetadata {
        public string DatasetPath { get; set; }
        public int OriginalImageWidth { get; set; }
        public int OriginalImageHeight { get; set; }
        public int OriginalNbrSlices { get; set; }
        public ColorDepth ColourDepth { get; set; }
        public Vector3 Scale { get; set; }
        public Vector3 EulerRotation { get; set; }
    }

    public interface IImporter {
        string DatsetPath { get; }
        UVDSMetadata ImportMetadata();
        bool ImportChunk(UInt32 brick_id, int brickSize, Vector3Int dims, out byte[] chunk_data);
        bool ImportChunk(UInt32 brick_id, int brickSize, Vector3Int dims, out UInt16[] chunk_data);
        bool IsMetadataImportable { get; }
    }

    public static class Importer {
        public static float SCALE_DEFAULT = -1.0f;

        public static void ImportMetadata(string dataset_path, ref VolumetricDataset volumetricDataset) {
            string metadata_fp;
            string[] volume_chunks_fps;

            try {
                metadata_fp = Directory.GetFiles(dataset_path, "metadata.txt")[0];
                volume_chunks_fps = Directory.GetFiles(dataset_path, "*.uvds");
                Array.Sort(volume_chunks_fps);
            } catch {
                throw new FileLoadException("Failed to extract UVDS dataset from provided directory");
            }
            using (StreamReader sr = new(metadata_fp)) {
                string line;
                Vector3 scale = new(1, 1, 1);
                Vector3 eulerRotation = new(1, 1, 1);
                while ((line = sr.ReadLine()) != null) {
                    string[] split = line.Split("=");
                    switch (split[0]) {
                        case "originalimagewidth":
                        metadata.OriginalImageWidth = int.Parse(split[1]);
                        break;
                        case "originalimageheight":
                        metadata.OriginalImageHeight = int.Parse(split[1]);
                        break;
                        case "originalnbrslices":
                        metadata.OriginalNbrSlices = int.Parse(split[1]);
                        break;
                        case "colordepth":
                        switch (split[1]) {
                            case "8":
                            metadata.ColourDepth = ColorDepth.UINT8;
                            break;
                            case "16":
                            metadata.ColourDepth = ColorDepth.UINT16;
                            break;
                            default:
                            break;
                        }

                        break;
                        case "voxeldimY":
                        float voxelDimY = float.Parse(split[1]);
                        if (voxelDimY == SCALE_DEFAULT) {

                            scale.y = 1.0f;
                            Debug.LogWarning(
                                "Voxel dimension Y has invalid default value."
                                    + "Default scale 1 is used for the volume along the Y axis. The dimensions of the "
                                    + "volume no longer reflect its dimensions in reality!"
                            );
                            break;
                        }
                        scale.y = (voxelDimY / 1000.0f) * metadata.OriginalImageHeight;
                        break;
                        case "voxeldimZ":
                        float voxelDimZ = float.Parse(split[1]);
                        if (voxelDimZ == SCALE_DEFAULT) {
                            scale.z = 1.0f;
                            Debug.LogWarning(
                                "Voxel dimension Z has invalid default value."
                                    + "Default scale 1 is used for the volume along the Z axis. The dimensions of the "
                                    + "volume no longer reflect its dimensions in reality!"
                            );
                            break;
                        }
                        scale.z = (voxelDimZ / 1000.0f) * metadata.OriginalNbrSlices;
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
                        default:
                        break;
                    }
                }
                metadata.Scale = scale;
                metadata.EulerRotation = eulerRotation;
            }
        }

        /// <summary>
        ///     Imports a single volume brick (i.e., a subset of the 3D volume) from a provided CT dataset.
        /// </summary>
        /// 
        /// <remarks>
        ///     Import will fail if chunk size in bytes exceeds the maximum size for an object allowed
        ///     by .NET (i.e., 2GBs). No size checks are performed in this function.
        /// </remarks>
        /// 
        /// <param name="dataset_path">UVDS root directory path</param>
        /// 
        /// <param name="metadata">UVDS metadata</param>
        /// 
        /// <param name="chunk_id">the id of the chunk to be imported. The file <paramref name="chunk_id"/>.uvds should exist
        /// in the provided resolution level subdirectory of the UVDS path</param>
        /// 
        /// <param name="resolution_lvl">resolution level of the requested chunk. The folder <paramref name="resolution_lvl"/>
        /// should exist as a direct subdirectory of the UVDS path</param>
        /// 
        /// <returns>byte array of the requested chunk. TODO: describe the expected layout</returns>
        /// 
        public static bool ImportChunk(out byte[] data, int chunk_id, int resolution_lvl) {
            var chunk_fp = Path.Combine(dataset_path, $"chunk_{chunk_id}.uvds");
            if (!File.Exists(chunk_fp)) {
                data = null;
                return false;
            }
            data = File.ReadAllBytes(chunk_fp);
            return true;
        }

        public static bool ImportChunk(string dataset_path, ref UInt16[] data, int chunk_id, int resolution_lvl) {
            string chunk_fp = Path.Combine(dataset_path, $"chunk_{chunk_id}.uvds");
            if (!File.Exists(chunk_fp)) {
                return false;
            }
            using (FileStream fs = File.OpenRead(chunk_fp)) {
                int i = 0;
                while (fs.Position < fs.Length) {
                    UInt16 val = (UInt16)((fs.ReadByte() << 8) | fs.ReadByte());
                    data[i] = val;
                    ++i;
                }
            }
            return true;
        }

    }
}
