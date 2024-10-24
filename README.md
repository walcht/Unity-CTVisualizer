# Unity CTVisualizer

<!--toc:start-->
- [Unity CTVisualizer](#unity-ctvisualizer)
  - [Workflow](#workflow)
  - [Installation](#installation)
  - [Usage](#usage)
  - [Project Structure](#project-structure)
  - [Optimization Techniques](#optimization-techniques)
    - [Empty space skipping](#empty-space-skipping)
    - [Early ray termination](#early-ray-termination)
  - [Performance Statistics](#performance-statistics)
  - [TODOs](#todos)
  - [License](#license)
<!--toc:end-->

A Unity3D package for efficiently visualizing and manipulating large-scale (mostly)
medical volumetric data.

![unity-ct-visualizer-00](https://github.com/walcht/Unity-CTVisualizer/assets/89390465/6084fb58-689a-4cf9-97f4-b84a0e262e9a)

## Workflow

UnityCT-Visualizer is actually made of two *sub-applications*:

1. Unity Volumetric DataSet (hereafter UVDS) Converter written purely in Python
1. Unity package that deals with visualization code

CT datasets with supported formats are converted using a Python script into a .uvds
format; a very minimal format that we defined and that our C# Unity importer expects.

![importer](https://github.com/walcht/Unity-CTVisualizer/assets/148270400/cbd2edd8-bde7-4b07-83cd-8f1adbc187d2)

We decided to use Python for parsing CT (or MRI) dataset formats and converting
them to **UVDS** because:

1. The excellent support certain Python packages (mainly pydicom) provide
for imaging data formats (e.g., DICOM).

1. We believe that using Python for data manipulation tasks is convenient and
very-easily extendable.

1. Datasets are supposed to be converted before runtime then imported to the target
device on which the Unity application will run. This way we can provide the bare
minimum needed for visualizations and that results in smaller datasets and faster
import time on the Unity side.

1. We want to separate code that deals with various dataset formats from code
that deals with visualization tasks.

## Installation

1. For converting volumetric medical datasets, **Python >= 3.10.12** is required.
Latest version of Python can be installed from [here](https://www.python.org/downloads/).
1. Required packages should be installed. Assuming you are in parent repository directory
(where this README exists) and assuming you have a pip installed:

    ```bash
    pip3 install -r uvdsconverter/requirements.txt
    ```

    or (if pip3 didn't work):

    ```bash
    pip install -r uvdsconverter/requirements.txt
    ```

1. For detailed instructions on how to use the converter, you can run the main
CLI script with help option:

    ```bash
    python3 uvdsconverter/uvds.py --help
    ```

    Which should output the following synopsis:

    ```bash
    Usage: uvds.py [OPTIONS] DATASET_DIR_OR_FILE_PATH UVDS_WRITE_FILE_PATH
    ```

1. **Unity = 2022.3.XXXX LTS** has to be installed. We try to keep testing our
package on later Unity versions. The table below describes the versions of
Unity editor which have been tested and their test results.

1. Clone this repository into some folder (currently we don't provided a Unity
package):

    ```bash
    git clone https://github.com/walcht/Unity-CTVisualizer.git
    ```

Tested on these Unity versions:

| Unity Version | OS             | Status | Notes |
|---------------|----------------|--------|-------|
| 2022.3.17f1   | 22.04.1-Ubuntu |:white_check_mark:||
| 2022.3.17f1   | macOS 14.2.1   |:white_check_mark:||

## Usage

 UnityCT-Visualizer is a UI-centric application, i.e., all operations are mainly
 done through the provided GUI. Visualize a CT\MRI dataset within the Unity Editor:

 1. Click on ```import``` to import a .uvds dataset. Archived UVDS datasets are
 also importable (currently only ZIP).

 1. Once import is done, the volumetric object should appear along with other UI
 components

 1. In the ```Visualization Parameters``` UI component, chose the transfer function
 you want to use (currently only 1D is supported). You can also change the
 interpolation, although currently we advice against choosing other than
 ```nearest neighbor```

 1. The default TF is TF1D, its UI will be shown in the bottom:

    ![unity-ct-visulizer-TF](https://github.com/walcht/Unity-CTVisualizer/assets/89390465/3d4decdd-6974-46c6-b572-f41088265e75)
    - green line is for opacities classification
    - bottom gradient color band/texture is for colors (no alpha) classification
    - changes are real-time reflected in the volumetric object

## Project Structure

TODO

## Optimization Techniques

### Empty space skipping
TODO

### Early ray termination

TODO

## Performance Statistics

TODO

## TODOs

- [ ] find an efficient *empty ray skipping* optimization technique implementation
- [ ] add 2D gradient-based transfer function
- [ ] add doxygen for documentation generation
- [ ] optimize DVR Shader implementations
- [ ] add zoom-in/zoom-out functionalities for the TF UI

## License

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
See LICENSE.txt file for more info.
