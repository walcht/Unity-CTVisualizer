// #define IN_CORE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityCTVisualizer {

    /// <summary>
    ///     Serializable wrapper around a volumetric dataset and its visualization
    ///     parameters.
    /// </summary>
    ///
    /// <remarks>
    ///     Includes metadata about the volume dataset, its visualization parameters,
    ///     and other configurable parameters. Once tweaked for optimal performance/visual quality
    ///     tradeoff, this can be saved (i.e., serialized) for later use.
    ///     It is not a good idea to store and serialize the brick cache here (i.e., the Texture3D
    ///     object(s)) since the brick cache is visualization-driven and usually is of very large
    ///     size.
    /// </remarks>
    [CreateAssetMenu(
        fileName = "volumetric_dataset",
        menuName = "UnityCTVisualizer/VolumetricDataset"
    )]
    public class VolumetricDataset : ScriptableObject {

        /////////////////////////////////
        // CONSTANTS
        /////////////////////////////////
        // public readonly int MAX_NBR_BRICK_CACHE_TEXTURES = 8;
        public readonly long MIN_BRICK_SIZE = (long)Math.Pow(32, 3);
        public readonly long MAX_BRICK_SIZE = (long)Math.Pow(128, 3);
        public readonly long MAX_BRICKS_CACHE_NBR_BRICKS = 32768; // == 2048^3 / 32^3
        public readonly long MAX_CACHE_USAGE_REPORTING_SIZE = 1024; // == 32768 / 32 of uint32 => 512 KB
        public readonly int BRICK_CACHE_MISSES_WINDOW = 128 * 128;
        public readonly int MEMORY_CACHE_MB = 4096;
        public float BRICK_CACHE_SIZE_MB;

        /////////////////////////////////
        // VISUALIZATION PARAMETERS
        /////////////////////////////////
        private float m_AlphaCutoff = 254.0f / 255.0f;
        private MaxIterations m_MaxIterations = MaxIterations._1024;
        private INTERPOLATION m_Interpolation = INTERPOLATION.TRILLINEAR;

        public float AlphaCutoff {
            get => m_AlphaCutoff; set {
                m_AlphaCutoff = value;
                VisualizationParametersEvents.ModelAlphaCutoffChange?.Invoke(value);
            }
        }
        public MaxIterations MaxIterations {
            get => m_MaxIterations; set {
                m_MaxIterations = value;
                VisualizationParametersEvents.ModelMaxIterationsChange?.Invoke(value);
            }
        }
        public INTERPOLATION InterpolationMethode {
            get => m_Interpolation; set {
                m_Interpolation = value;
                VisualizationParametersEvents.ModelInterpolationChange?.Invoke(value);
            }
        }

        private TF m_CurrentTF = TF.TF1D;
        private Dictionary<TF, ITransferFunction> m_TransferFunctions;
        public TF TransferFunction {
            set {
                m_CurrentTF = value;
                ITransferFunction tf_so;
                if (!m_TransferFunctions.TryGetValue(value, out tf_so)) {
                    tf_so = TransferFunctionFactory.Create(value);
                    m_TransferFunctions.Add(m_CurrentTF, tf_so);
                }
                VisualizationParametersEvents.ModelTFChange?.Invoke(m_CurrentTF, tf_so);
            }
        }

        public void DispatchVisualizationParamsChangeEvents() {
            VisualizationParametersEvents.ModelTFChange?.Invoke(m_CurrentTF, m_TransferFunctions[m_CurrentTF]);
            VisualizationParametersEvents.ModelAlphaCutoffChange?.Invoke(m_AlphaCutoff);
            VisualizationParametersEvents.ModelInterpolationChange?.Invoke(m_Interpolation);
            VisualizationParametersEvents.ModelMaxIterationsChange?.Invoke(m_MaxIterations);
        }

        /////////////////////////////////
        // PARAMETERS
        /////////////////////////////////
        private Vector3Int m_brick_cache_size;
        public Vector3Int BrickCacheSize { get => m_brick_cache_size; }

        private UVDSMetadata m_metadata = new();
        public UVDSMetadata Metadata { get => m_metadata; }

        [SerializeField]
        private string m_dataset_path;
        public string DatasetPath {
            get => m_dataset_path;
            set {
                m_dataset_path = value;
                Importer.ImportMetadata(m_dataset_path, ref m_metadata);
                m_brick_cache_size = new Vector3Int(
                    m_metadata.NbrBricksX * m_metadata.BrickSize,
                    m_metadata.NbrBricksY * m_metadata.BrickSize,
                    m_metadata.NbrBricksZ * m_metadata.BrickSize
                );
                BRICK_CACHE_SIZE_MB = (m_brick_cache_size.x / 1024.0f) * (m_brick_cache_size.y / 1024.0f) * m_brick_cache_size.z *
                    (m_metadata.ColourDepth == ColorDepth.UINT16 ? 2.0f : 1.0f);
            }
        }

        /*
        * 
        *    expected brick layout from the imported UVDS:
        * 
        *                         Y
        *                        ↗
        *                 c111 .+------------------------------+ c110
        *                    .' |                            .'|
        *                  .'   |                          .'  |
        *                .'     |                        .'    |
        *              .'       |                      .'      |
        *         Z  .'         |                    .'        |
        *         ↑.'           |                  .'          |
        *    c011 +-------------------------------+ c010       |
        *         |             |                 |            |
        *         |             |                 |            |
        *         |             |                 |            |
        *         |             |                 |            |
        *         |        c100 +-----------------|------------+ c101
        *         |          .'                   |          .'
        *         |        .'                     |        .'
        *         | * - - *                       |      .'
        *         |- - -* |                       |    .'
        *         |BRICK| |                       |  .'
        *         | ID0 |'                        |.'
        *    c000 +-------------+-----------------+ c001 ⟶  X
        *     
        *     bottom-left brick (c000) has ID 0, the next brick in that same (X, Z) brick
        *     plane has ID 1 and so on until top right of  the same brick plane the first
        *     brick exists in (c010) has ID:  (nbr_bricks_x_axis * nbr_bricks_z_axis) - 1
        *     the same process repeats moving along the Y axis until last brick (at c110)
        *     
        *     Note:
        *     nbr_bricks_[x,y,z]_axis reflects  the number of bricks  along an axis  in a
        *     given resolution level. For example if  nbr_bricks_x_axis=11  is the number
        *     of bricks along the X axis for resolution level 0 then the number of bricks
        *     for the next resolution level (always along the X axis) is:
        *
        *       nbr_bricks_x_axis_res_lvl_1 = (nbr_bricks_x_axis / 2^1) +
        *                                     (nbr_bricks_x_axis & 1)
        *                                   = 6
        *       
        *     in case  nbr_bricks_x_axis is odd,  an extra padding brick  has to be added
        *     for proper downsampling alignment. The above formula can be generalized for 
        *     any resolution level l to the following:
        *     
        *       nbr_bricks_x_axis_res_lvl_l = (nbr_bricks_x_axis / 2^l) +
        *                                     (nbr_bricks_x_axis & 1)
        *
        */

        public void ComputeVolumeOffset(UInt32 brick_id, out int x, out int y, out int z) {
            int brick_id_in_res_lvl = (int)(brick_id & 0x03FFFFFF);
            int resolution_lvl = (int)(brick_id >> 26);
            int brick_size = m_metadata.BrickSize;
            int nbr_bricks_x_axis_res_lvl = m_metadata.NbrBricksX;
            int nbr_bricks_y_axis_res_lvl = m_metadata.NbrBricksY;
            if (resolution_lvl > 0) {
                nbr_bricks_x_axis_res_lvl = (m_metadata.NbrBricksX >> resolution_lvl) + (m_metadata.NbrBricksX & 1);
                nbr_bricks_y_axis_res_lvl = (m_metadata.NbrBricksY >> resolution_lvl) + (m_metadata.NbrBricksY & 1);
            }
            x = brick_size * (brick_id_in_res_lvl % nbr_bricks_x_axis_res_lvl);
            y = brick_size * ((brick_id_in_res_lvl / nbr_bricks_x_axis_res_lvl) % nbr_bricks_y_axis_res_lvl);
            z = brick_size * (brick_id_in_res_lvl / (nbr_bricks_x_axis_res_lvl * nbr_bricks_y_axis_res_lvl));
        }
        public void ComputeVolumeOffset(int brick_id, int resolution_lvl, out int x, out int y, out int z) {
            int brick_size = m_metadata.BrickSize;
            int nbr_bricks_x_axis_res_lvl = m_metadata.NbrBricksX;
            int nbr_bricks_y_axis_res_lvl = m_metadata.NbrBricksY;
            if (resolution_lvl > 0) {
                nbr_bricks_x_axis_res_lvl = (m_metadata.NbrBricksX >> resolution_lvl) + (m_metadata.NbrBricksX & 1);
                nbr_bricks_y_axis_res_lvl = (m_metadata.NbrBricksY >> resolution_lvl) + (m_metadata.NbrBricksY & 1);
            }
            x = brick_size * (brick_id % nbr_bricks_x_axis_res_lvl);
            y = brick_size * ((brick_id / nbr_bricks_x_axis_res_lvl) % nbr_bricks_y_axis_res_lvl);
            z = brick_size * (brick_id / (nbr_bricks_x_axis_res_lvl * nbr_bricks_y_axis_res_lvl));
        }

        /// <summary>
        ///     Given a <paramref name="brick_id"/>, sends the respective volume brick to GPU's
        ///     brick cache or to the densities texture in case OUT_OF_CORE is not set.
        /// </summary>
        ///
        /// <remarks>
        ///
        ///     In case of in-core rendering (i.e., IN_CORE is set), the brick is simply loaded
        ///     into its original offset in the volume. In case of out-of-core rendering, two
        ///     scenarios arrise:
        ///     
        ///     <list type="number">
        ///         <item>there an empty brick slot in the brick cache => just put the brick there</item>
        ///         <item>there is not any empty brick slots => implement a cache replacement policy</item>
        ///     </list>
        ///     
        ///
        /// </remarks>
        public void LoadBrick(Vector3Int brick_cache_offset, byte[] brick_data) { }

        public void LoadAllBricksIntoCache(MemoryCache<UInt16> cache, ConcurrentQueue<UInt32> brick_reply_queue, IProgressHandler progressHandler = null) {
            if (progressHandler != null) {
                progressHandler.Progress = 0;
                // progressHandler.Message = $"loading {m_metadata.TotalNbrBricks} bricks";
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            Parallel.For(0, m_metadata.TotalNbrBricks, new ParallelOptions() {
                TaskScheduler = new LimitedConcurrencyLevelTaskScheduler(Environment.ProcessorCount - 2)
            }, i => {
                // load a chunk of a UVDS test dataset
                UInt16[] data = new UInt16[m_metadata.BrickSize * m_metadata.BrickSize * m_metadata.BrickSize];
                if (!Importer.ImportChunk(m_dataset_path, ref data, m_metadata, i, 0)) {
                    UnityEngine.Debug.LogError("could not import chunk");
                    return;
                }
                cache.Set((uint)i, new CacheEntry<UInt16>(data, 0, 0));
                brick_reply_queue.Enqueue((UInt32)i);
                if (progressHandler != null) {
                    progressHandler.Progress += 1.0f / m_metadata.TotalNbrBricks;
                }
            });
            stopwatch.Stop();
            UnityEngine.Debug.Log($"uploading to cache took: {stopwatch.Elapsed}s");
        }

        public void LoadAllBricksIntoCache(MemoryCache<byte> cache, ConcurrentQueue<UInt32> brick_reply_queue, IProgressHandler progressHandler = null) {
            if (progressHandler != null) {
                progressHandler.Progress = 0;
                // progressHandler.Message = $"loading {m_metadata.TotalNbrBricks} bricks";
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            Parallel.For(0, m_metadata.TotalNbrBricks, new ParallelOptions() {
                TaskScheduler = new LimitedConcurrencyLevelTaskScheduler(Environment.ProcessorCount - 2)
            }, i => {
                // load a chunk of a UVDS test dataset
                byte[] data;
                if (!Importer.ImportChunk(m_dataset_path, out data, m_metadata, i, 0)) {
                    UnityEngine.Debug.LogError("could not import chunk");
                    return;
                }
                cache.Set((uint)i, new CacheEntry<byte>(data, 0, 0));
        privat
                brick_reply_queue.Enqueue((UInt32)i);
                if (progressHandler != null) {
                    progressHandler.Progress += 1.0f / m_metadata.TotalNbrBricks;
                }
            });
            stopwatch.Stop();
            UnityEngine.Debug.Log($"uploading to cache took: {stopwatch.Elapsed}s");
        }

        private void OnEnable() {
            if (m_TransferFunctions == null) {
                m_TransferFunctions = new Dictionary<TF, ITransferFunction> { { TF.TF1D, TransferFunctionFactory.Create(TF.TF1D) } };
            }

            VisualizationParametersEvents.ViewTFChange += OnViewTFChange;
            VisualizationParametersEvents.ViewAlphaCutoffChange += OnViewAlphaCutoffChange;
            VisualizationParametersEvents.ViewMaxIterationsChange += OnViewMaxIterationsChange;
            VisualizationParametersEvents.ViewInterpolationChange += OnViewInterpolationChange;
        }

        private void OnDisable() {
            VisualizationParametersEvents.ViewTFChange -= OnViewTFChange;
            VisualizationParametersEvents.ViewAlphaCutoffChange -= OnViewAlphaCutoffChange;
            VisualizationParametersEvents.ViewMaxIterationsChange -= OnViewMaxIterationsChange;
            VisualizationParametersEvents.ViewInterpolationChange -= OnViewInterpolationChange;
        }

        private void OnViewAlphaCutoffChange(float alphaCutoff) {
            AlphaCutoff = Mathf.Clamp01(alphaCutoff);
        }

        private void OnViewMaxIterationsChange(MaxIterations maxIterations) {
            MaxIterations = maxIterations;
        }

        private void OnViewInterpolationChange(INTERPOLATION interpolation) {
            InterpolationMethode = interpolation;
        }

        private void OnViewTFChange(TF new_tf) {
            TransferFunction = new_tf;
        }
    }
}
