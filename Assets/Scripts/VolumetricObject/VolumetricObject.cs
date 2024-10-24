using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;
using UnityEngine.Profiling.Memory.Experimental;

namespace UnityCTVisualizer {

    [RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshFilter))]
    public class VolumetricObject : MonoBehaviour {

        private readonly int SHADER_BRICK_CACHE_TEX_ID = Shader.PropertyToID("_BrickCache");
        private readonly int SHADER_BRICK_CACHE_TEX_SIZE_ID = Shader.PropertyToID("_BrickCacheTexSize");
        private readonly int SHADER_TFTEX_ID = Shader.PropertyToID("_TFColors");
        private readonly int SHADER_ALPHA_CUTOFF_ID = Shader.PropertyToID("_AlphaCutoff");
        private readonly int SHADER_MAX_ITERATIONS_ID = Shader.PropertyToID("_MaxIterations");
        private readonly int SHADER_BRICK_CACHE_USAGE_BUFFER = Shader.PropertyToID("_BrickCacheUsageBuffer");
        private readonly int SHADER_BRICK_CACHE_MISSES_BUFFER = Shader.PropertyToID("_BrickCacheMissesBuffer");


        private VolumetricDataset m_volume_dataset = null;
        private ITransferFunction m_transfer_function = null;

        /////////////////////////////////
        // COROUTINES
        /////////////////////////////////
        private Coroutine m_interpolation_method_update;
        private Coroutine m_max_iterations_update;


        /////////////////////////////////
        // PARAMETERS
        /////////////////////////////////
        private Texture3D m_brick_cache = null;
        private int m_brick_cache_nbr_bricks;
        private IntPtr m_brick_cache_ptr = IntPtr.Zero;
        private TextureFormat m_brick_cache_format;
        private int m_tex_plugin_format;

        // TODO: I don't know how to make this less ugly...
        private MemoryCache<UInt16> m_cache_uint16;
        private MemoryCache<byte> m_cache_uint8;

        // private ComputeBuffer m_brick_cache_usage_buffer = null;
        // private ComputeBuffer m_brick_cache_misses_buffer = null;
        // private System.UInt32[] m_brick_cache_usage_buffer_reset;


        private Transform m_transform;
        private Material m_material;
        private IProgressHandler m_progress_handler;

        private ConcurrentQueue<UInt32> m_brick_reply_queue = new();

        private void Awake() {
            m_transform = GetComponent<Transform>();
            m_material = GetComponent<MeshRenderer>().sharedMaterial;
        }

        public void Init(VolumetricDataset volumetricDataset, IProgressHandler progressHandler = null) {
            if (volumetricDataset == null)
                throw new ArgumentNullException("the provided volumetric dataset should be a non-null reference");
            m_volume_dataset = volumetricDataset;
            m_progress_handler = progressHandler;

            StartCoroutine(InternalInit());
        }

        private IEnumerator InternalInit() {
            yield return new WaitUntil(() => (m_volume_dataset != null) && (m_transfer_function != null));

            int brick_size = m_volume_dataset.Metadata.BrickSize;

            // initialize colordepth-dependent stuff
            switch (m_volume_dataset.Metadata.ColourDepth) {
                case ColorDepth.UINT8: {
                    m_brick_cache_format = TextureFormat.R8;
                    m_tex_plugin_format = (int)TextureSubPlugin.Format.R8_UINT;
                    m_cache_uint8 = new MemoryCache<byte>(m_volume_dataset.MEMORY_CACHE_MB, (int)Math.Pow(brick_size, 3));
                    break;
                }
                case ColorDepth.UINT16: {
                    m_brick_cache_format = TextureFormat.R16;
                    m_tex_plugin_format = (int)TextureSubPlugin.Format.R16_UINT;
                    m_cache_uint16 = new MemoryCache<UInt16>(m_volume_dataset.MEMORY_CACHE_MB, (int)Math.Pow(brick_size, 3) * sizeof(UInt16));
                    break;
                }
                default:
                throw new Exception($"unknown ColourDepth value: {m_volume_dataset.Metadata.ColourDepth}");
            }

            // create the brick cache texture(s) natively in case of OpenGL/Vulkan to overcome
            // the 2GBs Unity/.NET limit. For Direct3D11/12 we don't have to create the textures
            // using the native plugin since these APIs already impose a 2GBs per-resource limit
            switch (SystemInfo.graphicsDeviceType) {
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12: {
                    if (m_volume_dataset.BRICK_CACHE_SIZE_MB > 2048) {
                        throw new NotImplementedException("multiple 3D texture brick caches are not yet implemented. "
                            + "Choose a smaller than 2GB brick cache or use a different graphics API (e.g., Vulkan/OpenGLCore)");
                    }
                    Debug.Log($"requested brick cache size{m_volume_dataset.BRICK_CACHE_SIZE_MB}MB is less than 2GB."
                        + "Using Unity's API to create the 3D texture");
                    m_brick_cache = new Texture3D(m_volume_dataset.BrickCacheSize.x, m_volume_dataset.BrickCacheSize.y,
                        m_volume_dataset.BrickCacheSize.z, m_brick_cache_format, mipChain: false, createUninitialized: true);
                    m_brick_cache_ptr = m_brick_cache.GetNativeTexturePtr();
                    Assert.AreNotEqual(m_brick_cache_ptr, IntPtr.Zero);
                    break;
                }
                case UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore:
                case UnityEngine.Rendering.GraphicsDeviceType.Vulkan: {
                    if (m_volume_dataset.BRICK_CACHE_SIZE_MB > 2048) {
                        Debug.Log($"requested brick cache size{m_volume_dataset.BRICK_CACHE_SIZE_MB}MB is larger than 2GB. "
                            + "Using native plugin to create the brick cache 3D texture");
                        TextureSubPlugin.UpdateCreateTexture3DParams((UInt32)m_volume_dataset.BrickCacheSize.x,
                            (UInt32)m_volume_dataset.BrickCacheSize.y, (UInt32)m_volume_dataset.BrickCacheSize.z,
                            m_tex_plugin_format);
                        GL.IssuePluginEvent(TextureSubPlugin.GetRenderEventFunc(),
                                (int)TextureSubPlugin.Event.CreateTexture3D);

                        yield return new WaitForEndOfFrame();

                        m_brick_cache_ptr = TextureSubPlugin.RetrieveCreatedTexture3D();
                        if (m_brick_cache_ptr == IntPtr.Zero) {
                            throw new NullReferenceException("native bricks cache pointer is nullptr " +
                                "make sure that your platform supports native code plugins");
                        }
                        m_brick_cache = Texture3D.CreateExternalTexture(m_volume_dataset.BrickCacheSize.x,
                            m_volume_dataset.BrickCacheSize.y, m_volume_dataset.BrickCacheSize.z, m_brick_cache_format,
                            mipChain: false, nativeTex: m_brick_cache_ptr);
                        break;
                    }
                    Debug.Log($"requested brick cache size{m_volume_dataset.BRICK_CACHE_SIZE_MB}MB is less than 2GB."
                        + "Using Unity's API to create the 3D texture");
                    m_brick_cache = new Texture3D(m_volume_dataset.BrickCacheSize.x, m_volume_dataset.BrickCacheSize.y,
                        m_volume_dataset.BrickCacheSize.z, m_brick_cache_format, mipChain: false, createUninitialized: true);
                    m_brick_cache_ptr = m_brick_cache.GetNativeTexturePtr();
                    Assert.AreNotEqual(m_brick_cache_ptr, IntPtr.Zero);
                    break;
                }
            }

            // set shader attributes
            m_material.SetVector(
                SHADER_BRICK_CACHE_TEX_SIZE_ID,
                new Vector4(
                    m_volume_dataset.BrickCacheSize.x,
                    m_volume_dataset.BrickCacheSize.y,
                    m_volume_dataset.BrickCacheSize.z,
                    0.0f
                )
            );
            m_material.SetTexture(
                SHADER_BRICK_CACHE_TEX_ID,
                m_brick_cache
            );

            // TODO: set up buffers
            // m_brick_cache_usage_buffer = new ComputeBuffer(m_volume_dataset.BrickCacheNbrBricks / sizeof(System.UInt32), sizeof(System.UInt32));
            // m_brick_cache_usage_buffer_reset = new System.UInt32[m_volume_dataset.BrickCacheNbrBricks / sizeof(System.UInt32)];

            // Array.Fill<System.UInt32>(m_brick_cache_usage_buffer_reset, 0);
            // ResetBrickCacheUsageBuffer();
            // m_brick_cache_misses_buffer = new ComputeBuffer(m_volume_dataset.BRICK_CACHE_MISSES_WINDOW * sizeof(System.UInt32) * 4, sizeof(System.UInt32));

            // m_attached_mesh_renderer.sharedMaterial.SetBuffer(SHADER_BRICK_CACHE_USAGE_BUFFER, m_brick_cache_usage_buffer);
            // m_attached_mesh_renderer.sharedMaterial.SetBuffer(SHADER_BRICK_CACHE_MISSES_BUFFER, m_brick_cache_misses_buffer);

            // scale mesh to match correct dimensions of the original volumetric data
            // m_transform.localScale = m_volume_dataset.Metadata.Scale;
            // rotate mesh according to provided rotation
            // m_transform.localRotation = Quaternion.Euler(m_volume_dataset.Metadata.EulerRotation);

            m_progress_handler.Enabled = true;
            Task t = Task.Run(() => {
                switch (m_volume_dataset.Metadata.ColourDepth) {
                    case ColorDepth.UINT8:
                    m_volume_dataset.LoadAllBricksIntoCache(m_cache_uint8, m_brick_reply_queue, m_progress_handler);
                    break;
                    case ColorDepth.UINT16:
                    m_volume_dataset.LoadAllBricksIntoCache(m_cache_uint16, m_brick_reply_queue, m_progress_handler);
                    break;
                    default:
                    throw new NotImplementedException();
                }
            });
            t.ContinueWith(t => { Debug.LogException(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
        }


        private void OnEnable() {
            VisualizationParametersEvents.ModelTFChange += OnModelTFChange;
            VisualizationParametersEvents.ModelAlphaCutoffChange += OnModelAlphaCutoffChange;
            VisualizationParametersEvents.ModelMaxIterationsChange += OnModelMaxIterationsChange;
            VisualizationParametersEvents.ModelInterpolationChange += OnModelInterpolationChange;

            // start handling GPU requests (i.e., visualization-driven bricks loading) or to avoid corroutines cost
            // and assuming only one camera exists in the scene, register a callback to Camera.onPostRender
            StartCoroutine(HandleGPURequests());
        }

        private void OnDisable() {
            VisualizationParametersEvents.ModelTFChange -= OnModelTFChange;
            VisualizationParametersEvents.ModelAlphaCutoffChange -= OnModelAlphaCutoffChange;
            VisualizationParametersEvents.ModelMaxIterationsChange -= OnModelMaxIterationsChange;
            VisualizationParametersEvents.ModelInterpolationChange -= OnModelInterpolationChange;

            if (m_transfer_function != null) {
                m_transfer_function.TFColorsLookupTexChange -= OnTFColorsLookupTexChange;
            }

            // don't forget to release the natively created texture
            TextureSubPlugin.UpdateClearTexture3DParams(m_brick_cache_ptr);
            GL.IssuePluginEvent(TextureSubPlugin.GetRenderEventFunc(), (int)TextureSubPlugin.Event.ClearTexture3D);
            StopAllCoroutines();
        }

        public IEnumerator HandleGPURequests() {
            yield return new WaitUntil(() => (m_volume_dataset != null) && (m_transfer_function != null) && (m_brick_cache != null));
            Debug.Log("READY");

            int total_nbr_bricks = m_volume_dataset.Metadata.TotalNbrBricks;
            int brick_size = m_volume_dataset.Metadata.BrickSize;
            int nbr_bricks_loaded = 0;

            GCHandle gc_data;

            HashSet<UInt32> _bricks_uploaded = new();
            HashSet<Vector3Int> _offsets = new();
            while (true) {
                // wait until current frame rendering is done ...
                yield return new WaitForEndOfFrame();

                // TODO: get brick requests from GPU in case of out-of-core rendering

                // upload requested bricks to the GPU from the bricks reply queue
                while (m_brick_reply_queue.TryDequeue(out UInt32 brick_id)) {
                    switch (m_volume_dataset.Metadata.ColourDepth) {
                        case ColorDepth.UINT8: {
                            var brick = m_cache_uint8.Get(brick_id);
                            Assert.IsNotNull(brick);
                            // we are sending a managed object to unmanaged thread (i.e., C++) the object has to be pinned to a
                            // fixed location in memory during the plugin call
                            gc_data = GCHandle.Alloc(brick.data, GCHandleType.Pinned);
                            break;
                        }
                        case ColorDepth.UINT16: {
                            var brick = m_cache_uint16.Get(brick_id);
                            Assert.IsNotNull(brick);
                            gc_data = GCHandle.Alloc(brick.data, GCHandleType.Pinned);
                            break;
                        }
                        default:
                        throw new NotImplementedException();
                    }

                    // TODO: maybe this is wrong for direct3D 11? Check row pitch (you are sending uint16_t and 
                    // the plugin probably expects uint8_t)

                    // update global state
                    m_volume_dataset.ComputeVolumeOffset((int)brick_id, 0, out int x, out int y, out int z);
                    // Debug.Log($"brick id: i={brick_id}; brick offset: x={x} y={y} z={z}");
                    TextureSubPlugin.UpdateTextureSubImage3DParams(m_brick_cache_ptr, x, y, z,
                        brick_size, brick_size, brick_size, gc_data.AddrOfPinnedObject(), level: 0,
                        format: m_tex_plugin_format);

                    // actual texture loading should be called from the render thread
                    GL.IssuePluginEvent(TextureSubPlugin.GetRenderEventFunc(),
                        (int)TextureSubPlugin.Event.TextureSubImage3D);

                    // wait for the end of frame again because the previous GL.IssuePluginEvent might not get
                    // called immediately (even though the documentation states that it does!)
                    // Since there are (probably) plenty of other threads adding cache entries, it is probable
                    // that the GC will move the cache entries' data (large arrays) around in the heap. The GCHandle.Free()
                    // call might happen too soon and we end up with uninitialized arrays sent to the native plugin
                    // which explains the empty bricks we get.
                    yield return new WaitForEndOfFrame();

                    // GC, you are now again free to manage the object
                    gc_data.Free();

                    _bricks_uploaded.Add(brick_id);
                    _offsets.Add(new Vector3Int(x, y, z));
                    ++nbr_bricks_loaded;
                    if (nbr_bricks_loaded == m_volume_dataset.Metadata.TotalNbrBricks) {
                        Debug.Log($"done. Bricks loaded: {nbr_bricks_loaded}");
                        Assert.IsTrue(_bricks_uploaded.Count == nbr_bricks_loaded);
                        Assert.IsTrue(_offsets.Count == nbr_bricks_loaded);
                        break;
                    }
                }

            }
        }

        /*
        private void ResetBrickCacheUsageBuffer() {
            m_brick_cache_usage_buffer.SetData(m_brick_cache_usage_buffer_reset);
        }
        */


        void OnTFColorsLookupTexChange(Texture2D new_colors_lookup_tex) {
            m_material.SetTexture(SHADER_TFTEX_ID, new_colors_lookup_tex);
        }

        private void OnModelAlphaCutoffChange(float value) {
            Debug.Log($"model: {value}");
            m_material.SetFloat(SHADER_ALPHA_CUTOFF_ID, value);
        }

        private void OnModelMaxIterationsChange(MaxIterations value) {
            Debug.Log($"model: {value}");
            if (m_max_iterations_update != null) {
                StopCoroutine(m_max_iterations_update);
                m_max_iterations_update = null;
            }
            int maxIters;
            switch (value) {
                case MaxIterations._128:
                maxIters = 128;
                break;
                case MaxIterations._256:
                maxIters = 256;
                break;
                case MaxIterations._512: maxIters = 512; break;
                case MaxIterations._1024: maxIters = 1024; break;
                case MaxIterations._2048: maxIters = 2048; break;
                default:
                throw new UnexpectedEnumValueException<MaxIterations>(value);
            }
            // do NOT update immediately as the brick cache texture could not be available
            m_max_iterations_update = StartCoroutine(UpdateWhenBrickCacheReady(() => {
                // Material.SetInteger() is busted ... do NOT use it
                m_material.SetFloat(SHADER_MAX_ITERATIONS_ID, maxIters);
                m_max_iterations_update = null;
            }));
        }

        private void OnModelTFChange(TF tf, ITransferFunction tf_so) {
            Debug.Log($"model: {tf}");
            if (m_transfer_function != null)
                m_transfer_function.TFColorsLookupTexChange -= OnTransferFunctionTexChange;
            m_transfer_function = tf_so;
            m_transfer_function.TFColorsLookupTexChange += OnTFColorsLookupTexChange;
            m_transfer_function.ForceUpdateColorLookupTexture();
        }

        private void OnModelInterpolationChange(INTERPOLATION value) {
            Debug.Log($"model: {value}");
            if (m_interpolation_method_update != null) {
                StopCoroutine(m_interpolation_method_update);
                m_interpolation_method_update = null;
            }
            switch (value) {
                case INTERPOLATION.NEAREST_NEIGHBOR:
                m_interpolation_method_update = StartCoroutine(UpdateWhenBrickCacheReady(
                    () => {
                        m_brick_cache.filterMode = FilterMode.Point;
                        m_interpolation_method_update = null;
                    }
                    ));
                break;

                case INTERPOLATION.TRILLINEAR:
                m_interpolation_method_update = StartCoroutine(UpdateWhenBrickCacheReady(
                    () => {
                        m_brick_cache.filterMode = FilterMode.Bilinear;
                        m_interpolation_method_update = null;
                    }));
                break;

                default:
                throw new UnexpectedEnumValueException<INTERPOLATION>(value);
            }
        }

        private void OnTransferFunctionTexChange(Texture2D newTex) {
            m_material.SetTexture(SHADER_TFTEX_ID, newTex);
        }

        private IEnumerator UpdateWhenBrickCacheReady(Action clbk) {
            yield return new WaitUntil(() => m_brick_cache != null);
            clbk();
        }
    }
}
