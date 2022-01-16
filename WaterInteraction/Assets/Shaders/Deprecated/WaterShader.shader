//With help from https://forum.unity.com/threads/modifying-vertex-fragment-shader-so-it-works-with-lighting-lightmapping.375250/
Shader "WaterInteraction/WaterShader"
{
	Properties
	{
		_MainTex("TextureMain", 2D) = "white" {}
		_Color("Color", Color) = (1, 1, 1, 1)
		[HideInInspector] _InputMap("InputData", 2D) = "black"{}
		_SpecColor("Specular Color", Color) = (1.0,1.0,1.0,1.0)
		_Shininess("Shininess", Float) = 10
	}
	SubShader
	{
		Pass{
			Tags {"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"


			//user defined variables
			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform float4 _Color;
			sampler2D _InputMap; //float4:: x,y Velocity || z density
			uniform float4 _SpecColor;
			uniform float _Shininess;


			//unity defined variables
			uniform float4 _LightColor0;

			//base input structs
			struct vertexInput {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
			};

			struct vertexOutput {
				float4 pos : SV_POSITION;
				float4 tex : TEXCOORD0;
				float4 posWorld : TEXCOORD1;
				float3 normalDir : TEXCOORD2;
			};

			vertexOutput vert(vertexInput v) {
				vertexOutput o;

				//acces texture
				//float4 input = tex2Dlod(_InputMap, float4(v.texcoord, 0, 0));
				//v.vertex.z += input.z * 0.1f;

				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				o.normalDir = normalize(mul(float4(v.normal, 0.0), unity_WorldToObject).xyz);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.tex = v.texcoord;

				return o;
			}

			fixed4 frag(vertexOutput i) : SV_Target
			{
				float3 normalDirection = i.normalDir;
				float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
				float3 lightDirection;
				float atten;

				if (_WorldSpaceLightPos0.w == 0.0) { //directional light
					atten = 1.0;
					lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				}
				else {
					 float3 fragmentToLightSource = _WorldSpaceLightPos0.xyz - i.posWorld.xyz;
					float distance = length(fragmentToLightSource);
					atten = 1.0 / distance;
					lightDirection = normalize(fragmentToLightSource);
				}

				//Lighting
				float3 diffuseReflection = atten * _LightColor0.xyz * saturate(dot(normalDirection, lightDirection));
				float3 specularReflection = diffuseReflection * _SpecColor.xyz * pow(saturate(dot(reflect(-lightDirection, normalDirection), viewDirection)) , _Shininess);

				float3 lightFinal = UNITY_LIGHTMODEL_AMBIENT.xyz + diffuseReflection + specularReflection;// + rimLighting;

				//Texture Maps
				float4 tex = tex2D(_MainTex, i.tex.xy * _MainTex_ST.xy + _MainTex_ST.zw);

				return float4(tex.xyz * lightFinal * _Color.xyz, 1.0);
			}
			ENDCG
		}
	}
}

//
//        Properties{
//     _Color("Color Tint", Color) = (1.0,1.0,1.0,1.0)
//     _MainTex("Diffuse Texture", 2D) = "white" {}
//     _SpecColor("Specular Color", Color) = (1.0,1.0,1.0,1.0)
//     _Shininess("Shininess", Float) = 10
//            }
//                SubShader{
//                  Pass {
//                    Tags {"LightMode" = "ForwardBase"}
//                    CGPROGRAM
//                    #pragma vertex vert
//                    #pragma fragment frag
//
//         //user defined variables
//         uniform sampler2D _MainTex;
//         uniform float4 _MainTex_ST;
//         uniform float4 _Color;
//         uniform float4 _SpecColor;
//         uniform float _Shininess;
//
//         //unity defined variables
//         uniform float4 _LightColor0;
//
//         //base input structs
//         struct vertexInput {
//           float4 vertex : POSITION;
//           float3 normal : NORMAL;
//           float4 texcoord : TEXCOORD0;
//         };
//         struct vertexOutput {
//           float4 pos : SV_POSITION;
//           float4 tex : TEXCOORD0;
//           float4 posWorld : TEXCOORD1;
//           float3 normalDir : TEXCOORD2;
//         };
//
//         //vertex Function
//
//         vertexOutput vert(vertexInput v) {
//           vertexOutput o;
//
//           o.posWorld = mul(_Object2World, v.vertex);
//           o.normalDir = normalize(mul(float4(v.normal, 0.0), _World2Object).xyz);
//           o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
//           o.tex = v.texcoord;
//
//           return o;
//         }
//
//         //fragment function
//
//         float4 frag(vertexOutput i) : COLOR
//         {
//           float3 normalDirection = i.normalDir;
//           float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
//           float3 lightDirection;
//           float atten;
//
//           if (_WorldSpaceLightPos0.w == 0.0) { //directional light
//             atten = 1.0;
//             lightDirection = normalize(_WorldSpaceLightPos0.xyz);
//           }
//           else {
//             float3 fragmentToLightSource = _WorldSpaceLightPos0.xyz - i.posWorld.xyz;
//             float distance = length(fragmentToLightSource);
//             atten = 1.0 / distance;
//             lightDirection = normalize(fragmentToLightSource);
//           }
//
//           //Lighting
//           float3 diffuseReflection = atten * _LightColor0.xyz * saturate(dot(normalDirection, lightDirection));
//           float3 specularReflection = diffuseReflection * _SpecColor.xyz * pow(saturate(dot(reflect(-lightDirection, normalDirection), viewDirection)) , _Shininess);
//
//           float3 lightFinal = UNITY_LIGHTMODEL_AMBIENT.xyz + diffuseReflection + specularReflection;// + rimLighting;
//
//           //Texture Maps
//           float4 tex = tex2D(_MainTex, i.tex.xy * _MainTex_ST.xy + _MainTex_ST.zw);
//
//           return float4(tex.xyz * lightFinal * _Color.xyz, 1.0);
//         }
//
//         ENDCG
//
//       }
//
//
//     }
//}


//// Use shader model 3.0 target, to get nicer looking lighting
//#pragma target 3.0

//sampler2D _MainTex;

//struct Input
//{
//    float2 uv_MainTex;
//};

//half _Glossiness;
//half _Metallic;
//fixed4 _Color;

//// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
//// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
//// #pragma instancing_options assumeuniformscaling
//UNITY_INSTANCING_BUFFER_START(Props)
//    // put more per-instance properties here
//UNITY_INSTANCING_BUFFER_END(Props)

//void surf (Input IN, inout SurfaceOutputStandard o)
//{
//    // Albedo comes from a texture tinted by color
//    fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
//    o.Albedo = c.rgb;
//    // Metallic and smoothness come from slider variables
//    o.Metallic = _Metallic;
//    o.Smoothness = _Glossiness;
//    o.Alpha = c.a;
//}
//ENDCG
