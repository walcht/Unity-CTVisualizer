# Unity CTVisualizer

A Unity3D package for efficient volumetric data direct rendering and manipulation.

## Workflow

CT datasets with supported formats are converted using a Python script into a .uvds
(Unity Volumetric DataSet) format; a format that we defined and that our C# Unity
importer expects.

![workflow](https://github.com/walcht/Unity-Immersive-Analytics/assets/89390465/b603309f-642f-4978-b999-57a27ebb7e6a)

We decided to use Python for parsing CT dataset formats and converting them to uvds
because:
1.  the excelent support certain Python packages provide for imaging data
    formats (e.g., DICOM)
1.  We believe that using Python for data manipulation tasks is convenient
1.  Datasets are supposed to be converted before runtime, therefore execution
    speed is not a core objective for this part
1.  We want to seperate code that deals with various dataset formats with code
    that deals with visualization tasks

## Performance Statistics

TODO: Add performance statistics for datasets with various sizes and characteristics
here.

## Limitations

## License
