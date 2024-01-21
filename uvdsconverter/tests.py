import unittest
import tempfile
from uvds import import_dataset, write_udvs_binary_stream


class TestDicomImport(unittest.TestCase):
    def test_write(self) -> None:
        self.test_dataset_fp = os.path.join(os.path.dirname(__file__), "/test_datasets", "dicom_dataset")
        dicom_ds = import_dataset(self.test_dataset_fp)
        with tempfile.TemporaryFile(mode="w+b") as tmp_uvds_stream:
            write_udvs_binary_stream(dicom_ds, tmp_uvds_stream)
            imported_uvds_ds = import_dataset(self.test_dataset_fp)
            self.assertEqual(dicom_ds, imported_uvds_ds)


if __name__ == "__main__":
    unittest.main()
