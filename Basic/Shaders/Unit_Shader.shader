Shader "Graph/Unit Surface" {
    Properties {
        _Smothness ("Smothness", Range(0,1)) = 0.5
    }

    SubShader {
        CGPROGRAM

        #pragma surface ConfigureSurface Standard fullforwardshadows
        #pragma target 3.0

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
