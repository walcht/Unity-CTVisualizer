from typing import List
import pydicom
import os
import glob
import re
import numpy as np
from dataclasses import dataclass
from numpy.typing import NDArray
import logging
import argparse

texture_format_mapping = {
    "MONOCHROME": 0,  # single channel
    "RGB": 1,  # triple channel - NOT SUPPORTED
    "PALETTE": 2,  # look-up table - NOT SUPPORTED
}


@dataclass
class UVDS:
    imagewidth: np.uint16
    imageheight: np.uint16
    nbrslices: np.uint16
    voxeldimX: np.float32
    voxeldimY: np.float32
    voxeldimZ: np.float32
    minDensity: np.int32
    maxDensity: np.int32
    densities: NDArray[np.float32]


def get_voxel_depth(sliceLocations: List[float]) -> float:
    unique_vals = set([abs(sliceLocations[i] - sliceLocations[i - 1]) for i in range(1, len(sliceLocations))])
    if len(unique_vals) > 1:
        logging.warning(f"unique voxel depth values (calculated from slice locations): {unique_vals}")
    return sum(unique_vals) / len(unique_vals)


def write_udvs_binary(uvds_data: UVDS, uvds_fp: str) -> None:
    # properly serialize output according to UDVS format
    logging.info(f"writing binary UVDS data to: {uvds_fp} ...")
    with open(uvds_fp, "ab") as output:
        output.write(uvds_data.imagewidth.tobytes())
        output.write(uvds_data.imageheight.tobytes())
        output.write(uvds_data.nbrslices.tobytes())
        output.write(uvds_data.voxeldimX.tobytes())
        output.write(uvds_data.voxeldimY.tobytes())
        output.write(uvds_data.voxeldimZ.tobytes())
        output.write(uvds_data.minDensity.tobytes())
        output.write(uvds_data.maxDensity.tobytes())
        output.write(uvds_data.densities.tobytes())
    logging.info(f"UVDS binary available at: {uvds_fp}")


def main(dirOrfilePath: str, uvds_fp: str) -> None:
    uvds_fp_wext = re.sub(r"\..*", ".uvds", uvds_fp)
    if os.path.isfile(uvds_fp_wext):
        raise FileExistsError(f"provided uvds output path: {uvds_fp_wext} already exists.")
    if os.path.isdir(dirOrfilePath):  # a directory of .dcm files was provided
        files: List[pydicom.FileDataset] = []
        ordered_files_list = glob.glob(os.path.join(dirOrfilePath, "*.dcm"), recursive=False)
        ordered_files_list.sort()
        logging.info(f"reading {len(ordered_files_list)} DICOM files ...")
        for fname in ordered_files_list:
            files.append(pydicom.dcmread(fname, force=False))
        width = files[0].Columns
        height = files[0].Rows
        nbrslices = len(files)
        voxel_depth = get_voxel_depth([f.SliceLocation.real for f in files])
        texture_format = texture_format_mapping[re.sub(r"[0-9]", "", files[0].PhotometricInterpretation)]
        # data has to be converted to densities
        data = np.empty(nbrslices * width * height, dtype=np.float32)
        for idx_slice in range(nbrslices):
            logging.info(
                f"converting slice number: {idx_slice + 1:4} of file: "
                + f"{os.path.basename(files[idx_slice].filename)}"  # type: ignore
            )
            flattened_array = files[idx_slice].pixel_array.flatten() * np.float32(
                files[idx_slice].RescaleSlope
            ) + np.float32(files[idx_slice].RescaleIntercept)
            data[idx_slice * width * height : (idx_slice + 1) * width * height] = flattened_array
        minDensity = data.min()
        maxDensity = data.max()
        # convert into [0.00, 1.00] range
        data = data - minDensity / (maxDensity - minDensity)
        uvds_data = UVDS(
            imagewidth=np.uint16(width),
            imageheight=np.uint16(height),
            nbrslices=np.uint16(nbrslices),
            voxeldimX=np.float32(files[0].PixelSpacing[0]),
            voxeldimY=np.float32(files[0].PixelSpacing[1]),
            voxeldimZ=np.float32(voxel_depth),
            minDensity=np.int32(minDensity),
            maxDensity=np.int32(maxDensity),
            densities=data,
        )
        write_udvs_binary(uvds_data, uvds_fp_wext)
        return
    if os.path.isfile(dirOrfilePath):  # a path to a single .dcm file was provided
        logging.info("single DICOM file was provided. Volumetric visualization is therefore not possible.")
        dataset: pydicom.FileDataset = pydicom.dcmread(dirOrfilePath)
        texture_format = texture_format_mapping[re.sub(r"[0-9]", "", dataset.PhotometricInterpretation)]
        data = dataset.pixel_array.flatten() * np.float32(dataset.RescaleSlope) + np.float32(
            dataset.RescaleIntercept
        )
        minDensity = data.min()
        maxDensity = data.max()
        # convert into [0.00, 1.00] range
        data = data - minDensity / (maxDensity - minDensity)
        uvds_data = UVDS(
            imagewidth=np.uint16(dataset.Columns),
            imageheight=np.uint16(dataset.Rows),
            nbrslices=np.uint16(1),
            voxeldimX=np.float32(dataset.PixelSpacing[0]),
            voxeldimY=np.float32(dataset.PixelSpacing[1]),
            voxeldimZ=np.float32(0.0),
            minDensity=np.int32(minDensity),
            maxDensity=np.int32(maxDensity),
            densities=data,
        )
        write_udvs_binary(uvds_data, uvds_fp_wext)
        return
    raise FileNotFoundError(f"{dirOrfilePath} is neither a valid directory path nor a valid filepath.")


if __name__ == "__main__":
    logging.basicConfig(format="%(msecs)dms [%(levelname)s]: %(message)s", level=logging.INFO)
    main(
        "/home/walidchtioui/ct_datasets/example_00",
        "/home/walidchtioui/Projects/Unity-Immersive-Analytics/Datasets/test_02.uvds",
    )
