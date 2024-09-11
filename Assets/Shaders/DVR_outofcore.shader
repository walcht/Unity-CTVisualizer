Shader "UnityCTVisualizer/DVR_outofcore"
{
    Properties
	{
        // this is a Texture2DArray of slices of block (granular) densities that constitute the original 3D volume.
	    [NoScaleOffset] _BricksCache("Density Bricks Cache", 2DArray) = "" {}
        [NoScaleOffset] _TFColors("Transfer Function Colors Texture", 2D) = "" {}
        [HideInInspector] _NbrBricksX("Number of bricks along the X axis of the volume", int) = -1
        [HideInInspector] _NbrBricksY("Number of bricks along the Y axis of the volume", int) = -1
        [HideInInspector] _NbrBricksZ("Number of bricks along the Z axis of the volume", int) = -1
		_AlphaCutoff("Opacity Cutoff", Range(0.0, 1.0)) = 0.95
        _MaxIterations("Maximum number of samples to take along longest path (cube diagonal)", int) = 512
	}

	SubShader {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 500
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
		    CGPROGRAM
            #pragma require 2darray
			#pragma vertex vert
			#pragma fragment frag
            #include "UnityCG.cginc"

            // Visualization Parameters
            #define MAX_ITERATIONS 512
            #define BLOCK_SIZE 64
            #define DENSITIES_DEPTH_16
            float _AlphaCutoff;
            
            UNITY_DECLARE_TEX2DARRAY(_BrickDensities);  // Brick Cache
            float _NbrBricksX;
            float _NbrBricksY;
            float _NbrBricksZ;
            float3 _DensitiesTexArrayDims;
            sampler2D _TFColors;

            #define BOUNDING_BOX_LONGEST_SEGMENT 1.732050808f  // diagonal of a cube
            #define STEP_SIZE 0.003382912f  // = (LONGEST_SEGMET / MAX_ITERATIONS)
            
            /// <summary>
            ///     Maximum depth of the residency octree. This in turn determines the size
            ///     of the corresponding octree array.
            /// </summary>
            #define MAX_OCTREE_DEPTH 5
            #define FLATTENED_OCTREE_ARRAY_SIZE 10 // == 1 + MAX_OCTREE_DEPTH

            /// <summary>
            ///
            ///     Residency octree node. Stores transfer-function independent metadata and per-volume
            ///     cache residency information.
            ///
            ///
            ///                         ROOT                    <-------- depth 0
            ///                          |
            ///        +----+----+----+--+--+----+----+----+
            ///        |    |    |    |     |    |    |    |
            ///       A0    A1  ...   |    ...  ...  ...  A7    <-------- depth 1
            ///                       |
            ///     +----+----+----+--+--+----+----+----+
            ///     |    |    |    |     |    |    |    |
            ///    ...  ...  ...  ...   ...  ...  ...  ...      <-------- depth 2
            ///
            ///
            ///     Each node has at most 8 children nodes. The octree data structure is implemented as
            ///     an array in the shader (because shaders don't support pointers).
            ///     
            ///     Size of the array representation is: sum(8^i) over i from 0 to MAX_OCTREE_DEPTH
            ///     The smallest volume subdivision is (unit: voxels): VOLUME_DIMENSION * 2^(-MAX_OCTREE_DEPTH)
            ///
            ///     Reagardless of the color depth, resolution residency bitmask is 16 bits - therefore
            ///     maximum levels of resolution is 16.
            ///
            /// </summary>
            /// <example>
            ///
            ///     For a volume of the size 6000^3 (8-bit 216 GBs raw uncompressed size):
            ///
            ///         For MAX_OCTREE_DEPTH = 10 we get the following properties of the array representation:
            ///             - number of elements: 1 227 133 513 (definitely not ok)
            ///             - smallest subdivision: 5.859375 voxels (definitely not ok)
            ///
            ///         For MAX_OCTREE_DEPTH = 8 we get:
            ///             - number of elements: 19 173 961 (not ok)
            ///             - smallest subdivision: 23.4375 voxels (not ok)
            ///
            ///         For MAX_OCTREE_DEPTH = 7 we get:
            ///             - number of elements: 2 396 745 (ok-ish)
            ///             - array size (4 bytes struct): 9.59 MBs (ok)
            ///             - smallest subdivision:  voxels (ok-ish)
            ///
            ///         For MAX_OCTREE_DEPTH = 6 we get:
            ///             - number of elements: 299 593 (ok)
            ///             - array size (4 bytes struct): 1,12 MBs (very nice)
            ///             - smallest subdivision: 93.75 voxels (ok)
            /// 
            ///         For MAX_OCTREE_DEPTH = 5 we get:
            ///             - number of elements: 37 449 (very nice)
            ///             - array size (4 bytes struct): 0.15 MBs (vety nice)
            ///             - smallest subdivision: 187.5 voxels (ok-ish)
            ///
            ///     Looking at these examples it seems impractical to set MAX_OCTREE_DEPTH to anythin above 7
            ///     The reasonable candidates seem to be: 7, 6, and 5
            ///
            /// </example>
            struct ResidencyNode {
                half min;
                half max;
                uint resolutionsresidency;
            };
            uint ResidencyOctreeArraySize;
            RWStructuredBuffer<ResidencyNode> ResidencyOctree: register(u1);

            /// <summary>
            ///     Parametric description of a ray: r = origin + t * direction
            /// </summary>
            struct Ray {
                float3 origin;
                float3 dir;
            };

            /// <summary>
            ///     Samples the one dimensional transfer function texture for
            ///     color and alpha.
            /// </summary>
            float4 sampleTF1DColor(float density) {
                return tex2Dlod(_TFColors, float4(density, 0.0f, 0.0f, 0.0f));
            }

            /// <summary>
            ///     Uses viewing parameters to determine the desired resolution level
            ///     for the provided sample point. 0 corresponds to original resolution.
            /// </summary>
            int chooseDesiredResolutionLevel(float3 p) {
                return 0;
            }

            
            /// <summary>
            ///     Samples density from the brick cache 3D texture
            ///     slices corresponding to the bottom left block of the original volume:
            ///
            /// </summary>
            /// <remark>
            ///     Original volume's slices should be arranged along the Y-axis.
            /// </remark>
            float4 sampleDensity(float3 volumeCoord, float3 texArraySize)
            {
                float brickID = floor(volumeCoord.x * _NbrBlocksX) floor(volumeCoord.y * _NbrBlocksY);
                // is brick in cache?
                float s_00 = UNITY_SAMPLE_TEX2DARRAY_LOD(_BrickDensities, float3(, , arrayTexIndex), 0);
                float s_01 = UNITY_SAMPLE_TEX2DARRAY_LOD(_BrickDensities, float3(, , arrayTexIndex), 0);
                return lerp(s_00, s_01, t);
            }

            
            /// <summary>
            ///     Axis-Aligned Bounding Box (AABB) box. An AABB only needs
            ///     coordinate of its two corner points to fully describe it.
            /// </summary>
            struct Box {
                float3 min;
                float3 max;
            };

            // TODO: Does this handle NaN edge cases properly?
            /// <summary>
            ///     Branch-free and efficient(?) Axis-Aligned Bounding Box (AABB)
            ///     intersection algorithm. For an in-depth explanation, visit:
            ///     https://tavianator.com/2022/ray_box_boundary.html
            /// </summary>
            /// <remark>
            ///     This version may result in artifacts around the corners of the AABB.
            /// </remark>
            void slabs(Ray r, Box b, out float t_in, out float t_out) {
                float3 inverseDir = 1.0f / r.dir;
                float3 t0 = (b.min - r.origin) * inverseDir;
                float3 t1 = (b.max - r.origin) * inverseDir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);
                t_in = max(max(tmin.x, tmin.y), tmin.z);
                t_out = min(min(tmax.x, tmax.y), tmax.z);
            }
            

            float3 getRayDirection(float3 objectSpaceVertex) {
                // means orthogonal camera mode. Here every ray has the same direction
                if (unity_OrthoParams.w > 0) {
                    float3 cameraWorldDir = mul((float3x3)unity_CameraToWorld, float3(0.0f, 0.0f, 1.0f));
                    return normalize(mul(unity_WorldToObject, cameraWorldDir));
                }
                // means perspective
                return normalize(-ObjSpaceViewDir(float4(objectSpaceVertex, 0.0f)));
            }
            
            struct v2f
            {
                float4 clipVertex : SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 modelVertex : TEXCOORD1;
            };
            
            /// <summary>
            ///     Vertex shader: gets called once per vertex. In our DVR usecase
            ///     the volume is being drawn inside a Cube mesh.
            /// </summary>
            v2f vert(float4 modelVertex: POSITION, float2 uv: TEXCOORD0)
            {
                v2f output;
                output.clipVertex = UnityObjectToClipPos(modelVertex);
                output.uv = uv;
                output.modelVertex = modelVertex.xyz;
                return output;
            }
            
            fixed4 frag(v2f interpolated) : SV_Target
            {
                // initialize a ray in model space
                Ray ray;
                ray.origin = interpolated.modelVertex;
                ray.dir = getRayDirection(interpolated.modelVertex);
                // initialize the axis-aligned bounding box (AABB)
                Box aabb;
                aabb.min = float3(-0.5f, -0.5f, -0.5f);
                aabb.max = float3(0.5f, 0.5f, 0.5f);
                float t_in;
                float t_out;
                slabs(ray, aabb, t_in, t_out);  // intersect ray with the AABB and get parametric start and end values
                t_in = max(0.0f, t_in);  // in case we're inside the volume, t_in might be negative
                float3 start = ray.origin;
                // distance of segment intersecting with AABB volume
                // TODO: do we need abs here?
                float seg_len = abs(t_out - t_in);
                int num_iterations = (int)clamp(seg_len / STEP_SIZE, 1, MAX_ITERATIONS);
                float3 delta_step = ray.dir * STEP_SIZE;
                float4 accm_color = float4(0.0f, 0.0f, 0.0f, 0.0f);
                float3 accm_ray = start;
                for (int iter = 0; iter < num_iterations; ++iter)
                {
                    float sampled_density;
                    float4 src;
                    int l = chooseDesiredResolutionLevel(accm_ray + 0.5f);  // currently not used
                    sampled_density = sampleDensity(accm_ray + 0.5f, _DensitiesTexSize);  // map [-0.5, 0.5] range to [0.0, 1.0]
                    src = sampleTF1DColor(sampled_density);
                    // blending and compositing
                    src.rgb *= src.a;
                    accm_color = (1.0f - accm_color.a) * src + accm_color;
                    accm_ray += delta_step;
                    // early-ray-termination optimization technique
                    if (accm_color.a > _AlphaCutoff) break;
                }
                return accm_color;
            }
			ENDCG
		}
	}
}

