from converter import BaseConverter, UVDS
from typing import List
import os
import glob
import logging
import pydicom
import numpy as np


class DicomConverter(BaseConverter):
    def __get_voxel_depth(self, sortedDatasets: List[pydicom.FileDataset]) -> float:
        u_slice_thickness, u_slice_thickness_counts = np.unique(
            np.array([ds.SliceThickness for ds in sortedDatasets]), return_counts=True
        )
        if len(u_slice_thickness) > 1:
            logging.warning(
                f"more than one unique slice thikness (or voxel depth) were encountered: {u_slice_thickness}"
            )
            logging.info("returning averaged voxel depth with highest frequency")
        return u_slice_thickness[np.argmax(u_slice_thickness_counts)]

    def convert_dataset(self, dataset_dir_or_fp: str) -> UVDS:
        if os.path.isdir(dataset_dir_or_fp):  # a directory of .dcm files was provided
            files: List[pydicom.FileDataset] = []
            ordered_files_list = glob.glob(os.path.join(dataset_dir_or_fp, "*.dcm"), recursive=False)
            logging.info(f"reading {len(ordered_files_list)} DICOM files ...")
            for fname in ordered_files_list:
                files.append(pydicom.dcmread(fname, force=False))
            files.sort(key=lambda ds: ds.SliceLocation)
            width = files[0].Columns
            height = files[0].Rows
            nbrslices = len(files)
            voxel_depth = self.__get_voxel_depth(files)
            # data has to be converted to densities
            data = np.empty(nbrslices * width * height, dtype=np.float16)
            for idx_slice in range(nbrslices):
                logging.info(
                    f"converting slice number: {idx_slice + 1:4} of file: "
                    + f"{os.path.basename(files[idx_slice].filename)}"  # type: ignore
                )
                flattened_array = files[idx_slice].pixel_array.flatten() * np.float16(
                    files[idx_slice].RescaleSlope
                ) + np.float16(files[idx_slice].RescaleIntercept)
                data[idx_slice * width * height : (idx_slice + 1) * width * height] = flattened_array
            minDensity: np.float16 = data.min()
            maxDensity: np.float16 = data.max()
            return UVDS(
                imagewidth=np.uint16(width),
                imageheight=np.uint16(height),
                nbrslices=np.uint16(nbrslices),
                voxeldimX=np.float32(files[0].PixelSpacing[0]),
                voxeldimY=np.float32(files[0].PixelSpacing[1]),
                voxeldimZ=np.float32(voxel_depth),
                minDensity=minDensity,
                maxDensity=maxDensity,
                densities=data,
            )
        raise NotADirectoryError(f"{dataset_dir_or_fp} is not a valid directory path.")

    @staticmethod
    def is_this_converter_suitable(dataset_dir_or_fp: str) -> bool:
        if os.path.isdir(dataset_dir_or_fp):
            dcm_files = glob.glob(os.path.join(dataset_dir_or_fp, "*.dcm"), recursive=False)
            return len(dcm_files) > 0
        return False


if __name__ == "__main__":
    dicom_converter = DicomConverter()
    result = dicom_converter.convert_dataset(r"/home/walidchtioui/ct_datasets/example02")
    print("done")
