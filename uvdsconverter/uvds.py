import logging
import click
from converters import Converter


logging.basicConfig(format="%(msecs)dms [%(levelname)s]: %(message)s", level=logging.INFO)


@click.argument()
def run(dataset_fp: str, uvds_write_fp: str) -> None:
    """Converts CT (or MRI) data into optimized, binary Unity Volumetric DataSet (UVDS) format."""
    uvds_converter = Converter.converter_factory(dataset_fp)
    uvds = uvds_converter.convert_dataset(dataset_fp)
    uvds.write_binary(uvds_write_fp)
    logging.info("Done")


if __name__ == "__main__":
    run()
