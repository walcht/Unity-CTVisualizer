from __future__ import annotations
from abc import ABCMeta, abstractmethod
from dataclasses import dataclass
from io import BufferedIOBase
from typing import Any, Dict, List, Tuple
import numpy as np
from numpy.typing import NDArray
import logging
import os
import re


@dataclass
class UVDS:
    """Unity Volumetric DataSet (UVDS) data format"""

    imagewidth: np.uint16 = np.uint16(-1)
    imageheight: np.uint16 = np.uint16(-1)
    nbrslices: np.uint16 = np.uint16(-1)
    voxeldimX: np.float32 = np.float32(-1)
    voxeldimY: np.float32 = np.float32(-1)
    voxeldimZ: np.float32 = np.float32(-1)
    minDensity: np.float32 = np.float32(-1)
    maxDensity: np.float32 = np.float32(-1)
    densities: NDArray[np.float32] = np.array([], dtype=np.float32)

    def write_binary_stream(self, uvds_stream: BufferedIOBase) -> None:
        """Writes UVDS binary to a buffered raw IO steam

        Parameters
        ----------
        uvds_stream : BufferedIOBase
            stream to which the binary UVDS will be written
        """
        uvds_stream.write(self.imagewidth.tobytes())
        uvds_stream.write(self.imageheight.tobytes())
        uvds_stream.write(self.nbrslices.tobytes())
        uvds_stream.write(self.voxeldimX.tobytes())
        uvds_stream.write(self.voxeldimY.tobytes())
        uvds_stream.write(self.voxeldimZ.tobytes())
        uvds_stream.write(self.minDensity.tobytes())
        uvds_stream.write(self.maxDensity.tobytes())
        uvds_stream.write(self.densities.tobytes())

    def write_binary(self, uvds_fp: str) -> None:
        """Write UVDS binary data to a provided filepath

        Parameters
        ----------
        uvds_fp : str
            absolute filepath to where the binary data is going to be written. Basename should end with the
            .uvds extension. If that is not the case, then whatever extension that was provided (if any) will
            be replaced with a .uvds extension.

        Raises
        ------
        FileExistsError
            if provided filepath already exists
        """
        if not uvds_fp.endswith(".uvds"):
            logging.warning(
                f"provided uvds filepath's basename: {os.path.basename(uvds_fp)} should end with .uvds"
            )
            uvds_fp = re.sub(r"\..*", ".uvds", uvds_fp)
            logging.info("added .uvds extension to provided filepath")
        if os.path.isfile(uvds_fp):
            raise FileExistsError(f"provided uvds output path: {uvds_fp} already exists.")
        logging.info(f"writing binary UVDS data to: {uvds_fp} ...")
        with open(uvds_fp, "ab") as output:
            # TODO: [Adrienne] binary files can be really large. We can compress them using ZIP then write
            #       them (with .zip extension). This function should take an option that specifies whether to
            #       use compression or not, then following this SO guide:
            #       https://stackoverflow.com/questions/2463770/python-in-memory-zip-library, the data should
            #       be compressed in a ZIP archive.
            self.write_binary_stream(output)
        logging.info(f"UVDS binary available at: {uvds_fp}")

    @staticmethod
    def serialize_binary_stream(uvds_stream: BufferedIOBase) -> UVDS:
        """Serializes (i.e., converts input data into a Python UVDS class instance) UVDS binary data provided
        through a raw IO stream.

        Parameters
        ----------
        uvds_stream : BufferedIOBase
            buffered raw IO stream containing UVDS binary data

        Returns
        -------
        UVDS
            Instance of UVDS dataclass
        """
        return UVDS(
            imagewidth=np.frombuffer(uvds_stream.read(2), dtype=np.uint16, count=1)[0],
            imageheight=np.frombuffer(uvds_stream.read(2), dtype=np.uint16, count=1)[0],
            nbrslices=np.frombuffer(uvds_stream.read(2), dtype=np.uint16, count=1)[0],
            voxeldimX=np.frombuffer(uvds_stream.read(4), dtype=np.float32, count=1)[0],
            voxeldimY=np.frombuffer(uvds_stream.read(4), dtype=np.float32, count=1)[0],
            voxeldimZ=np.frombuffer(uvds_stream.read(4), dtype=np.float32, count=1)[0],
            minDensity=np.frombuffer(uvds_stream.read(4), dtype=np.float32, count=1)[0],
            maxDensity=np.frombuffer(uvds_stream.read(4), dtype=np.float32, count=1)[0],
            densities=np.frombuffer(uvds_stream.read(), dtype=np.float32)[0],
        )

    @staticmethod
    def serialize_binary(uvds_fp: str) -> UVDS:
        """Serializes (i.e., converts input data into a Python UVDS class instance) binary UVDS data in a
        provided filepath.

        Parameters
        ----------
        uvds_fp : str
            filepath to binary UVDS data

        Returns
        -------
        UVDS
            Instance of UVDS dataclass
        """
        with open(uvds_fp, "rb") as binary_stream:
            result = UVDS.serialize_binary_stream(binary_stream)
            return result


class UnsupportedDatasetFormatException(Exception):
    """Raised when no appropriated converter (or importer) for a provided dataset format is found"""

    pass


class BaseConverterMetaclass(type, metaclass=ABCMeta):
    """Converter metaclass (i.e., class for creating converter classes). The main purpose of this metaclass
    is to add references to the BaseConverter for every defined class that inherits from BaseConverter."""

    def __new__(cls, clsname: str, bases: Tuple[type], namespaces: Dict[str, Any]):
        newly_created_cls = super().__new__(cls, clsname, bases, namespaces)
        for base_cls in bases:
            # store reference to this converter class if it inherits from BaseConverter
            if base_cls == BaseConverter:
                BaseConverter.importers.append(newly_created_cls)
        return newly_created_cls


class BaseConverter(metaclass=BaseConverterMetaclass):
    importers: List[type] = []

    @abstractmethod
    def convert_dataset(self, dataset_dir_or_fp: str) -> UVDS:
        """Imports and converts CT (or MRI) dataset to Unity Volumetric DataSet (UVDS) format

        Parameters
        ----------
        dataset_dir_or_fp : str
            absolute path to a dataset directory or a single file

        Returns
        -------
        UVDS
            Instance of a UVDS dataclass containing converted import data attributes
        """
        ...

    @abstractmethod
    def is_this_converter_suitable(self, dataset_dir_or_fp: str) -> bool:
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

    @classmethod
    def factory(cls, dataset_dir_or_fp: str) -> BaseConverter:
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
        for potential_importer in cls.importers:
            if potential_importer.is_this_importer_suitable(dataset_dir_or_fp):
                return potential_importer()
        raise UnsupportedDatasetFormatException(
            f"provided dataset's format at: {dataset_dir_or_fp} is not supported"
        )