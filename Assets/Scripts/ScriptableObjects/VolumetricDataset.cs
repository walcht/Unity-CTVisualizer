using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace UnityCTVisualizer {
    [CreateAssetMenu(fileName = "volumetric_dataset", menuName = "UnityCTVisualizer/VolumetricDataset")]
    // [PreferBinarySerialization]
    public class VolumetricDataset: ScriptableObject
    {
        // from https://docs.unity3d.com/Manual/class-Texture3D.html
        const ushort MAX_TEXTURE3D_DIM = 2048;
        
        [SerializeField, Tooltip("UVDS dataset absolute path. Call LoadUVDS after setting this to properly load the dataset.")]
        string _datasetPath;
        public string DatasetPath {
            get => _datasetPath;
            set {
                if (!File.Exists(value)) {
                    Debug.LogError("Non exsitant filepath to a UVDS dataset was provided. Aborting ...");
                    return;
                }
                _datasetPath = value;
                LoadUVDS(_datasetPath);
            }
        }

        ushort imageWidth;
        ushort imageHeight;
        ushort nbrSlices;
        float voxelDimX;
        float voxelDimY;
        float voxelDimZ;
        TextureFormat textureFormat;
        int minDensity;
        int maxDensity;
        Texture3D volumetricTex;

        // values calculated from input dataset
        int dimension;

        private void LoadUVDS(string uvdsFilePath) {
            using (var stream = File.Open(uvdsFilePath, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream))
                {
                    imageWidth = reader.ReadUInt16();
                    imageHeight = reader.ReadUInt16();
                    nbrSlices = reader.ReadUInt16();

                    if (imageWidth > MAX_TEXTURE3D_DIM || imageHeight > MAX_TEXTURE3D_DIM ||
                            nbrSlices > MAX_TEXTURE3D_DIM) {
                        Debug.LogError($"Exceeding maximum Texture3D dimension: {MAX_TEXTURE3D_DIM}. Aborting ...");
                        return;
                    }

                    voxelDimX = reader.ReadSingle();
                    voxelDimY = reader.ReadSingle();
                    voxelDimZ = reader.ReadSingle();
                    textureFormat = (TextureFormat)reader.ReadUInt16();
                    minDensity = reader.ReadInt32();
                    maxDensity = reader.ReadInt32();
                    
                    volumetricTex = new Texture3D(imageWidth, imageHeight, nbrSlices, textureFormat, false);
                    volumetricTex.wrapMode = TextureWrapMode.Clamp;
                    
                    int i;
                    float[] densities = new float[imageWidth * imageHeight * nbrSlices];
                    // the data provided to Texture3D instance depends on the provided TextureFormat
                    switch (textureFormat)
                    {
                        case TextureFormat.RFloat:
                            for (i = 0; reader.BaseStream.Position != reader.BaseStream.Length; ++i) {
                                densities[i] = reader.ReadSingle();
                            }
                            break;
                        default:
                            Debug.LogError($"TextureFormat {textureFormat} is currently not supported. Aborting ...");
                            return;
                    }
                    if (i != imageHeight) {
                        Debug.LogError("Less elements in provided UVDS dataset than expected. Aborting ...");
                        return;
                    }

                    volumetricTex.SetPixelData(densities, 0);
                    volumetricTex.Apply();

                }
            }
        }

    }
}
