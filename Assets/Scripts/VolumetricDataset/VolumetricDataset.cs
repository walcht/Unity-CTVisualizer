// #define IN_CORE

using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityCTVisualizer {

    /// <summary>
    ///     Serializable wrapper around a volumetric dataset
    /// </summary>
    ///
    /// <remarks>
    ///     Includes metadata about the volume dataset, its visualization parameters,
    ///     and other configurable parameters. Once tweaked for optimal performance/visual quality
    ///     tradeoff, this can be saved (i.e., serialized) for later use.
    /// </remarks>
    [CreateAssetMenu(
        fileName = "volumetric_dataset",
        menuName = "UnityCTVisualizer/VolumetricDataset"
    )]
    public class VolumetricDataset : ScriptableObject {

        [DllImport("RenderingPlugin")]
        private static extern void SendBrickDataToGPU(
            System.IntPtr texture,
            System.Int32 tex_width,
            System.Int32 tex_height,
            System.Int32 tex_depth,
            System.Int32 x_offset,
            System.Int32 y_offset,
            System.Int32 z_offset,
            System.Int32 brick_width,
            System.Int32 brick_height,
            System.Int32 brick_depth,
            System.IntPtr data
        );

        public IProgressHandler ProgressHandler { get; set; }
        private UVDSMetadata m_metadata = new();
        public UVDSMetadata Metadata { get => m_metadata; }


        // bricks-cache related
#if IN_CORE
        private Vector3Int m_brick_cache_size;
        public Vector3Int BrickCacheSize { get => m_brick_cache_size; }
#else
        private Vector3Int m_brick_cache_size = new(1024, 1024, 1024);
        public Vector3Int BrickCacheSize { get => m_brick_cache_size; set { m_brick_cache_size = value; } }
#endif
        private Texture3D m_brick_cache;
        public Texture3D BrickCacheTex { get => m_brick_cache; }
        private IntPtr m_brick_cache_ptr;
        private TextureFormat m_format;

        // visualization parameters

        [SerializeField]
        Texture3D m_densities_gradient_sampler = null;

        [SerializeField]
        private string m_dataset_path;
        public string DatasetPath {
            get => m_dataset_path;
            set {
                m_dataset_path = value;
                Importer.ImportMetadata(m_dataset_path, ref m_metadata);
#if IN_CORE
                m_brick_cache_size = new Vector3Int(m_metadata.ImageWidth, m_metadata.ImageHeight, m_metadata.NbrSlices);
#endif
                if (m_metadata.ColourDepth == ColorDepth.UINT8) {
                    m_format = TextureFormat.R8;
                } else if (m_metadata.ColourDepth == ColorDepth.FLOAT16) {
                    m_format = SystemInfo.SupportsTextureFormat(TextureFormat.RHalf) ? TextureFormat.RHalf : TextureFormat.RFloat;
                } else {
                    // TODO: add appropriate throw exception
                    throw new Exception("TODO");
                }

                // TODO: handle unsufficient memory exceptions
                m_brick_cache = new Texture3D(
                    m_brick_cache_size.x,
                    m_brick_cache_size.y,
                    m_brick_cache_size.z,
                    m_format,
                    mipChain: false,
                    createUninitialized: true
                );
                m_brick_cache.filterMode = FilterMode.Bilinear;
                m_brick_cache_ptr = m_brick_cache.GetNativeTexturePtr();
                if (m_brick_cache_ptr == IntPtr.Zero) {
                    throw new NullReferenceException("native bricks cache pointer is nullptr" +
                        "make sure that your platform support native code plug-ins");
                }
#if IN_CORE
                // construct 
#endif
            }
        }


        public Texture3D GetDensitiesGradientSampler {
            get {
                if (m_densities_gradient_sampler == null) { }
                // GenerateGradientMagnitudeVolume();
                return m_densities_gradient_sampler;
            }
            private set { m_densities_gradient_sampler = value; }
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

        public Vector3Int ComputeVolumeOffset(int brick_id, int resolution_lvl) {
            int nbr_bricks_x_axis_res_lvl = (m_metadata.NbrBricksX >> resolution_lvl) + (m_metadata.NbrBricksX & 1);
            int nbr_bricks_z_axis_res_lvl = (m_metadata.NbrBricksZ >> resolution_lvl) + (m_metadata.NbrBricksZ & 1);
            return new(
                (brick_id % nbr_bricks_x_axis_res_lvl) * m_metadata.BrickSize,
                (brick_id % (nbr_bricks_x_axis_res_lvl * nbr_bricks_z_axis_res_lvl)) * m_metadata.BrickSize,
                ((brick_id / nbr_bricks_x_axis_res_lvl) % nbr_bricks_z_axis_res_lvl) * m_metadata.BrickSize
           );
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
        public IEnumerator LoadBrick(Vector3Int brick_cache_offset, byte[] brick_data) {
            // wait until current frame rendering is done because manipulating GPU data while rendering might be dangerous
            yield return new WaitForEndOfFrame();
            // make sure GC doesn't change location of brick data in memory (i.e., pinned)
            GCHandle gc_brick_data = GCHandle.Alloc(brick_data, GCHandleType.Pinned);
            SendBrickDataToGPU(
                m_brick_cache_ptr,
                m_brick_cache_size.x,
                m_brick_cache_size.y,
                m_brick_cache_size.z,
                brick_cache_offset.x,
                brick_cache_offset.y,
                brick_cache_offset.z,
                m_metadata.BrickSize,
                m_metadata.BrickSize,
                m_metadata.BrickSize,
                gc_brick_data.AddrOfPinnedObject()
            );
            // GC, do whatever you want now ...
            gc_brick_data.Free();
        }
    }
}
