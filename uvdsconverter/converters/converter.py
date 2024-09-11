from __future__ import annotations
from abc import ABCMeta, abstractmethod
from io import TextIOWrapper
from typing import Any, Tuple, Literal
import logging
import math
import os


class DatasetNotImported(Exception):
    """Raised when an operation that requires a dataset to be imported is called before importing a dataset"""

    pass


class UnsupportedDatasetFormatException(Exception):
    """Raised when no appropriated converter (or importer) for a provided dataset format is found"""

    pass


class BaseConverterMetaclass(type, metaclass=ABCMeta):
    """Converter metaclass (i.e., class for creating converter classes). The main purpose of this metaclass
    is to add references to the BaseConverter for every defined class that inherits from BaseConverter.
    """

    def __new__(cls, clsname: str, bases: Tuple[type], namespaces: dict[str, Any]):
        newly_created_cls = super().__new__(cls, clsname, bases, namespaces)
        for base_cls in bases:
            # store reference to this converter class if it inherits from BaseConverter
            if base_cls == BaseConverter:
                BaseConverter.converters.append(newly_created_cls)  # type: ignore
        return newly_created_cls


class BaseConverter(metaclass=BaseConverterMetaclass):
    """Base class for CT (or MRI) datasets convertion to UVDS format"""

    converters: list[type[BaseConverter]] = []

    def __init__(self) -> None:
        # UVDS metadata attributes
        self.originalimagewidth: int = -1
        self.originalimageheight: int = -1
        self.originalnbrslices: int = -1
        self.bricksize: int = -1
        self.nbrbricksX: int = -1
        self.nbrbricksY: int = -1
        self.nbrbricksZ: int = -1
        self.totalnbrbricks: int = -1
        self.resolutionlevels: int = -1
        self.downsamplinginterpolation: Literal["nearest", "trilinear"] = "trilinear"
        self.colordepth: int = -1
        self.lz4compressed: bool = True
        self.voxeldimX: float = 1
        self.voxeldimY: float = 1
        self.voxeldimZ: float = 1
        self.eulerrotX: float = 0
        self.eulerrotY: float = 0
        self.eulerrotZ: float = 0
        self.densitymin: float = -1
        self.densitymax: float = -1

    def write_metadata_stream(self, text_stream: TextIOWrapper) -> None:
        """Writes UVDS metadata text to a text (string) stream

        Parameters
        ----------
        text_stream: TextIOWrapper
            string stream to which the UVDS metadata text will be written
        """
        if (
            self.originalimageheight < 0
            or self.originalimagewidth < 0
            or self.originalnbrslices < 0
        ):
            raise DatasetNotImported(
                "attempt at writing UVDS metadata before importing a dataset"
            )
        text_stream.write(f"originalimagewidth={self.originalimagewidth}\n")
        text_stream.write(f"originalimageheight={self.originalimageheight}\n")
        text_stream.write(f"originalnbrslices={self.originalnbrslices}\n")
        text_stream.write(f"bricksize={self.bricksize}\n")
        text_stream.write(f"nbrbricksX={self.nbrbricksX}\n")
        text_stream.write(f"nbrbricksY={self.nbrbricksY}\n")
        text_stream.write(f"nbrbricksZ={self.nbrbricksZ}\n")
        text_stream.write(f"totalnbrbricks={self.totalnbrbricks}\n")
        text_stream.write(f"resolutionlevels={self.resolutionlevels}\n")
        text_stream.write(f"downsamplinginterpolation={self.downsamplinginterpolation}\n")
        text_stream.write(f"colordepth={self.colordepth}\n")
        text_stream.write(f"lz4compressed={1 if self.lz4compressed else 0}\n")
        text_stream.write(f"voxeldimX={self.voxeldimX}\n")
        text_stream.write(f"voxeldimY={self.voxeldimY}\n")
        text_stream.write(f"voxeldimZ={self.voxeldimZ}\n")
        text_stream.write(f"eulerrotX={self.eulerrotX}\n")
        text_stream.write(f"eulerrotY={self.eulerrotY}\n")
        text_stream.write(f"eulerrotZ={self.eulerrotZ}")
        text_stream.write(f"densitymin={self.densitymin}")
        text_stream.write(f"densitymax={self.densitymax}")

    def write_metadata(self, output_dir: str) -> None:
        """Writes UVDS metadata.txt file to provided output directory

        Parameters
        ----------
        output_dir: str
            absolute directory path to where the `metadata.txt` is going to be written.
            `output_dir` is expected to be empty otherwise an exception will be raised.

        Raises
        ------
        NotADirectoryError
            if provided directory does not exist
        """
        if not os.path.isdir(output_dir):
            raise NotADirectoryError(
                f"provided metadata output directory path: {output_dir} does not exist."
            )
        with open(os.path.join(output_dir, "metadata.txt"), "wt") as ss:
            self.write_metadata_stream(ss)

    @abstractmethod
    def write_binary_chuncks(self, directory_path: str, basename: str = "") -> None:
        """Write UVDS binary chuncks data to a provided directory path"""
        ...

    def get_nbrbricks(
        self,
        width: int,
        height: int,
        nbrslices: int,
        bricksize: int,
    ) -> tuple[int, int, int]:
        """Calculates the number of bricks for each dimension based on dataset dimensions
        and bricksize

        Parameters
        ----------
        width : int
        height : int
        nbrslices : int
        bricksize : int
            should be a power of two

        Returns
        -------
        tuple[int, int, int]
            [nbrbricksX, nbrbricksY, nbrbricksZ]
        """
        return (
            math.ceil(width / bricksize),  # nbrbricksX
            math.ceil(height / bricksize),  # nbrbricksY
            math.ceil(nbrslices / bricksize),  # nbrbricksZ
        )

    @abstractmethod
    def import_dataset(
        self,
        dataset_dir_or_fp: str,
        bricksize: int = 32,
        resolution_levels: int = 0,
        compress: bool = True,
    ) -> None:
        """Imports the CT (or MRI) dataset to Unity Volumetric DataSet (UVDS) format

        Parameters
        ----------
        dataset_dir_or_fp : str
            absolute path to a dataset directory or a single file
        """
        ...

    @staticmethod
    @abstractmethod
    def is_this_converter_suitable(dataset_dir_or_fp: str) -> bool:
        """Checks whether this converter is suitable for the provided CT (or MRI) dataset

        Parameters
        ----------
        dataset_dir_or_fp : str
            absolute path to a dataset directory or a single file

        Returns
        -------
        bool
            True if this converter is suitable. False otherwise
        """
        ...

    @staticmethod
    def factory(dataset_dir_or_fp: str) -> BaseConverter:
        """Factory for creating appropriate converters for a given dataset path.

        Parameters
        ----------
        dataset_dir_or_fp : str
            absolute path to a dataset directory or a single file

        Returns
        -------
        BaseConverter
            suitable converter that can be used to convert the dataset to UVDS format
        """
        logging.debug(f"detected converters: {BaseConverter.converters}")
        for potential_converter in BaseConverter.converters:
            if potential_converter.is_this_converter_suitable(dataset_dir_or_fp):
                logging.debug(f"selected converter: {potential_converter}")
                return potential_converter()
        raise UnsupportedDatasetFormatException(
            f"provided dataset's format at: {dataset_dir_or_fp} is not supported"
        )
