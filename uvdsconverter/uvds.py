from typing import List
import pydicom
import os
import glob
import re
import numpy as np
from dataclasses import dataclass
from numpy.typing import NDArray
import struct
import math
import logging

texture_format_mapping = {
    "MONOCHROME": 0,  # single channel
    "RGB": 1,  # triple channel - NOT SUPPORTED
    "PALETTE": 2,  # look-up table - NOT SUPPORTED
}


@dataclass
class UVDS:
    imagewidth: int
    imageheight: int
    nbrslices: int
    voxeldimX: float
    voxeldimY: float
    voxeldimZ: float
    textureformat: int
    densities: List[float]


def get_voxel_depth(sliceLocations: List[float]) -> float:
    unique_vals = set([abs(sliceLocations[i] - sliceLocations[i - 1]) for i in range(1, len(sliceLocations))])
    if len(unique_vals) > 1:
        logging.warning(f"unique voxel depth values (calculated from slice locations): {unique_vals}")
    return sum(unique_vals) / len(unique_vals)


def write_udvs_binary(uvds_data: UVDS, uvds_fp: str) -> None:
    # properly serialize output according to UDVS format
    logging.info(f"writing binary UVDS data to: {uvds_fp} ...")
    with open(uvds_fp, "wb") as output:
        output.write(struct.pack("HHH", uvds_data.imagewidth, uvds_data.imageheight, uvds_data.nbrslices))
        output.write(struct.pack("fff", uvds_data.voxeldimX, uvds_data.voxeldimY, uvds_data.voxeldimZ))
        output.write(struct.pack("H", uvds_data.textureformat))
        for i in range(len(uvds_data.densities)):
            output.write(struct.pack("f", uvds_data.densities[i]))
    logging.info(f"UVDS binary available at: {uvds_fp}")


def main(dirOrfilePath: str, uvds_fp: str) -> None:
    uvds_fp_wext = re.sub(r"\..*", ".uvds", uvds_fp)
    if os.path.isfile(uvds_fp_wext):
        raise FileExistsError(f"provided uvds output path: {uvds_fp_wext} already exists.")
    # a directory of .dcm files was provided
    if os.path.isdir(dirOrfilePath):
        files: List[pydicom.FileDataset] = []
        ordered_files_list = glob.glob(os.path.join(dirOrfilePath, "*.dcm"), recursive=False)
        ordered_files_list.sort()
        for fname in ordered_files_list:
            files.append(pydicom.dcmread(fname, force=False))
        width = files[0].Columns
        height = files[0].Rows
        nbrslices = len(files)
        voxel_depth = get_voxel_depth([f.SliceLocation.real for f in files])
        texture_format = texture_format_mapping[re.sub(r"[0-9]", "", files[0].PhotometricInterpretation)]
        # data has to be converted to densities
        data = []
        for idx_slice in range(nbrslices):
            logging.info(f"converting slice number: {idx_slice + 1}\t of file: {files[idx_slice].filename}")
            for idx_row in range(height):
                for idx_col in range(width):
                    # idx_pixel_in_slice = (idx_row * width) + idx_col
                    # idx_pixel_in_data = (idx_slice * width * height) + idx_pixel_in_slice
                    pixel_in_hounsfield_unit = (
                        files[idx_slice].pixel_array[idx_row, idx_col] * files[idx_slice].RescaleSlope
                        + files[idx_slice].RescaleIntercept
                    )
                    data.append(pixel_in_hounsfield_unit)
        uvds_data = UVDS(
            imagewidth=width,
            imageheight=height,
            nbrslices=nbrslices,
            voxeldimX=files[0].PixelSpacing[0].real,
            voxeldimY=files[0].PixelSpacing[1].real,
            voxeldimZ=voxel_depth,
            textureformat=texture_format,
            densities=data,
        )
        write_udvs_binary(uvds_data, uvds_fp_wext)
        return
    # a path to a .dcm file was provided
    if os.path.isfile(dirOrfilePath):
        dataset: pydicom.FileDataset = pydicom.dcmread(dirOrfilePath)
        # pixel data can be float, double float or simply bytes
        data = dataset.pixel_array
        # convert pixel data to HU (mater density units)
        pass
        uvds_data = UVDS(
            imagewidth=0,
            imageheight=0,
            nbrslices=1,
            voxeldimX=0.0,
            voxeldimY=0.0,
            voxeldimZ=0,
            textureformat=0,
            densities=[],
        )
        write_udvs_binary(uvds_data, uvds_fp_wext)
        return
    raise FileNotFoundError(f"{dirOrfilePath} is neither a valid directory path nor a valid filepath.")


if __name__ == "__main__":
    logging.root.setLevel(logging.INFO)
    main(
        "/home/walidchtioui/ct_datasets/example02",
        "/home/walidchtioui/Projects/Unity-Immersive-Analytics/Datasets/test.uvds",
    )
