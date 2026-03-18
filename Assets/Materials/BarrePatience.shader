Shader "Barrage/UI/BarrePatience"
{
    Properties
    {
        [HideInInspector] _MainTex       ("Texture",              2D)           = "white" {}

        _FillAmount     ("Fill Amount",       Range(0, 1))  = 1.0
        _SeuilMoyenne   ("Seuil Moyenne",     Range(0, 1))  = 0.5
        _SeuilBasse     ("Seuil Basse",       Range(0, 1))  = 0.25

        _ColorHaute     ("Color Haute",       Color)        = (0.18, 0.80, 0.18, 1)
        _ColorMoyenne   ("Color Moyenne",     Color)        = (0.95, 0.70, 0.10, 1)
        _ColorBasse     ("Color Basse",       Color)        = (0.90, 0.15, 0.10, 1)

        // Signal électrique — bord droit de la barre
        _WaveAmp1       ("Wave Amp 1",        Float)        = 0.06
        _WaveFreq1      ("Wave Freq 1",       Float)        = 9.0
        _WaveSpeed1     ("Wave Speed 1",      Float)        = 5.0

        _WaveAmp2       ("Wave Amp 2",        Float)        = 0.025
        _WaveFreq2      ("Wave Freq 2",       Float)        = 23.0
        _WaveSpeed2     ("Wave Speed 2",      Float)        = -11.0

        _WaveAmp3       ("Wave Amp 3",        Float)        = 0.010
        _WaveFreq3      ("Wave Freq 3",       Float)        = 52.0
        _WaveSpeed3     ("Wave Speed 3",      Float)        = 18.0

        // Glow sur le bord du signal
        _GlowWidth      ("Glow Width",        Float)        = 0.05
        _GlowIntensity  ("Glow Intensity",    Float)        = 2.2

        // Lignes d'énergie internes (scanlines verticales animées)
        _EnergyFreq     ("Energy Line Freq",  Float)        = 18.0
        _EnergyAmp      ("Energy Line Amp",   Float)        = 0.12
        _EnergySpeed    ("Energy Line Speed", Float)        = 3.0

        // Bord arrondi (coins)
        _CornerRadius   ("Corner Radius",     Float)        = 0.10

        // Ondulation douce du bord supérieur (effet liquide)
        _TopWaveAmp1    ("Top Wave Amp 1",    Float)        = 0.055
        _TopWaveFreq1   ("Top Wave Freq 1",   Float)        = 2.8
        _TopWaveSpeed1  ("Top Wave Speed 1",  Float)        = 0.7

        _TopWaveAmp2    ("Top Wave Amp 2",    Float)        = 0.028
        _TopWaveFreq2   ("Top Wave Freq 2",   Float)        = 5.5
        _TopWaveSpeed2  ("Top Wave Speed 2",  Float)        = -1.1

        _TopWaveAmp3    ("Top Wave Amp 3",    Float)        = 0.012
        _TopWaveFreq3   ("Top Wave Freq 3",   Float)        = 11.0
        _TopWaveSpeed3  ("Top Wave Speed 3",  Float)        = 1.8

        // Largeur du fondu sur le bord supérieur ondulé
        _TopEdgeSoftness ("Top Edge Softness", Float)       = 0.04

        // UI
        _StencilComp    ("Stencil Comparison", Float)       = 8
        _Stencil        ("Stencil ID",         Float)       = 0
        _StencilOp      ("Stencil Operation",  Float)       = 0
        _StencilWriteMask("Stencil Write Mask",Float)       = 255
        _StencilReadMask ("Stencil Read Mask", Float)       = 255
        _ColorMask      ("Color Mask",         Float)       = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
            "PreviewType"     = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        Blend    SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4    _ClipRect;

            float _FillAmount;
            float _SeuilMoyenne;
            float _SeuilBasse;

            float4 _ColorHaute;
            float4 _ColorMoyenne;
            float4 _ColorBasse;

            float _WaveAmp1, _WaveFreq1, _WaveSpeed1;
            float _WaveAmp2, _WaveFreq2, _WaveSpeed2;
            float _WaveAmp3, _WaveFreq3, _WaveSpeed3;

            float _GlowWidth;
            float _GlowIntensity;

            float _EnergyFreq;
            float _EnergyAmp;
            float _EnergySpeed;

            float _CornerRadius;

            float _TopWaveAmp1,  _TopWaveFreq1,  _TopWaveSpeed1;
            float _TopWaveAmp2,  _TopWaveFreq2,  _TopWaveSpeed2;
            float _TopWaveAmp3,  _TopWaveFreq3,  _TopWaveSpeed3;
            float _TopEdgeSoftness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.uv       = v.uv;
                o.worldPos = v.vertex;
                o.color    = v.color;
                return o;
            }

            // ── Helpers ─────────────────────────────────────────────────────

            // Coins arrondis : distance SDF à un rectangle arrondi en UV [0,1]
            float RoundedBoxSDF(float2 uv, float radius)
            {
                float2 d = abs(uv - 0.5) - (0.5 - radius);
                return length(max(d, 0.0)) - radius;
            }

            // Couleur de la barre selon le niveau de remplissage
            float3 BarColor(float fill)
            {
                if (fill > _SeuilMoyenne)
                    return lerp(_ColorMoyenne.rgb, _ColorHaute.rgb,
                                (fill - _SeuilMoyenne) / max(1.0 - _SeuilMoyenne, 0.001));
                else if (fill > _SeuilBasse)
                    return lerp(_ColorBasse.rgb, _ColorMoyenne.rgb,
                                (fill - _SeuilBasse) / max(_SeuilMoyenne - _SeuilBasse, 0.001));
                else
                    return _ColorBasse.rgb;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float x = i.uv.x;  // 0 = gauche, 1 = droite
                float y = i.uv.y;  // 0 = bas,    1 = haut
                float t = _Time.y;

                // ── Signal électrique sur le bord droit (fill boundary) ───────
                // Sinusoïdes en Y → le bord ondule verticalement
                float wave = sin(y * _WaveFreq1 + t * _WaveSpeed1) * _WaveAmp1
                           + sin(y * _WaveFreq2 + t * _WaveSpeed2) * _WaveAmp2
                           + sin(y * _WaveFreq3 + t * _WaveSpeed3) * _WaveAmp3;

                float edgeX    = _FillAmount + wave;
                float distEdge = edgeX - x;  // > 0 = intérieur, < 0 = extérieur

                // ── Ondulation douce du bord supérieur (effet liquide) ────────
                // Sinusoïdes en X, basses fréquences → surface liquide calme
                float topWave = sin(x * _TopWaveFreq1 + t * _TopWaveSpeed1) * _TopWaveAmp1
                              + sin(x * _TopWaveFreq2 + t * _TopWaveSpeed2) * _TopWaveAmp2
                              + sin(x * _TopWaveFreq3 + t * _TopWaveSpeed3) * _TopWaveAmp3;

                // topEdge : la surface du liquide en coordonnées UV Y
                // Elle est active seulement dans la zone remplie (x < edgeX)
                float topEdge   = 1.0 - topWave; // proche de 1.0 (haut de la barre)
                float distTop   = topEdge - y;    // > 0 = sous la surface (rempli)

                // Fondu doux sur le bord supérieur
                float topMask   = smoothstep(0.0, _TopEdgeSoftness, distTop);

                // ── Couleur principale de la barre ───────────────────────────
                float3 barColor = BarColor(_FillAmount);

                // ── Lignes d'énergie internes (verticales animées) ───────────
                float energyLine = sin(x * _EnergyFreq + t * _EnergySpeed) * _EnergyAmp;
                float3 interior  = barColor * (1.0 + energyLine);

                // ── Pulsation globale (légère) ───────────────────────────────
                float pulse  = 1.0 + 0.04 * sin(t * 4.0);
                interior    *= pulse;

                // ── Reflet liquide sur le bord supérieur ─────────────────────
                // Une fine ligne plus claire juste sous la surface
                float surfaceGlow  = smoothstep(_TopEdgeSoftness * 2.0, 0.0, abs(distTop - _TopEdgeSoftness * 0.5));
                float3 surfaceColor = barColor * 2.2 + float3(0.3, 0.3, 0.3);
                interior = lerp(interior, surfaceColor, surfaceGlow * 0.6);

                // ── Glow beam sur le bord droit (signal électrique) ──────────
                float glowFactor = smoothstep(_GlowWidth, 0.0, abs(distEdge));
                float3 glowColor = barColor * _GlowIntensity + float3(0.4, 0.4, 0.4);

                // ── Composition couleur finale ───────────────────────────────
                float3 finalColor = lerp(interior, glowColor, glowFactor);

                // ── Alpha ────────────────────────────────────────────────────
                float alpha = 0.0;

                // Intérieur : doit être à gauche du bord droit ET sous la surface liquide
                if (distEdge > 0.0)
                    alpha = 0.88 * topMask;

                // Halo du bord droit (légèrement à l'extérieur du fill)
                alpha = max(alpha, glowFactor * 0.85 * topMask);

                // ── Coins arrondis ───────────────────────────────────────────
                float cornerDist = RoundedBoxSDF(i.uv, _CornerRadius);
                float cornerMask = 1.0 - smoothstep(-0.005, 0.005, cornerDist);
                alpha *= cornerMask;

                // ── Clip UI (masques, ScrollRect, etc.) ─────────────────────
                alpha *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);

                return fixed4(saturate(finalColor), alpha);
            }
            ENDCG
        }
    }
}
