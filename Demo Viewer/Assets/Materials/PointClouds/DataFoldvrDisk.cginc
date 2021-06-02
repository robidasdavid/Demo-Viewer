// Pcx - Point cloud importer & renderer for 
// https://github.com/keijiro/Pcx

#include "UnityCG.cginc"
#include "Common.cginc"

// Uniforms
half4 _Tint;
half _PointSize;
float4x4 _Transform;
int _ShowLayerColors;

struct Point
{
    float3 position;
    uint color;
};

static const half3 colors[8] =
{
    half3(1, 0, 0), // red
    half3(1, .5, 0), // orange
    half3(.9, .8, .2), // yellow
    half3(.2, .8, .26), // green
    half3(.2, .8, .9), // cyan
    half3(.2, .3, .9), // blue
    half3(1, .5, 0), // purple
    half3(.6, .2, .93), // pink
};

#if _COMPUTE_BUFFER
StructuredBuffer<Point> _PointBuffer;
#endif

// Vertex input attributes
struct Attributes
{
    #if _COMPUTE_BUFFER
    uint vertexID : SV_VertexID;
    #else
    float4 position : POSITION;
    half3 color : COLOR;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Fragment varyings
struct Varyings
{
    float4 position : SV_POSITION;
    #if !PCX_SHADOW_CASTER
    half3 color : COLOR;
    uint layer: LAYER;
    UNITY_FOG_COORDS(0)
    #endif

    UNITY_VERTEX_OUTPUT_STEREO
};

// Vertex phase
Varyings Vertex(Attributes input)
{
    Varyings o;

    // for stereo rendering
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_OUTPUT(Varyings, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    // Retrieve vertex attributes.
    #if _COMPUTE_BUFFER
    Point pt = _PointBuffer[input.vertexID];
    float4 pos = mul(_Transform, float4(pt.position, 1));
    half4 colAndLayer = PcxDecodeColorAndLayer(pt.color);
    half3 col = colAndLayer.xyz / (255);
    uint layer = colAndLayer.w;
    #else
    float4 pos = input.position;
    half3 col = input.color;
    uint layer = 0;
    #endif

    #if !PCX_SHADOW_CASTER
    // Color space convertion & applying tint
    #if UNITY_COLORSPACE_GAMMA
        col *= _Tint.rgb * 2;
    #else
    col *= LinearToGammaSpace(_Tint.rgb) * 2;
    col = GammaToLinearSpace(col);
    #endif
    #endif

    // Set vertex output.

    o.position = UnityObjectToClipPos(pos);
    #if !PCX_SHADOW_CASTER
    o.color = col;
    o.layer = layer;
    UNITY_TRANSFER_FOG(o, o.position);
    #endif
    return o;
}

// Geometry phase
[maxvertexcount(36)]
void Geometry(point Varyings input[1], inout TriangleStream<Varyings> outStream)
{
    float4 origin = input[0].position;
    float2 extent = abs(UNITY_MATRIX_P._11_22 * _PointSize);

    // Copy the basic information.
    Varyings o = input[0];

    // Determine the number of slices based on the radius of the
    // point on the screen.
    const float radius = extent.y / origin.w * _ScreenParams.y;
    const uint slices = min((radius + 1) / 5, 4) + 2;

    // Slightly enlarge quad points to compensate area reduction.
    // Hopefully this line would be complied without branch.
    if (slices == 2) extent *= 1.2;

    // Top vertex
    o.position.y = origin.y + extent.y;
    o.position.xzw = origin.xzw;
    outStream.Append(o);

    UNITY_LOOP for (uint i = 1; i < slices; i++)
    {
        float sn, cs;
        sincos(UNITY_PI / slices * i, sn, cs);

        // Right side vertex
        o.position.xy = origin.xy + extent * float2(sn, cs);
        outStream.Append(o);

        // Left side vertex
        o.position.x = origin.x - extent.x * sn;
        outStream.Append(o);
    }

    // Bottom vertex
    o.position.x = origin.x;
    o.position.y = origin.y - extent.y;
    outStream.Append(o);

    outStream.RestartStrip();
}

half4 Fragment(Varyings input) : SV_Target
{
    #if PCX_SHADOW_CASTER
    return 0;
    #else

    // static const half3 local_colors[8]
    // {
    //     // input.color.rgb,
    //     half3(1, 0, 0), // red
    //     half3(1, .5, 0), // orange
    //     half3(.9, .8, .2), // yellow
    //     half3(.2, .8, .26), // green
    //     half3(.2, .8, .9), // cyan
    //     half3(.2, .3, .9), // blue
    //     half3(1, .5, 0), // purple
    //     half3(.6, .2, .93), // pink
    // };
    // input.color.r = 1;// = half3(1,0,0);

    // input.color.r =local_colors[input.layer].r; 
    // input.color.g =local_colors[input.layer].g;
    // input.color.b =local_colors[input.layer].b; 
    if (input.layer != 0 && _ShowLayerColors == 1)
    {
        input.color.r = colors[input.layer-1].r;
        input.color.g = colors[input.layer-1].g;
        input.color.b = colors[input.layer-1].b;
    }
    half4 c = half4(input.color, _Tint.a);
    UNITY_APPLY_FOG(input.fogCoord, c);
    return c;
    #endif
}
