# Unity CTVisualizer

<!--toc:start-->
- [Unity CTVisualizer](#unity-ctvisualizer)
  - [Workflow](#workflow)
  - [Installation](#installation)
  - [Usage](#usage)
  - [Performance Statistics](#performance-statistics)
  - [Limitations](#limitations)
  - [License](#license)
<!--toc:end-->

A Unity3D package for efficiently visualizing and manipulating (mostly)
medical volumetric data.

## Workflow

UnityCT-Visualizer is actually made of two *sub-applications*:

1. Unity Volumetric DataSet (hereafter UVDS) Converter written purely in Python
1. Unity package that deals with visualization code

CT datasets with supported formats are converted using a Python script into a .uvds
format; a very minimal format that we defined and that our C# Unity importer expects.

![workflow](https://github.com/walcht/Unity-Immersive-Analytics/assets/89390465/b603309f-642f-4978-b999-57a27ebb7e6a)

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

## Usage

## Project Structure

## Optimization Techniques

TODO: [Adrienne] can you provided the set of optimization techniques we implemented
and also describe them with some nicely descriptive images?

### Empty space skipping

In this optimization method, we define volume levels that determine the number of cells into which the volume should be divided. At level 0, we divide the volume into ![equation](https://latex.codecogs.com/svg.image?{\color{Black}N\times&space;N\times&space;N}) cells, where ![equation](https://latex.codecogs.com/svg.image?{\color{Black}N=2^{M}}). Here, ![equation](https://latex.codecogs.com/svg.image?{\color{Black}M&plus;1}) represents the number of levels, and the levels are indexed from 0 to ![equation](https://latex.codecogs.com/svg.image?{\color{Black}M}). On level 1, we increase the size of cells so that each cell is equal in size to 8 cells from the previous level. On this level, the volume is divided into  ![equation](https://latex.codecogs.com/svg.image?{\color{Black}N\times&space;N\times&space;N}) cells. We repeat this process for each level until the volume contains only one cell.

<img src="Documentation/1empty_space_skipping.png" alt="Hierarchical enumeration of object space for N = 5." width="400">
*1. Hierarchical enumeration of object space for N = 5.*

We treat voxels as points located on the vertices of cells, each having opacity and color. On level 0, the value of a cell is zero if all eight voxels on its vertices have an opacity of zero. On a higher level `m` (`m > 0`), a cell contains a zero if all eight cells that make up this cell on level `m - 1` contain zeros.

The empty space skipping algorithm starts on the top level M. When the ray enters the cell, we check its value. If the value of the cell is zero, we determine the next cell on the same level by following the ray. If the parent of the next cell and the parent of the current cell are different, we move up to the parent of the next cell, otherwise we move to the next cell following the ray. This makes it possible to quickly progress through empty spaces. 
If the the value of a cell is one, we move down one level. When we reach the lowest level, we know that at least one of the voxels located on the vertices of the cell has an opacity value greater then 0. We sample the ray section that falls within this cell. We approximate the color and opacity of each sample point by trilinearly interpolating the color and opacity values of the eight surrounding voxels.

<img src="Documentation/2empty_space_skipping.png" alt="Ray tracing of hierarchical enumeration." width="400">
*2. Ray tracing of hierarchical enumeration.*

## Performance Statistics

TODO: Add performance statistics for datasets with various sizes and characteristics
here.

## Limitations

## License
