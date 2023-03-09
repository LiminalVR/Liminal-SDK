uniform float Fade;
uniform float _DitherMin;
uniform float _DitherMax;

inline float Dither4x4Bayer(int x, int y)
{
	const float dither[16] = {
		 1,  9,  3, 11,
		13,  5, 15,  7,
		 4, 12,  2, 10,
		16,  8, 14,  6 };
	int r = y * 4 + x;
	return dither[r] / 16; // same # of instructions as pre-dividing due to compiler magic
}

void Unity_Distance_float3(float3 A, float3 B, out float Out)
{
	Out = distance(A, B);
}

void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
{
	Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

void ClipByDither(float4 screenPosition, float3 worldPos)
{
	float4 ase_screenPos = screenPosition;
	float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
	ase_screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
	float2 clipScreen5 = ase_screenPosNorm.xy * _ScreenParams.xy;
	float dither5 = Dither4x4Bayer(fmod(clipScreen5.x, 4), fmod(clipScreen5.y, 4));
	float cameraDist = length(worldPos.xyz - _WorldSpaceCameraPos.xyz);
	float remapValue;
	float2 clamp = float2(_DitherMin, _DitherMax);
	Unity_Remap_float(cameraDist, clamp, float2 (1, 0), remapValue);
	clip(dither5 - (0.05 + remapValue));
}