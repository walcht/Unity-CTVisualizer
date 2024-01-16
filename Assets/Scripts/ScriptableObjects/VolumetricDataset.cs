using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UnityCTVisualizer {
    public class VolumetricDataset: ScriptableObject
    {
        public ushort ImageWidth { get; set; }
        public ushort ImageHeight { get; set; }
        public ushort NumberOfSlices { get; set; }
        public float VoxelDimX { get; set; }
        public float VoxelDimY { get; set; }
        public float VoxelDimZ { get; set; }
        public TextureFormat TextureFormat { get; set; }
        public List<float> Densities { get; set; }
    }
}