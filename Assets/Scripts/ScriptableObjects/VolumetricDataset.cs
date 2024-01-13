using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEditor;

namespace UnityIA {
    public class VolumetricDataset: ScriptableObject
    {
        Color[] _flattenedImagesData;
        public int width;
        public int height;
        public int depth;
        TextureFormat format;
        Texture3D _dataTexture = null;

        public Texture3D GetDataTexture {
            get => _dataTexture;
        }

        public void SaveTexture(string assetPath) {
            if (_dataTexture != null)
                AssetDatabase.CreateAsset(_dataTexture, assetPath);
        }

        public Color[] FlattenedImageData {
            set {
                _flattenedImagesData = value;
                _dataTexture = new Texture3D(width, height, depth, format, mipChain: false, createUninitialized: true);
                _dataTexture.SetPixels(_flattenedImagesData, 0);
                _dataTexture.wrapMode = TextureWrapMode.Clamp;
            }
            private get => _flattenedImagesData;
        }
    }
}