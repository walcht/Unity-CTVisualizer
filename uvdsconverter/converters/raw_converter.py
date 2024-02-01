from converter import BaseConverter, UVDS
import os
import numpy as np
import click


class RawConverter(BaseConverter):
    def convert_dataset(self, dataset_dir_or_fp: str) -> UVDS:
        """Imports and converts RAW (.raw extension) dataset to Unity Volumetric DataSet (UVDS) format.
        RAW files do not contain volume attributes in them such as width, height or depth, so when running
        the uvds.py script, command line prompts will ask the user for these values.

        Parameters
        ----------
        dataset_dir_or_fp : str
            absolute path to a dataset directory or a single file

        Returns
        -------
        UVDS
            Instance of a UVDS dataclass containing converted import data attributes
        """
        if os.path.isfile(dataset_dir_or_fp):
            endianness_options = ["L", "B"]
            data_type_options = {
                "uint8": np.uint8,
                "int8": np.int8,
                "uint16": np.uint16,
                "int16": np.int16,
                "uint32": np.uint32,
                "int32": np.int32,
            }
            width = click.prompt("Enter width", type=int)
            height = click.prompt("Enter height", type=int)
            depth = click.prompt("Enter depth", type=int)
            voxeldimX = click.prompt("Enter voxel dimension x", type=float)
            voxeldimY = click.prompt("Enter voxel dimension y", type=float)
            voxeldimZ = click.prompt("Enter voxel dimension z", type=float)
            endianness = click.prompt(
                "Enter endianness (Little-endian - L, Big-endian - B)",
                type=click.Choice(endianness_options),
                default="L",
            )
            num_of_bytes_to_skip = click.prompt(
                "Enter number of bytes to skip (header size)", type=int, default=0
            )
            data_type_str = click.prompt(
                "Enter data type", type=click.Choice(list(data_type_options.keys())), default="uint8"
            )
            dtype = data_type_options[data_type_str]
            if endianness == "B":
                dtype = np.dtype(dtype).newbyteorder(">")
            num_elements = width * height * depth
            raw_data = np.empty(num_elements, dtype=dtype)
            with open(dataset_dir_or_fp, "rb") as file:
                file.seek(num_of_bytes_to_skip)
                actual_file_size = os.path.getsize(dataset_dir_or_fp)
                expected_file_size = num_elements * np.dtype(dtype).itemsize + num_of_bytes_to_skip
                if expected_file_size > actual_file_size:
                    raise ValueError(
                        f"Expected file size ({expected_file_size} bytes) is greater than "
                        + "actual file size ({actual_file_size} bytes)."
                    )
                raw_data = np.fromfile(file, dtype=dtype, count=num_elements)
            minDensity: np.float16 = raw_data.min()
            maxDensity: np.float16 = raw_data.max()
            return UVDS(
                imagewidth=np.uint16(width),
                imageheight=np.uint16(height),
                nbrslices=np.uint16(depth),
                voxeldimX=np.float32(voxeldimX),
                voxeldimY=np.float32(voxeldimY),
                voxeldimZ=np.float32(voxeldimZ),
                minDensity=minDensity,
                maxDensity=maxDensity,
                densities=raw_data,
            )
        raise FileNotFoundError(f"{dataset_dir_or_fp} is not a valid filepath.")

    @staticmethod
    def is_this_converter_suitable(dataset_dir_or_fp: str) -> bool:
        """Checks whether this raw dataset converter is suitable for the provided CT (or MRI) dataset

        Parameters
        ----------
        dataset_fp : str
            absolute path to a dataset directory or a single file

        Returns
        -------
        bool
            True if this raw dataset converter is suitable. False otherwise
        """
        return os.path.isfile(dataset_dir_or_fp) and os.path.splitext(dataset_dir_or_fp)[-1].lower() == ".raw"


if __name__ == "__main__":
    dicom_converter = RawConverter()
    result = dicom_converter.convert_dataset(
        os.path.join(os.path.dirname(__file__), "test_datasets", "raw", "Bonsai1.512x512x182.raw")
    )
    print("done")
