Shader "UnityCTVisualizer/DirectVolumeRenderingShader"
{
    Properties
	{
	    [NoScaleOffset] _Densities("Densities", 3D) = "" {}
        [NoScaleOffset] _TFColors("Transfer Function Colors Texture", 2D) = "" {}
		_AlphaCutoff("Opacity Cutoff", Range(0.0, 1.0)) = 0.95
	}

	SubShader {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 500
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
		    CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #include "UnityCG.cginc"

            #define MAX_ITERATIONS 256
            #define BOUNDING_BOX_LONGEST_SEGMENT 1.732050808f  // diagonal of a cube
            #define STEP_SIZE 0.006765823f  // = (LONGEST_SEGMET / MAX_ITERATIONS)
            
            sampler3D _Densities;
            sampler2D _TFColors;
            float _AlphaCutoff;

            /// <summary>
            ///     Parametric description of a ray: r = origin + t * direction
            /// </summary>
            struct Ray {
                float3 origin;
                float3 dir;
            };
            
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
            void slabs(Ray r, Box b, out float t_in, out float t_out) {
                float3 inverseDir = 1.0f / r.dir;
                float3 t0 = (b.min - r.origin) * inverseDir;
                float3 t1 = (b.max - r.origin) * inverseDir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);
                t_in = max(max(tmin.x, tmin.y), tmin.z);
                t_out = min(min(tmax.x, tmax.y), tmax.z);
            }
            
            /// <summary>
            ///     Samples the one dimensional transfer function texture for
            ///     color and alpha.
            /// </summary>
            float4 sampleTF1DColor(float density) {
                return tex2Dlod(_TFColors, float4(density, 0.0f, 0.0f, 0.0f));
            }

            float3 getRayDirection(float3 objectSpaceVertex) {
                if (unity_OrthoParams.w > 0) {
                    // means orthogonal camera mode. Here every ray has the same direction
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
            ///     Vertex shader: gets called once per vertex. In our DVR use
            ///     case this should get called 8 times because the volume is
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
                Ray ray;
                ray.origin = interpolated.modelVertex;
                ray.dir = getRayDirection(interpolated.modelVertex);
                // TODO:    this is a uniform! Maybe don't do it in the fragment
                //          shader?
                // initialize the axis-aligned bounding box (AABB)
                Box aabb;
                aabb.min = float3(-0.5f, -0.5f, -0.5f);
                aabb.max = float3(0.5f, 0.5f, 0.5f);
                // intersect ray with the AABB and get parametric start and end values
                float t_in;
                float t_out;
                slabs(ray, aabb, t_in, t_out);
                // in case we're inside the volume, t_in might be negative
                t_in = max(0.0f, t_in);
                float3 start = ray.origin;
                // distance of segment intersecting with AABB volume
                // TODO: do we need abs here?
                float seg_len = abs(t_out - t_in);
                int num_iterations = (int)clamp(seg_len / STEP_SIZE, 1, MAX_ITERATIONS);
                float3 delta_step = ray.dir * STEP_SIZE;
                float4 accm_color = float4(0.0f, 0.0f, 0.0f, 0.0f);
                float3 accm_ray = start;
                // TODO:    improve sampling loop (maybe use unroll with outside check?)
                for (int iter = 0; iter < num_iterations; iter++)
                {
                    // don't forget to transition from [-0.5, 0.5] range to [0.0, 1.0]
                    float sampled_density = tex3Dlod(_Densities, float4(accm_ray + 0.5f, 0.0f)).r;
                    // apply transfer function
                    float4 src = sampleTF1DColor(sampled_density);
                    // blending
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

