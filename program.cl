// #define GLINTEROP


__kernel void device_function( __global float3* oldPos, __global float3* newPos, __global float3* oldDir, __global float3 newDir, float particleCount, float time )
{
	// adapted from inigo quilez - iq/2013
	int idx = get_global_id( 0 );
	int idy = get_global_id( 1 );
	int id = idx + 512 * idy;
	if (id >= particleCount) return;
	oldPos[id] 
	
	
}
