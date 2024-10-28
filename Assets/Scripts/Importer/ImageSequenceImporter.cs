using System;
using UnityEngine;
using System.IO;
using UnityEngine.Assertions;

namespace UnityCTVisualizer {
    public class ImageSequenceImporter : IImporter {

        private string m_DatasetPath;
        private string[] m_Filepaths;

        public ImageSequenceImporter(string datasetPath, string[] orderedFilepaths) {
            m_DatasetPath = datasetPath;
            m_Filepaths = orderedFilepaths;
        }

        private Vector3Int GetNbrOfChunks(Vector3Int dims, int brickSize) {
            return new Vector3Int(
                Mathf.CeilToInt(dims.x / (float)brickSize),
                Mathf.CeilToInt(dims.y / (float)brickSize),
                Mathf.CeilToInt(dims.z / (float)brickSize)
            );
        }

        private int GetBrickIndex(UInt32 brick_id) {
            return (int)(brick_id & 0x03FFFFFF);
        }
        private int GetBrickResolutionLevel(UInt32 brick_id) {
            return (int)(brick_id >> 26);
        }

        public string DatsetPath { get => m_DatasetPath; }
        public UVDSMetadata ImportMetadata() {
            throw new NotImplementedException("this importer does not support importing of metadata. "
                + "Check IsMetadataImportable before calling this.");
        }

        public bool ImportChunk(UInt32 brick_id, int brickSize, Vector3Int dims, out byte[] data) {
            Vector3Int nbr_chunks = GetNbrOfChunks(dims, brickSize);
            int brick_idx = GetBrickIndex(brick_id);
            int resolution_lvl = GetBrickResolutionLevel(brick_id);
            if (resolution_lvl > 0) {
                throw new NotImplementedException("lower resolution levels are not yet implemented");
            }

            int pad_right = brickSize * nbr_chunks.x - dims.x;
            int pad_top = brickSize * nbr_chunks.y - dims.y;

            int slice_start_idx = (nbr_chunks.z - 1 - brick_idx / (nbr_chunks.x * nbr_chunks.y)) * brickSize;
            int col_start = ((brick_idx % (nbr_chunks.x * nbr_chunks.y)) % nbr_chunks.x) * brickSize;
            int row_start = (nbr_chunks.y - 1 - (brick_idx % (nbr_chunks.x * nbr_chunks.y)) / nbr_chunks.y) * brickSize;

            // in C# this array is initialized with 0s
            data = new byte[(int)Math.Pow(brickSize, 3)];
            int buf_offset = 0;
            for (int i = 0; i < brickSize; ++i) {
                int slice_idx = slice_start_idx - i;
                // check if this is a padding slice (slice filled with 0s)
                if (slice_idx < dims.z) {
                    // load current slice
                    var mat = new OpenCvSharp.Mat(m_Filepaths[slice_idx], OpenCvSharp.ImreadModes.Grayscale);
                    // add padding (if necessary)
                    var padded_mat = mat.CopyMakeBorder(pad_top, 0, 0, pad_right, OpenCvSharp.BorderTypes.Constant, OpenCvSharp.Scalar.All(0));
                    padded_mat.SubMat(row_start, row_start + 4, col_start, col_start + 4).GetArray<byte>(out byte[] brick_slide_data);
                    Assert.IsTrue(brick_slide_data.Length == (brickSize * brickSize));
                    Array.Copy(brick_slide_data, 0, data, buf_offset, brick_slide_data.Length);
                    buf_offset += brickSize * brickSize;
                }
            }
            return true;
        }
        public bool ImportChunk(UInt32 brick_id, int brickSize, Vector3Int dims, out UInt16[] data) {
        }

        public bool IsMetadataImportable { get => false; }

    }
}
