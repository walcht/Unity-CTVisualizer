import os
import sys


sys.path.append(os.path.dirname(__file__))
from converter import BaseConverter as Converter  # NOQA

# add import for new converters here (CRUCIAL)
import dicom_converter  # NOQA
import raw_converter  # NOQA
