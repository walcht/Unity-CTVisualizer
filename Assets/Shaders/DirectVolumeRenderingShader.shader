Shader "UnityCTVisualizer/DirectVolumeRenderingShader"
{
    Properties
	{
	    [NoScaleOffset] _Densities("Densities", 3D) = "" {}
        [NoScaleOffset] _TFColors("Transfer Function Colors Texture", 2D) = "" {}
		_AlphaCutoff("Opacity Cutoff", Range(0.0, 1.0)) = 0.95
        _MaxIterations("Maximum number of samples to take along longest path (cube diagonal)", int) = 512
        [HideInInspector] _DensitiesTexSize("Densities 3D texture dimensions", Vector) = (1, 1, 1)
	}

	SubShader {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 500
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
		    CGPROGRAM
            #pragma multi_compile NEAREST_NEIGHBOR TRILINEAR_PRE_CLASSIFICATION TRILINEAR_POST_CLASSIFICATION
			#pragma vertex vert
			#pragma fragment frag
            #include "UnityCG.cginc"

            #define MAX_ITERATIONS 512
            #define BOUNDING_BOX_LONGEST_SEGMENT 1.732050808f  // diagonal of a cube
            #define STEP_SIZE 0.003382912f  // = (LONGEST_SEGMET / MAX_ITERATIONS)
            
            sampler3D _Densities;
            sampler2D _TFColors;
            float _AlphaCutoff;
            float3 _DensitiesTexSize;

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

            /// 
            /// <summary>
            ///     Converts RGB color to HSV space with no changes to the alpha component.
            ///     Code copied and modified from: https://stackoverflow.com/questions/15095909/from-rgb-to-hsv-in-opengl-glsl
            /// </summary>
            float4 rgb2hsv(float4 c)
            {
                float4 K = float4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float4(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x, c.a);
            }
            
            /// <summary>
            ///     Converts HSV color to RGB space with no changes to the alpha component.
            ///     Code copied and modified from: https://stackoverflow.com/questions/15095909/from-rgb-to-hsv-in-opengl-glsl
            /// </summary>
            float4 hsv2rgb(float4 c)
            {
                float4 K = float4(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0f - K.www);
                return float4(c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0f, 1.0f), c.y), c.a);
            }

            
            /// for reference: https://paulbourke.net/miscellaneous/interpolation/
            ///
            ///                          Y
            ///         Z              ↗
            ///        ↑   .+------- c1
            ///          .' |      .'|
            ///         +---+----+'  |
            ///         |   |    |   |
            ///         |  ,+----+---+ 
            ///         |.'      | .' 
            ///        c0--------+' ⟶  X
            ///
            /// <summary>
            ///     Trilinear interpolation. Use this for pre-classification interpolation (i.e.,
            ///     interpolate density then classify using TF lookup texture. This method is expensive.
            /// </summary>
            float4 interpolateTrilinear(sampler3D tex, float3 texCoord, float3 texSize)
            {
            	// shift the coordinate from [0,1] to [-0.5, texSize-0.5]
            	float3 coord_grid = texCoord * texSize - 0.5f;
            	float3 index = floor(coord_grid);
                // v is value withing the 8 vertices (cube) is used for interpolation
            	float3 v = coord_grid - index;
                float3 one_minus_v = 1.0f - v;
                float3 one_over_texsize = 1.0f / texSize;
                // the values at the vertices are needed. Since this is an AABB, only the 2 corners c0 and c1
                // need to be determined
                float3 c0 = one_over_texsize * (index + 0.5f);
                float3 c1 = one_over_texsize * (index + 1.5f);
            	float4 V_000 = tex3Dlod(tex, float4(c0, 0.0f));
            	float4 V_100 = tex3Dlod(tex, float4(c1.x, c0.y, c0.z, 0.0f));
            	float4 V_010 = tex3Dlod(tex, float4(c0.x, c1.y, c0.z, 0.0f));
            	float4 V_110 = tex3Dlod(tex, float4(c1.x, c1.y, c0.z, 0.0f));
            	float4 V_001 = tex3Dlod(tex, float4(c0.x, c0.y, c1.z, 0.0f));
            	float4 V_101 = tex3Dlod(tex, float4(c1.x, c0.y, c1.z, 0.0f));
            	float4 V_011 = tex3Dlod(tex, float4(c0.x, c1.y, c1.z, 0.0f));
            	float4 V_111 = tex3Dlod(tex, float4(c1, 0.0f));
                return  V_000 * one_minus_v.x * one_minus_v.y * one_minus_v.z +
                        V_100 * v.x * one_minus_v.y * one_minus_v.z +
                        V_010 * one_minus_v.x * v.y * one_minus_v.z +
                        V_001 * one_minus_v.x * one_minus_v.y * v.z +
                        V_101 * v.x * one_minus_v.y * v.z +
                        V_011 * one_minus_v.x * v.y * v.z +
                        V_110 * v.x * v.y * one_minus_v.z +
                        V_111 * v.x * v.y * v.z;
            
            }
            
            // TODO:    this piece of junk is unoptimized af. Please improve this garbage. Nvm I think this
            //          will always yield worse results
            // NOTE: For more accurate interpolations, we convert color from RGB space to HSL
            /// <summary>
            ///     Same as translateTrilinear except that colors sampled from the provided transfer function
            ///     are sampled instead of densities. This function is even more expensive because double the
            ///     sampling operations are used (16 vs. 8)
            /// </summary>
            /// <remark>
            ///     
            /// </remark>
            float4 interpolateTrilinearColor(sampler3D tex, float3 texCoord, float3 texSize)
            {
            	float3 coord_grid = texCoord * texSize - 0.5;
            	float3 index = floor(coord_grid);
                // v is value withing the 8 vertices (cube) is used for interpolation
            	float3 v = coord_grid - index;
                float3 one_minus_v = 1.0f - v;
                float3 one_over_texsize = 1.0f / texSize;
                // the values at the vertices are needed. Since this is an AABB, only the 2 corners c0 and c1
                // need to be determined
                float3 c0 = one_over_texsize * (index + 0.5f);
                float3 c1 = one_over_texsize * (index + 1.5f);
                // do the linear interpolation in HSV space
            	float4 V_000 = rgb2hsv(sampleTF1DColor(tex3Dlod(tex, float4(c0, 0.0f))));
            	float4 V_100 = rgb2hsv(sampleTF1DColor(tex3Dlod(tex, float4(c1.x, c0.y, c0.z, 0.0f))));
            	float4 V_010 = rgb2hsv(sampleTF1DColor(tex3Dlod(tex, float4(c0.x, c1.y, c0.z, 0.0f))));
            	float4 V_110 = rgb2hsv(sampleTF1DColor(tex3Dlod(tex, float4(c1.x, c1.y, c0.z, 0.0f))));
            	float4 V_001 = rgb2hsv(sampleTF1DColor(tex3Dlod(tex, float4(c0.x, c0.y, c1.z, 0.0f))));
            	float4 V_101 = rgb2hsv(sampleTF1DColor(tex3Dlod(tex, float4(c1.x, c0.y, c1.z, 0.0f))));
            	float4 V_011 = rgb2hsv(sampleTF1DColor(tex3Dlod(tex, float4(c0.x, c1.y, c1.z, 0.0f))));
            	float4 V_111 = rgb2hsv(sampleTF1DColor(tex3Dlod(tex, float4(c1, 0.0f))));
                // then convert back to RGB space
                return  hsv2rgb(
                            V_000 * one_minus_v.x * one_minus_v.y * one_minus_v.z +
                            V_100 * v.x * one_minus_v.y * one_minus_v.z +
                            V_010 * one_minus_v.x * v.y * one_minus_v.z +
                            V_001 * one_minus_v.x * one_minus_v.y * v.z +
                            V_101 * v.x * one_minus_v.y * v.z +
                            V_011 * one_minus_v.x * v.y * v.z +
                            V_110 * v.x * v.y * one_minus_v.z +
                            V_111 * v.x * v.y * v.z
                        );
            }

            /// <summary>
            ///     Tricubic interpolation. Use this for pre-classification interpolation (i.e.,
            ///     interpolate density then classify using TF lookup texture. This method is more
            ///     expensive than interpolateTrilinear.
            /// </summary>
            float4 interpolateTricubic(sampler3D tex, float3 texCoord, float3 texSize)
            {
            	// shift the coordinate from [0,1] to [-0.5, texSize-0.5]
            	float3 coord_grid = texCoord * texSize - 0.5f;
            	float3 index = floor(coord_grid);
                // v is value withing the 8 vertices (cube) is used for interpolation
            	float3 v = coord_grid - index;
                float3 one_minus_v = 1.0f - v;
                float3 one_over_texsize = 1.0f / texSize;
                // the values at the vertices are needed. Since this is an AABB, only the 2 corners c0 and c1
                // need to be determined
                float3 c0 = one_over_texsize * (index + 0.5f);
                float3 c1 = one_over_texsize * (index + 1.5f);
            	float4 V_000 = tex3Dlod(tex, float4(c0, 0.0f));
            	float4 V_100 = tex3Dlod(tex, float4(c1.x, c0.y, c0.z, 0.0f));
            	float4 V_010 = tex3Dlod(tex, float4(c0.x, c1.y, c0.z, 0.0f));
            	float4 V_110 = tex3Dlod(tex, float4(c1.x, c1.y, c0.z, 0.0f));
            	float4 V_001 = tex3Dlod(tex, float4(c0.x, c0.y, c1.z, 0.0f));
            	float4 V_101 = tex3Dlod(tex, float4(c1.x, c0.y, c1.z, 0.0f));
            	float4 V_011 = tex3Dlod(tex, float4(c0.x, c1.y, c1.z, 0.0f));
            	float4 V_111 = tex3Dlod(tex, float4(c1, 0.0f));
                return  V_000 * one_minus_v.x * one_minus_v.y * one_minus_v.z +
                        V_100 * v.x * one_minus_v.y * one_minus_v.z +
                        V_010 * one_minus_v.x * v.y * one_minus_v.z +
                        V_001 * one_minus_v.x * one_minus_v.y * v.z +
                        V_101 * v.x * one_minus_v.y * v.z +
                        V_011 * one_minus_v.x * v.y * v.z +
                        V_110 * v.x * v.y * one_minus_v.z +
                        V_111 * v.x * v.y * v.z;
            
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
                    float sampled_density;
                    float4 src;
                    if (NEAREST_NEIGHBOR)
                    {
                        sampled_density = tex3Dlod(_Densities, float4(accm_ray + 0.5f, 0.0f)).r;
                        src = sampleTF1DColor(sampled_density);
                    }
                    else if (TRILINEAR_PRE_CLASSIFICATION)
                    {
                        sampled_density = interpolateTrilinear(_Densities, accm_ray + 0.5f, _DensitiesTexSize);
                        src = sampleTF1DColor(sampled_density);
                    }
                    // TODO: is this interpolation correct? Why is it slow af?
                    else if (TRILINEAR_POST_CLASSIFICATION) 
                    {
                        src = interpolateTrilinearColor(_Densities, accm_ray + 0.5f, _DensitiesTexSize);
                    }
                    // blending
                    // TODO:    is direct sampling of alpha the correct method? Check what this is doing:
                    //          https://github.com/LDeakin/VkVolume/blob/master/shaders/volume_render.frag
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

