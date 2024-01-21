from converters.converter import BaseConverter, UVDS
from typing import List
import os
import glob
import logging
import pydicom
import numpy as np
from numpy.typing import NDArray


class DicomConverter(BaseConverter):
    def __get_voxel_depth(self, sliceLocations: NDArray[np.float_]) -> float:
        u_slices, u_slices_count = np.unique(sliceLocations, return_counts=True)
        if len(u_slices) > 1:
            logging.warning(
                f"more than one unique voxel depth value (calculated from slice locations): {u_slices}"
            )
            logging.info("returning voxel depth with highest frequency")
        return u_slices[np.argmax(u_slices_count)]

    def import_dataset(self, dataset_dir_or_fp: str) -> UVDS:
        if os.path.isdir(dataset_dir_or_fp):  # a directory of .dcm files was provided
            files: List[pydicom.FileDataset] = []
            ordered_files_list = glob.glob(os.path.join(dataset_dir_or_fp, "*.dcm"), recursive=False)
            ordered_files_list.sort()
            logging.info(f"reading {len(ordered_files_list)} DICOM files ...")
            for fname in ordered_files_list:
                files.append(pydicom.dcmread(fname, force=False))
            width = files[0].Columns
            height = files[0].Rows
            nbrslices = len(files)
            voxel_depth = self.__get_voxel_depth(np.array([f.SliceLocation.real for f in files]))
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
            return UVDS(
                imagewidth=np.uint16(width),
                imageheight=np.uint16(height),
                nbrslices=np.uint16(nbrslices),
                voxeldimX=np.float32(files[0].PixelSpacing[0]),
                voxeldimY=np.float32(files[0].PixelSpacing[1]),
                voxeldimZ=np.float32(voxel_depth),
                minDensity=np.float32(minDensity),
                maxDensity=np.float32(maxDensity),
                densities=data,
            )
        if os.path.isfile(dataset_dir_or_fp):  # a path to a single .dcm file was provided
            logging.info(
                "single DICOM file was provided. Volumetric visualization is therefore not possible."
            )
            dataset: pydicom.FileDataset = pydicom.dcmread(dataset_dir_or_fp)
            data = dataset.pixel_array.flatten() * np.float32(dataset.RescaleSlope) + np.float32(
                dataset.RescaleIntercept
            )
            minDensity = data.min()
            maxDensity = data.max()
            return UVDS(
                imagewidth=np.uint16(dataset.Columns),
                imageheight=np.uint16(dataset.Rows),
                nbrslices=np.uint16(1),
                voxeldimX=np.float32(dataset.PixelSpacing[0]),
                voxeldimY=np.float32(dataset.PixelSpacing[1]),
                voxeldimZ=np.float32(0.0),
                minDensity=np.float32(minDensity),
                maxDensity=np.float32(maxDensity),
                densities=data,
            )
        raise FileNotFoundError(
            f"{dataset_dir_or_fp} is neither a valid directory path nor a valid filepath."
        )

    def is_this_importer_suitable(self, dataset_dir_or_fp: str) -> bool:
        if os.path.isdir(dataset_dir_or_fp):
            dcm_files = glob.glob(os.path.join(dataset_dir_or_fp, "*.dcm"), recursive=False)
            return len(dcm_files) > 0
        if os.path.isfile(dataset_dir_or_fp):
            return os.path.basename(dataset_dir_or_fp).endswith(".dcm")
        raise FileNotFoundError(
            f"{dataset_dir_or_fp} is neither a valid directory path nor a valid filepath."
        )
