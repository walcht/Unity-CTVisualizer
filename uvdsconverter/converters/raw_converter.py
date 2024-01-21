from converters.converter import BaseConverter, UVDS
import os


class RawConverter(BaseConverter):
    def convert_dataset(self, dataset_dir_or_fp: str) -> UVDS:
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
        if os.path.isfile(dataset_dir_or_fp):
            # TODO: Adrienne, please implement RawConverter that converts raw data input
            #       (see test_datasets/raw/ directory for examples of RAW datasets)
            #       Important thing to note: RAW datasets do not contain volume attributes in them! So you
            #       can either request the user to supply additional arguments or you can leave them empty
            #       (so that they are supplied in Unity)
            return UVDS()
        raise FileNotFoundError(f"{dataset_dir_or_fp} is not a valid filepath.")

    def is_this_converter_suitable(self, dataset_dir_or_fp: str) -> bool:
        """Checks whether this raw dataset converter is suitable for the provided CT (or MRI) dataset

        Parameters
        ----------
        dataset_dir_or_fp : str
            absolute path to a dataset directory or a single file

        Returns
        -------
        bool
            True if this raw dataset converter is suitable. False otherwise
        """
        if os.path.isfile(dataset_dir_or_fp):
            # TODO: [Adrienne] simplest thing is to check for file extension here. (".raw")
            return False
        raise FileNotFoundError(
            f"{dataset_dir_or_fp} is neither a valid directory path nor a valid filepath."
        )
