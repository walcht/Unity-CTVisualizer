using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityCTVisualizer {
    public enum TextureFormat
    {
        MONOCHROME,
        RGB,
        PALETTE,
    }
    public class UVDSImporter {
        public VolumetricDataset Import(string fileName) 
        {
            if (File.Exists(fileName))
            {
                var volumetricDataset = ScriptableObject.CreateInstance<VolumetricDataset>();
                using (var stream = File.Open(fileName, FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        volumetricDataset.ImageWidth = reader.ReadUInt16();
                        volumetricDataset.ImageHeight = reader.ReadUInt16();
                        volumetricDataset.NumberOfSlices = reader.ReadUInt16();
                        volumetricDataset.VoxelDimX = reader.ReadSingle();
                        volumetricDataset.VoxelDimY = reader.ReadSingle();
                        volumetricDataset.VoxelDimZ = reader.ReadSingle();
                        volumetricDataset.TextureFormat = (TextureFormat)reader.ReadUInt16();
                        volumetricDataset.Densities = new List<float>();
                        while (reader.BaseStream.Position < reader.BaseStream.Length) {
                            var density = reader.ReadSingle();
                            volumetricDataset.Densities.Add(density);
                        }

                    }
                }
                return volumetricDataset;
            }
            throw new FileNotFoundException($"{fileName} is not found.");   
        }
    }
}