import logging
import click
from converters.converter import BaseConverter as Converter


logging.basicConfig(format="%(msecs)dms [%(levelname)s]: %(message)s", level=logging.INFO)


@click.command()
@click.argument("dataset-dir-or-file-path", type=click.Path(exists=True))
@click.argument("uvds-write-file_path", type=click.Path(exists=False, dir_okay=False))
@click.option("--compress/--no-compress", default=True)
def run(dataset_dir_or_file_path: str, uvds_write_file_path: str, compress: bool) -> None:
    """Converts CT (or MRI) data into optimized, binary Unity Volumetric DataSet (UVDS) format."""
    uvds_converter = Converter.factory(dataset_dir_or_file_path)
    uvds = uvds_converter.convert_dataset(dataset_dir_or_file_path)
    # TODO: [Adrienne] this should take the compress boolean option
    uvds.write_binary(uvds_write_file_path)
    logging.info("Done")


if __name__ == "__main__":
    run()
