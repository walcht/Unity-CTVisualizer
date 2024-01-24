from converter import BaseConverter, UVDS
import os
import numpy as np
import click


class RawConverter(BaseConverter):
    def convert_dataset(self,dataset_fp: str) -> UVDS:
        # TODO: [Adrienne] mention in this documentation how are you going to populate missing fields (
        #       because .raw files don't contain pixel spacing and other attributes...)
        """Imports and converts RAW (.raw extension) dataset to Unity Volumetric DataSet (UVDS) format
        Parameters
        ----------
        dataset_dir_or_fp : str
            absolute path to a dataset directory or a single file

        Returns
        -------
        UVDS
            Instance of a UVDS dataclass containing converted import data attributes
        """
        if os.path.isfile(dataset_fp):
            # TODO: Adrienne, please implement RawConverter that converts raw data input
            #       (see test_datasets/raw/ directory for examples of RAW datasets)
            #       Important thing to note: RAW datasets do not contain volume attributes in them! So you
            #       can either request the user to supply additional arguments or you can leave them empty
            #       (so that they are supplied in Unity)

            width = click.prompt("Enter width", type=int)
            height = click.prompt("Enter height", type=int)
            depth = click.prompt("Enter depth", type=int)
            voxeldimX = click.prompt("Enter voxel dimension x", type=float)
            voxeldimY = click.prompt("Enter voxel dimension y", type=float)
            voxeldimZ = click.prompt("Enter voxel dimension z", type=float)
            num_elements = width * height * depth
            raw_data = np.empty(num_elements, dtype=np.float16)
            with open(dataset_fp, 'rb') as file:
                raw_data = np.fromfile(file, dtype=np.float16, count=num_elements)
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
        raise FileNotFoundError(f"{dataset_fp} is not a valid filepath.")

    @staticmethod
    def is_this_converter_suitable(dataset_fp: str) -> bool:
        """Checks whether this raw dataset converter is suitable for the provided CT (or MRI) dataset

        Parameters
        ----------
        dataset_fp : str
            absolute path to a dataset directory or a single file

        Returns
        -------
        bool
            True if this raw dataset converter is suitable. False otherwise
        """            # TODO: [Adrienne] simplest thing is to check for file extension here. (".raw")
        return os.path.isfile(dataset_fp) and os.path.splitext(dataset_fp)[-1].lower() == ".raw"
