Shader "Unlit/Clouds"
{
    Properties
    {
        cloudscale("cloudscale",float)=1.1
		speed("speed",float)=0.03
		clouddark("cloundark",float)=0.5
		cloudlight("cloudlight",float)=0.3
		cloudcover("cloudcover",float)=0.2
		cloudalpha("cloudalpha",float)=8.0
		skytint("skytint",float)=0.5

		skycolor1("skycolor1",color)=(0.2,0.4,0.6)
		skycolor2("skycolor2",color)=(0.4,0.7,1.0)

		m("m",vector)=(1.6,1.2,-1.2,1.6)
		st("st",vector)=(1.0,1.0,0.0,0.0)
    }


    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

			float cloudscale;
			float speed;
			float clouddark;
			float cloudlight;
			float cloudcover;
			float cloudalpha;
			float skytint;
			fixed4 skycolor1;
			fixed4 skycolor2;
			float4 st;

			matrix m;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

			//-----------------产生随机值-------------------
			float2 hash( float2 p ) {
	            p = float2(dot(p,float2(127.1,311.7)), dot(p,float2(269.5,183.3)));
	            return -1.0 + 2.0*frac(sin(p)*43758.5453123);
            }

			//-----------------noise噪声图------------------
			float noise( float2 p ) {
                float K1 = 0.366025404; // (sqrt(3)-1)/2;
                float K2 = 0.211324865; // (3-sqrt(3))/6;
	            float2 i = floor(p + (p.x+p.y)*K1);	
                float2 a = p - i + (i.x+i.y)*K2;
                float2 o = (a.x>a.y) ? float2(1.0,0.0) : float2(0.0,1.0); //float2 of = 0.5 + 0.5*float2(sign(a.x-a.y), sign(a.y-a.x));
                float2 b = a - o + K2;
	            float2 c = a - 1.0 + 2.0*K2;
                float3 h = max(0.5-float3(dot(a,a), dot(b,b), dot(c,c) ), 0.0 );
	            float3 n = h*h*h*h*float3( dot(a,hash(i+0.0)), dot(b,hash(i+o)), dot(c,hash(i+1.0)));
                return dot(n, float3(70.0,70.0,70.0));	
            }

			//---------------分型布朗运动函数（Fractional brownian motion）-------------
			//通过多次调用noise累加
            float fbm(float2 n) 
			{
	           float total = 0.0, amplitude = 0.1;
	           for (int i = 0; i < 7; i++) 
			   {
		           total += noise(n) * amplitude;
		           n = mul(m,n);
		           amplitude *= 0.4;
	           }
	           return total;
           }
            

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv*st.xy+st.zw;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = 1;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

				float2 p = i.uv;
	            float2 uv = i.uv;   
                float time = _Time.y * speed;
                float q = fbm(uv * cloudscale * 0.5);
                
                //------------获得波形--------------
	            float r = 0.0;
	            uv *= cloudscale;
                uv -= q - time;
                float weight = 0.8;

                for (int i=0; i<8; i++){
	            	r += abs(weight*noise( uv ));
                    uv = mul(m,uv)+time;//m*uv + time;
	            	weight *= 0.7;
                }
                
                //------------再次获得另一波形--------
	            float f = 0.0;
                uv = p;//i.uv;//p*float2(iResolution.x/iResolution.y,1.0);
	            uv *= cloudscale;
                uv -= q - time;
                weight = 0.7;
                for (int i=0; i<8; i++){
	            	f += weight*noise( uv );
                    uv = mul(m,uv) + time;
	            	weight *= 0.6;
                }
                
				//-----------叠加波形-----------------
                f *= r + f;
                
                //----------设置噪声颜色--------------
                float c = 0.0;
                time = _Time.y * speed * 2.0;
                uv = p;
	            uv *= cloudscale*2.0;
                uv -= q - time;
                weight = 0.4;
                for (int i=0; i<7; i++){
	            	c += weight*noise( uv );
                    uv = mul(m,uv) + time;
	            	weight *= 0.6;
                }
                
                //---------设置噪声颜色-----------------
                float c1 = 0.0;
                time = _Time.y * speed * 3.0;
                uv = p;//i.uv;//p*float2(iResolution.x/iResolution.y,1.0);
	            uv *= cloudscale*3.0;
                uv -= q - time;
                weight = 0.4;
                for (int i=0; i<7; i++){
	            	c1 += abs(weight*noise( uv ));
                    uv = mul(m,uv) + time;
	            	weight *= 0.6;
                }
	            
				//---------------叠加颜色比例-----------
                c += c1;
                
				//---------------设置天空颜色-----------
                float3 skycolour = lerp(skycolor2, skycolor1, p.y);;//mix(skycolour2, skycolour1, p.y);
                float3 cloudcolour = float3(1.1, 1.1, 0.9) * clamp((clouddark + cloudlight*c), 0.0, 1.0);
   	            
				//---------------设置云层密度------------
                f = cloudcover + cloudalpha*f*r;
                
				//---------------混合天空和云------------
                float3 result = lerp(skycolour, clamp(skytint * skycolour + cloudcolour, 0.0, 1.0), clamp(f + c, 0.0, 1.0));
                
	            col = float4( result, 1.0 );
				
				//col=float4(hash(p),0.0,0.0);
				//col=fbm(p);
				//col=noise(p);
				//col=q;
				//col=r;
				//col=f;
				//col=r+f;
				//col=(f*(r+f));
				//col=c;
                return col;
            }
            ENDCG
        }
    }
}
