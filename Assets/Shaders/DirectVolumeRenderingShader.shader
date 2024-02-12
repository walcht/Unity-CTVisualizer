Shader "UnityCTVisualizer/DirectVolumeRenderingShader"
{
    Properties
	{
	    [NoScaleOffset] _Densities("Densities", 3D) = "" {}
        [NoScaleOffset] _TFColors("Transfer Function Colors Texture", 2D) = "" {}
		_AlphaCutoff("Opacity Cutoff", Range(0.0, 1.0)) = 0.95
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
            #pragma multi_compile NEAREST_NEIGHBOR TRICUBIC_PRE_CLASSIFICATION TRICUBIC_POST_CLASSIFICATION
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

            ////
            /// Copyright (c) 2008-2009, Danny Ruijters. All rights reserved.
            /// http://www.dannyruijters.nl/cubicinterpolation/
            /// This file is part of CUDA Cubic B-Spline Interpolation (CI).
            /// 
            /// Redistribution and use in source and binary forms, with or without
            /// modification, are permitted provided that the following conditions are met:
            /// *  Redistributions of source code must retain the above copyright
            ///    notice, this list of conditions and the following disclaimer.
            /// *  Redistributions in binary form must reproduce the above copyright
            ///    notice, this list of conditions and the following disclaimer in the
            ///    documentation and/or other materials provided with the distribution.
            /// *  Neither the name of the copyright holders nor the names of its
            ///    contributors may be used to endorse or promote products derived from
            ///    this software without specific prior written permission.
            /// 
            /// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
            /// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
            /// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
            /// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
            /// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
            /// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
            /// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
            /// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
            /// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
            /// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
            /// POSSIBILITY OF SUCH DAMAGE.
            /// 
            /// The views and conclusions contained in the software and documentation are
            /// those of the authors and should not be interpreted as representing official
            /// policies, either expressed or implied.
            /// 
            /// When using this code in a scientific project, please cite one or all of the
            /// following papers:
            /// *  Daniel Ruijters and Philippe Th�venaz,
            ///    GPU Prefilter for Accurate Cubic B-Spline Interpolation, 
            ///    The Computer Journal, vol. 55, no. 1, pp. 15-20, January 2012.
            ///    http://dannyruijters.nl/docs/cudaPrefilter3.pdf
            /// *  Daniel Ruijters, Bart M. ter Haar Romeny, and Paul Suetens,
            ///    Efficient GPU-Based Texture Interpolation using Uniform B-Splines,
            ///    Journal of Graphics Tools, vol. 13, no. 4, pp. 61-69, 2008.
            ///
            // TODO: re-implement this. Understand it and make it faster (if possible)
            float4 interpolateTricubic(sampler3D tex, float3 texCoord, float3 texSize)
            {
            	// shift the coordinate from [0,1] to [-0.5, texSize-0.5]
            	float3 coord_grid = texCoord * texSize - 0.5;
            	float3 index = floor(coord_grid);
            	float3 fraction = coord_grid - index;
            	float3 one_frac = 1.0 - fraction;
            
            	float3 w0 = 1.0/6.0 * one_frac*one_frac*one_frac;
            	float3 w1 = 2.0/3.0 - 0.5 * fraction*fraction*(2.0-fraction);
            	float3 w2 = 2.0/3.0 - 0.5 * one_frac*one_frac*(2.0-one_frac);
            	float3 w3 = 1.0/6.0 * fraction*fraction*fraction;
            
            	float3 g0 = w0 + w1;
            	float3 g1 = w2 + w3;
            	float3 mult = 1.0 / texSize;
            	float3 h0 = mult * ((w1 / g0) - 0.5 + index);  //h0 = w1/g0 - 1, move from [-0.5, texSize-0.5] to [0,1]
            	float3 h1 = mult * ((w3 / g1) + 1.5 + index);  //h1 = w3/g1 + 1, move from [-0.5, texSize-0.5] to [0,1]
            
            	// fetch the eight linear interpolations
            	// weighting and fetching is interleaved for performance and stability reasons
            	float4 tex000 = tex3Dlod(tex, float4(h0, 0.0));
            	float4 tex100 = tex3Dlod(tex, float4(h1.x, h0.y, h0.z, 0.0));
            	tex000 = lerp(tex100, tex000, g0.x);  //weigh along the x-direction
            	float4 tex010 = tex3Dlod(tex, float4(h0.x, h1.y, h0.z, 0.0));
            	float4 tex110 = tex3Dlod(tex, float4(h1.x, h1.y, h0.z, 0.0));
            	tex010 = lerp(tex110, tex010, g0.x);  //weigh along the x-direction
            	tex000 = lerp(tex010, tex000, g0.y);  //weigh along the y-direction
            	float4 tex001 = tex3Dlod(tex, float4(h0.x, h0.y, h1.z, 0.0));
            	float4 tex101 = tex3Dlod(tex, float4(h1.x, h0.y, h1.z, 0.0));
            	tex001 = lerp(tex101, tex001, g0.x);  //weigh along the x-direction
            	float4 tex011 = tex3Dlod(tex, float4(h0.x, h1.y, h1.z, 0.0));
            	float4 tex111 = tex3Dlod(tex, float4(h1, 0.0));
            	tex011 = lerp(tex111, tex011, g0.x);  //weigh along the x-direction
            	tex001 = lerp(tex011, tex001, g0.y);  //weigh along the y-direction
            
            	return lerp(tex001, tex000, g0.z);  //weigh along the z-direction
            }

            float4 interpolateTricubicColor(sampler3D tex, float3 texCoord, float3 texSize)
            {
            	// shift the coordinate from [0,1] to [-0.5, texSize-0.5]
            	float3 coord_grid = texCoord * texSize - 0.5;
            	float3 index = floor(coord_grid);
            	float3 fraction = coord_grid - index;
            	float3 one_frac = 1.0 - fraction;
            
            	float3 w0 = 1.0/6.0 * one_frac*one_frac*one_frac;
            	float3 w1 = 2.0/3.0 - 0.5 * fraction*fraction*(2.0-fraction);
            	float3 w2 = 2.0/3.0 - 0.5 * one_frac*one_frac*(2.0-one_frac);
            	float3 w3 = 1.0/6.0 * fraction*fraction*fraction;
            
            	float3 g0 = w0 + w1;
            	float3 g1 = w2 + w3;
            	float3 mult = 1.0 / texSize;
            	float3 h0 = mult * ((w1 / g0) - 0.5 + index);  //h0 = w1/g0 - 1, move from [-0.5, texSize-0.5] to [0,1]
            	float3 h1 = mult * ((w3 / g1) + 1.5 + index);  //h1 = w3/g1 + 1, move from [-0.5, texSize-0.5] to [0,1]
            
            	// fetch the eight linear interpolations
            	// weighting and fetching is interleaved for performance and stability reasons
            	float4 col000 = sampleTF1DColor(tex3Dlod(tex, float4(h0, 0.0)));
            	float4 col100 = sampleTF1DColor(tex3Dlod(tex, float4(h1.x, h0.y, h0.z, 0.0)));
            	col000 = lerp(col100, col000, g0.x);  //weigh along the x-direction
            	float4 col010 = sampleTF1DColor(tex3Dlod(tex, float4(h0.x, h1.y, h0.z, 0.0)));
            	float4 col110 = sampleTF1DColor(tex3Dlod(tex, float4(h1.x, h1.y, h0.z, 0.0)));
            	col010 = lerp(col110, col010, g0.x);  //weigh along the x-direction
            	col000 = lerp(col010, col000, g0.y);  //weigh along the y-direction
            	float4 col001 = sampleTF1DColor(tex3Dlod(tex, float4(h0.x, h0.y, h1.z, 0.0)));
            	float4 col101 = sampleTF1DColor(tex3Dlod(tex, float4(h1.x, h0.y, h1.z, 0.0)));
            	col001 = lerp(col101, col001, g0.x);  //weigh along the x-direction
            	float4 col011 = sampleTF1DColor(tex3Dlod(tex, float4(h0.x, h1.y, h1.z, 0.0)));
            	float4 col111 = sampleTF1DColor(tex3Dlod(tex, float4(h1, 0.0)));
            	col011 = lerp(col111, col011, g0.x);  //weigh along the x-direction
            	col001 = lerp(col011, col001, g0.y);  //weigh along the y-direction
            
            	return lerp(col001, col000, g0.z);  //weigh along the z-direction
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
                    if (TRICUBIC_PRE_CLASSIFICATION)
                    {
                        sampled_density = interpolateTricubic(_Densities, accm_ray + 0.5f, _DensitiesTexSize);
                        src = sampleTF1DColor(sampled_density);
                    }
                    // TODO: is this interpolation correct? Why is it slow af?
                    else if (TRICUBIC_POST_CLASSIFICATION) 
                    {
                        src = interpolateTricubicColor(_Densities, accm_ray + 0.5f, _DensitiesTexSize);
                    }
                    else  // NEAREST_NEIGHBOR
                    {
                        sampled_density = tex3Dlod(_Densities, float4(accm_ray + 0.5f, 0.0f)).r;
                        src = sampleTF1DColor(sampled_density);
                    }
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

