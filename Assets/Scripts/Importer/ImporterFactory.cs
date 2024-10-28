using System.IO;
using System;
using System.Linq;

namespace UnityCTVisualizer {
    public class ImporterFactory {
        public static IImporter Create(string dataset_path) {
            string[] fps = Directory.EnumerateFiles(dataset_path).OrderBy(
                (string filepath) => Path.GetFileName(filepath)).ToArray();
            if (fps.Length == 0) throw new Exception("dataset is empty");
            string extension = Path.GetExtension(fps[0]);
            if (fps.Any((fp) => !String.Equals(Path.GetExtension(fp), extension))) {
                throw new Exception("non-homogenous dataset directory is provided. " +
                    "All files should have the same type/extension");
            }
            switch (extension) {
                case ".png":
                case ".jpeg":
                case ".jpg":
                case ".tif":
                return new ImageSequenceImporter(dataset_path, fps);
                case ".dicom":
                case ".dcm":
                return new DicomImporter(dataset_path);
                default:
                throw new NotImplementedException($"importer for the extension: {extension} is not yet supported");
            }
        }
    }
}
