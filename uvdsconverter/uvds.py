import logging
import click
import converters

logging.basicConfig(format="%(msecs)dms [%(levelname)s]: %(message)s", level=logging.DEBUG)


@click.command()
@click.argument("dataset-dir-or-file-path", type=click.Path(exists=True))
@click.argument("output-dir", type=click.Path(exists=True, dir_okay=True, file_okay=False))
def run(
    dataset_dir_or_file_path: str,
    output_dir: str,
) -> None:
    """Converts CT (or MRI) data into a human readable metadata.txt file and
    chunked, lz4-compressed, binary Unity Volumetric DataSet (UVDS) format.

    UVDS is a simple format that is meant to be consumed directly by Unity with
    very minimal (or none) transformations during runtime. The format is meant
    to be consumed by Unity's Texture2D or Texture2DArray. For instance, if a
    volume brick (i.e., chunck) is requested during rendering, the chunck is
    simply decompressed and applied directly to the Texture2D.

    Out-of-core conversion is supported (i.e., dataset is too large to fit in
    memory all at once. The converter has a minimal memory footprint and keeps
    at most a decoded image raw data (i.e., slice) in memory at once.

    Currently supported CT (or MRI) datasets include:

    DICOM:
    a widely used international standard to transmit, store, retrieve,
    print, process, and display medical imaging information. Most probably
    everything needed to correctly visualize the volumetric data is also
    provided within the same file.

    RAW:
    binary raw format. Users are prompted for crucial volumetric properties
    such as: width, height, color depth, etc.

    IMAGE SEQUENCES (PNG, TIFF):
    a sequence of slice images ordered according to their names. User is
    prompted for crucial and optional volumetric properties such as: width,
    height, color depth, etc.
    """
    uvds_converter = converters.Converter.factory(dataset_dir_or_file_path)
    uvds_converter.import_dataset(dataset_dir_or_file_path)
    uvds_converter.write_metadata(output_dir)
    uvds_converter.write_binary_chuncks(output_dir)
    logging.info("done")


if __name__ == "__main__":
    run()
