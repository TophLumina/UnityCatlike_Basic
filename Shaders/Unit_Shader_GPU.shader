Shader "Graph/Unit Surface GPU"
{
    Properties
    {
        _Smothness ("Smothness", Range(0,1)) = 0.5
    }

    SubShader
    {
        CGPROGRAM

        #pragma surface ConfigureSurface Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma editor_sync_compilation
        
        #pragma target 4.5

        #include "URPShader_Graph.hlsl"

        struct Input {
            float3 worldPos;
        };

        float _Smothness;

        void ConfigureSurface (Input input, inout SurfaceOutputStandard surface)
        {
            surface.Albedo = saturate(input.worldPos * 0.5 + 0.5);
            surface.Smoothness = _Smothness;
        }

        ENDCG
    }

    Fallback "Diffuse"
}
