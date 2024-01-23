import logging
import click
import converters

logging.basicConfig(format="%(msecs)dms [%(levelname)s]: %(message)s", level=logging.INFO)


@click.command()
@click.argument("dataset-dir-or-file-path", type=click.Path(exists=True))
@click.argument("uvds-write-file_path", type=click.Path(exists=False, dir_okay=False))
@click.option(
    "--compress/--no-compress",
    default=True,
    show_default=True,
    help="whether to compress output into a ZIP format",
)
def run(
    dataset_dir_or_file_path: str,
    uvds_write_file_path: str,
    compress: bool,
) -> None:
    """Converts CT (or MRI) data into optimized, binary Unity Volumetric
    DataSet (UVDS) format.

    UVDS is a simple format that is meant to be consumed by
    UnityCT-Visualizer. The format encapsulates the minimum set of necessary
    attributes for correctly visualizing a volumetric dataset. Since not all
    formats provide all described attributes, defaults are set to maximum
    range of each attribute type (e.g., 65535 for uint16). UVDS importer
    should check for these defaults before proceeding.

    Currently supported CT (or MRI) datasets include:

    DICOM:

    a widely used international standard to transmit, store, retrieve,
    print, process, and display medical imaging information. Most probably
    everything needed to correctly visualize the volumetric data is also
    provided within the same file.

    RAW:

    binary raw format. Users are prompted for crucial volumetric properties
    such as: width, height, depth, etc.

    """
    uvds_converter = converters.Converter.factory(dataset_dir_or_file_path)
    uvds = uvds_converter.convert_dataset(dataset_dir_or_file_path)
    # TODO: [Adrienne] this should take the compress boolean option
    uvds.write_binary(uvds_write_file_path)
    logging.info("done")


if __name__ == "__main__":
    run()
