from converter import BaseConverter  # type: ignore
from dataclasses import dataclass
import os
from PIL import Image
import glob
import logging
import numpy as np
from numpy.typing import NDArray
import click
from typing import Literal
from tqdm import tqdm # type: ignore
import lz4.frame  # type: ignore


@dataclass
class Metadata:
    width: int
    height: int
    nbrslices: int
    colordepth: Literal[8, 16]


class ImagesConverter(BaseConverter):
    """"""

    supported_formats: dict[str, str] = {"TIFF": ".tif", "PNG": ".png"}

    def __init__(self) -> None:
        super().__init__()
        self.sorted_image_fps: list[str]

    def get_sorted_images_from_dir(
        self, dataset_dir: str, verbose: bool = True
    ) -> list[str]:
        for supported_format, ext in self.supported_formats.items():
            fps = [
                fp
                for fp in glob.glob(
                    os.path.join(dataset_dir, f"*{ext}"), recursive=False
                )
            ]
            if fps:
                if verbose:
                    print(f"found: {len(fps)} {supported_format.upper()} image files")
                # order according to filename - very important!
                return sorted(fps)
        if verbose:
            print("could not find any supported image files")
        return []

    def extract_metadata(self, sorted_image_fps: list[str]) -> Metadata:
        openedImg = Image.open(sorted_image_fps[0])
        return Metadata(
            width=openedImg.width,
            height=openedImg.height,
            nbrslices=len(sorted_image_fps),
            colordepth=8 if openedImg.mode == "P" else 16,
        )

    def import_dataset(
        self,
        dataset_dir: str,
        bricksize: int = 128,
        resolution_levels: int = 0,
        compress: bool = True,
    ) -> None:
        """Imports and converts a sequence of images to Unity Volumetric DataSet (UVDS) format.
        Image files do not contain volume attributes such as width, height or depth therefore
        command line prompt the user for these values.

        Supported image file formats are:
            - TIFF (.tif)
            - PNG (.png)

        Parameters
        ----------
        dataset_dir : str
            absolute path to a dataset directory
        """
        if not os.path.isdir(dataset_dir):
            raise NotADirectoryError(
                f"provided images path is not a directory path: ${dataset_dir}"
            )
        self.sorted_image_fps = self.get_sorted_images_from_dir(dataset_dir)
        if not self.sorted_image_fps:
            raise RuntimeError(
                "no images of any supported formats is found in the provided directory"
            )
        metadata = self.extract_metadata(self.sorted_image_fps)
        self.originalimagewidth = metadata.width
        self.originalimageheight = metadata.height
        self.originalnbrslices = metadata.nbrslices
        self.colordepth = metadata.colordepth
        self.bricksize = bricksize
        self.nbrbricksX, self.nbrbricksY, self.nbrbricksZ = self.get_nbrbricks(
            metadata.width, metadata.height, metadata.nbrslices, bricksize
        )
        self.totalnbrbricks = self.nbrbricksX * self.nbrbricksY * self.nbrbricksZ
        self.resolutionlevels = resolution_levels
        self.lz4compressed = compress
        self.voxeldimX = click.prompt(
            "Enter voxel dimension x: ", type=float, default=1.0
        )
        self.eulerrotY = click.prompt(
            "Enter voxel dimension y: ", type=float, default=1.0
        )
        self.voxeldimZ = click.prompt(
            "Enter voxel dimension z: ", type=float, default=1.0
        )
        self.eulerrotX = click.prompt(
            "Enter Euler rotation around X axis: ", type=float, default=0.0
        )
        self.eulerrotY = click.prompt(
            "Enter Euler rotation around Y axis: ", type=float, default=0.0
        )
        self.eulerrotZ = click.prompt(
            "Enter Euler rotation around Z axis: ", type=float, default=0.0
        )

    def write_binary_chuncks(
        self, directory_path: str, basename: str = "chunck"
    ) -> None:
        """Write UVDS binary chuncks data to a provided directory path"""
        # read one image at a time
        if self.sorted_image_fps is None:
            raise RuntimeError("")  # TODO
        if not len(self.sorted_image_fps):
            raise RuntimeError("")  # TODO
        # padding
        pad_width = self.nbrbricksX * self.bricksize - self.originalimagewidth
        pad_height = self.nbrbricksY * self.bricksize - self.originalimageheight
        pad_slices = self.nbrbricksZ * self.bricksize - self.originalnbrslices
        pad_cols_before = pad_width // 2 + pad_width % 2
        pad_cols_after = pad_width // 2
        pad_rows_before = pad_height // 2 + pad_height % 2
        pad_rows_after = pad_height // 2
        pad_slices_before = pad_slices // 2 + pad_slices % 2
        nbrslices = self.nbrbricksZ * self.bricksize
        # for each image slice we write to nbrbricksX * nbrbricksY chuncks this way only
        # width * height pixels have to reside in memory at a given time
        for slice_idx in tqdm(range(nbrslices)):
            data: NDArray[np.uint8 | np.uint16]
            # check if slice corresponds to a padding slice
            if (
                slice_idx < pad_slices_before
                or (slice_idx - pad_slices_before) >= self.originalnbrslices
            ):
                data = np.zeros(
                    (
                        self.nbrbricksY * self.bricksize,
                        self.nbrbricksX * self.bricksize,
                    ),
                    dtype=np.uint8 if self.colordepth == 8 else np.uint16,
                )
            else:
                with Image.open(
                    self.sorted_image_fps[slice_idx - pad_slices_before]
                ) as im:
                    # row-major from top-left of the image
                    data = np.asarray(im)
                    data = np.pad(
                        data,
                        pad_width=(
                            (pad_rows_before, pad_rows_after),
                            (pad_cols_before, pad_cols_after),
                        ),
                        mode="constant",
                        constant_values=(0,),
                    )
            for i in range(self.nbrbricksX * self.nbrbricksY):
                with lz4.frame.open(
                    os.path.join(
                        directory_path,
                        f"{basename}_{i + (slice_idx // self.bricksize) * self.nbrbricksX * self.nbrbricksY}.uvds" +
                            f"{".lz4" if self.lz4compressed else ""}",
                    ),
                    mode="ab",
                    compression_level=lz4.frame.COMPRESSIONLEVEL_MAX
                ) as bs:  # relax - it's Binary Stream
                    row_start_idx = self.bricksize * (i // self.nbrbricksX)
                    row_end_idx = row_start_idx + self.bricksize
                    col_start_idx = self.bricksize * (i % self.nbrbricksX)
                    col_end_idx = col_start_idx + self.bricksize
                    bs.write(
                        data[
                            row_start_idx:row_end_idx, col_start_idx:col_end_idx
                        ].tobytes()  # type: ignore
                    )

    @staticmethod
    def is_this_converter_suitable(dataset_dir_or_fp: str) -> bool:
        """Checks whether this image sequence dataset converter is suitable
        for the provided CT (or MRI) dataset.

        Parameters
        ----------
        dataset_dir_or_fp : str
            absolute path to a dataset directory or a single file

        Returns
        -------
        bool
            True if this image sequence dataset converter is suitable. False otherwise
        """
        if os.path.isdir(dataset_dir_or_fp):
            for supported_format, ext in ImagesConverter.supported_formats.values():  # type: ignore
                fps = glob.glob(
                    os.path.join(dataset_dir_or_fp, f"*{ext}"), recursive=False  # type: ignore
                )
                if fps:
                    logging.info(
                        f"found: {len(fps)} of images of the supported format: {supported_format.upper()}"  # type: ignore
                    )
                    return True
        return False


if __name__ == "__main__":
    converter = ImagesConverter()
    converter.import_dataset(dataset_dir=r"D:\CTDatasets\dataset_02", bricksize=64)
    converter.write_metadata("output")
    converter.write_binary_chuncks("output")
    print("done")
