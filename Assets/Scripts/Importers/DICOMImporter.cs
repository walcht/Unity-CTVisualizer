using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Device;
using UnityIA;

public class DICOMImporter : IImporter
{
    public VolumetricDataset Import(string fp) {
        var volumeDataset =  ScriptableObject.CreateInstance<VolumetricDataset>();
        // use fo-dicom to read the .dcm fp and extract relevant tags and actual
        // image data
        volumeDataset.width = 0;
        volumeDataset.height = 0;
        volumeDataset.depth = 0;
        volumeDataset.FlattenedImageData = null;  // later
        return volumeDataset;
    }

    private void InternalImport() {

    }
}
