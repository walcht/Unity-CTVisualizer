using System;
using UnityCTVisualizer;

public class DicomImporter : IImporter
{
    public DicomImporter(string datasetPath) {
        m_DatasetPath = datasetPath;
    }

    private string m_DatasetPath;
    public string DatsetPath { get => m_DatasetPath; }
    public UVDSMetadata ImportMetadata() {
        return null;
    }

    public bool ImportChunk(UInt32 chunk_id, int brickSize, out byte[] data) {
        return true;
    }

    public bool ImportChunk(UInt32 chunk_id, int brickSize, out UInt16[] data) {
        data = null;
        return true;
    }
    public bool IsMetadataImportable { get => true; }
}
