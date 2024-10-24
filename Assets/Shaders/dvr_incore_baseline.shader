Shader "UnityCTVisualizer/dvr_incore_baseline"
{
    Properties
	{
        // without HideInInspector, significant performance penalties occur when viewing
        // a material with this shader in Unity Editor
	    [HideInInspector] _BrickCache("Brick Cache", 3D) = "" {}
        [HideInInspector] _TFColors("Transfer Function Colors Texture", 2D) = "" {}
		_AlphaCutoff("Opacity Cutoff", Range(0.0, 1.0)) = 0.95
        // should be an int, but Unity crashes when calling Material.SetInteger()
        // see: https://discussions.unity.com/t/crash-when-calling-material-setinteger-with-int-shaderlab-properties/891920
        _MaxIterations("Maximum number of samples to take along longest path (cube diagonal)", float) = 512
        [HideInInspector] _BrickCacheTexSize("Brick Cache 3D texture dimensions", Vector) = (1, 1, 1)
	}

	SubShader {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 500
		Cull Front
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
		    CGPROGRAM
            #pragma multi_compile TEXTURE_RESHAPED_OFF TEXTURE_RESHAPED_ON
			#pragma vertex vert
			#pragma fragment frag
            #include "UnityCG.cginc"

            #define BOUNDING_BOX_LONGEST_SEGMENT 1.732050808f  // diagonal of a cube
            
            sampler3D _BrickCache;
            sampler2D _TFColors;
            float _AlphaCutoff;
            float _MaxIterations;
            float3 _BrickCacheTexSize;

            /// <summary>
            ///     Parametric description of a ray: r = origin + t * direction
            /// </summary>
            struct Ray {
                // in object space shifted by (0.5, 0.5, 0.5) (i.e., reference is at some edge vertex of the normalized bounding cube)
                float3 origin;
                float3 dir;
                // t_in is 0
                float t_out;
            };

            /// <summary>
            ///     Samples the one dimensional transfer function texture for
            ///     color and alpha.
            /// </summary>
            float4 sampleTF1DColor(float density) {
                return tex2Dlod(_TFColors, float4(density, 0.0f, 0.0f, 0.0f));
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
            ///     Branch-free and efficient Axis-Aligned Bounding Box (AABB)
            ///     intersection algorithm. For an in-depth explanation, visit:
            ///     https://tavianator.com/2022/ray_box_boundary.html
            /// </summary>
            /// <remark>
            ///     This version may result in artifacts around the corners of
            ///     the AABB.
            /// </remark>
            float slabs(float3 origin, float3 dir, Box b) {
                float3 inverseDir = 1.0f / dir;
                float3 t0 = (b.min - origin) * inverseDir;
                float3 t1 = (b.max - origin) * inverseDir;
                float3 tmax = max(t0, t1);
                float t_out = min(min(tmax.x, tmax.y), tmax.z);
                return t_out;
            }


            ///
            ///     Process only backward-facing triangles
            ///     
            ///         .+--------+
            ///       .' |        |
            ///      +   |  x <---|---- fragment position in object space is the start of the
            ///      |   |        |     view ray towards the camera
            ///      |  ,+--------+
            ///      |.'        .'
            ///      +--------+'
            ///
            ///
            Ray getRayFromBackface(float3 modelVertex) {
                Ray ray;
                ray.origin = modelVertex + float3(0.5f, 0.5f, 0.5f);

                // get normalized ray direction in object space and in view space (i.e., camera space)
                float3 ray_dir_viewspace;
                float3 ray_origin_viewspace = UnityObjectToViewPos(modelVertex);
                if (unity_OrthoParams.w > 0) {
                    // orthogonal camera mode. Every ray has the same direction
                    ray_dir_viewspace = float3(0.0f, 0.0f, 1.0f);
                    float3 cameraWorldDir = mul((float3x3)unity_CameraToWorld, ray_dir_viewspace);
                    ray.dir = normalize(mul(unity_WorldToObject, cameraWorldDir));
                } else {
                    // perspective camera. Ray direction depends on interpolated model vertex position for this fragment
                    ray_dir_viewspace = -normalize(ray_origin_viewspace);
                    ray.dir = normalize(ObjSpaceViewDir(float4(modelVertex, 0.0f)));
                }

                // initialize the axis-aligned bounding box (AABB)
                Box aabb;
                aabb.min = float3(0.0f, 0.0f, 0.0f);
                aabb.max = float3(1.0f, 1.0f, 1.0f);
                // intersect ray with the AABB and get parametric start and end values
                ray.t_out = slabs(ray.origin, ray.dir, aabb);

                // t_out corresponds to the volume's AABB exit point but the exit point may be reached earlier if
                // the camera is inside the volume. t_frust corresponds to the frustrum exit
                float t_frust = -(ray_origin_viewspace.z + _ProjectionParams.y) / ray_dir_viewspace.z;
                ray.t_out = min(ray.t_out, t_frust);

                return ray;
            }

            Ray flipRay(Ray ray) {
                Ray flipped_ray;
                flipped_ray.origin = ray.origin + ray.t_out * ray.dir;
                flipped_ray.dir = -ray.dir;
                flipped_ray.t_out = ray.t_out;
                return flipped_ray;
            }
            
            struct v2f
            {
                float4 clipVertex : SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 modelVertex : TEXCOORD1;
            };
            
            /// <summary>
            ///     Vertex shader: gets called once per vertex. In our DVR use
            ///     case this should get called at least 8 times because the volume is
            ///     being drawn inside a Cube mesh.
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
                Ray ray = flipRay(getRayFromBackface(interpolated.modelVertex));
                // distance of segment intersecting with AABB volume
                float seg_len = ray.t_out;  // because t_in == 0
                float step_size = BOUNDING_BOX_LONGEST_SEGMENT / _MaxIterations;
                int num_iterations = (int)clamp(seg_len / step_size, 1, (int)_MaxIterations);
                float3 delta_step = ray.dir * step_size;
                float4 accm_color = float4(0.0f, 0.0f, 0.0f, 0.0f);
                float3 accm_ray = ray.origin;
                // TODO:    improve sampling loop (maybe use unroll with outside check?)
                for (int iter = 0; iter < num_iterations; ++iter)
                {
                    float sampled_density = tex3Dlod(_BrickCache, float4(accm_ray, 0.0f)).r;
                    float4 src = sampleTF1DColor(sampled_density);
                    // move to next sample point
                    accm_ray += delta_step;
                    // blending
                    src.rgb *= src.a;
                    accm_color = (1.0f - accm_color.a) * src + accm_color;
                    // early-ray-termination optimization technique
                    if (accm_color.a > _AlphaCutoff) break;
                }
                return accm_color;
            }
			ENDCG
		}
	}
}