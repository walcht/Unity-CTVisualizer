import unittest
import tempfile
import os
from converters import Converter, DicomConverter


# TODO: implement DICOM converter tester
class TestDicomImport(unittest.TestCase):
    def test_write(self) -> None:
        test_dataset_fp = os.path.join(os.path.dirname(__file__), "/datasets", "dicom")
        uvds_converter = Converter.factory(test_dataset_fp)
        self.assertEqual(type(uvds_converter), DicomConverter)
        dicom_ds = uvds_converter.convert_dataset(test_dataset_fp)
        with tempfile.TemporaryFile(mode="w+b") as tmp_uvds_stream:
            pass


if __name__ == "__main__":
    unittest.main()
