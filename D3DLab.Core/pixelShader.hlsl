/*
float4 main(float4 position : SV_POSITION) : SV_TARGET{
	return float4(1.0, 0.0, 0.0, 1.0);
}
*/
float4 main(float4 position : SV_POSITION, float4 color : COLOR) : SV_TARGET {
	return color;
}