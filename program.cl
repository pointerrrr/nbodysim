// #define GLINTEROP


__kernel void device_function( __global float3* oldPos, __global float3* newPos, __global float3* oldDir, __global float3* newDir, __global float* vel, int particleCount,
		float deltaTime, float boxSize, int size)
{
	// adapted from inigo quilez - iq/2013
	int idx = get_global_id( 0 );
	int idy = get_global_id( 1 );

	int id = idx + size * idy;
	if (id >= particleCount) return;

	
	
	newPos[id] = oldPos[id] + (oldDir[id] * (float3)(vel[id], vel[id], vel[id]) * (float3)(deltaTime, deltaTime, deltaTime));
	newDir[id] = oldDir[id];	
	//newPos[id] = (float3)(id * 1.0f, id* 1.0f, id * 1.0f);

	if(newPos[id].x < 0-(boxSize/2))
	{
		newPos[id] = (float3)( 0.0001f-(boxSize/2), newPos[id].y, newPos[id].z);	
		newDir[id] = (float3) (newDir[id].x * (-1.0f), newDir[id].y, newDir[id].z);
	}
	if(newPos[id].x > (boxSize/2))
	{
		newPos[id] = (float3)( (boxSize/2)-0.0001f, newPos[id].y, newPos[id].z);	
		newDir[id] = (float3) (newDir[id].x * (-1.0f), newDir[id].y, newDir[id].z);
	}
	if(newPos[id].y < 0-(boxSize/2))
	{
		newPos[id] = (float3)(newPos[id].x, 0.0001f-(boxSize/2), newPos[id].z);	
		newDir[id] = (float3) (newDir[id].x, newDir[id].y * (-1.0f), newDir[id].z);
	}
	if(newPos[id].y > (boxSize/2))
	{
		newPos[id] = (float3)(newPos[id].x, (boxSize/2)-0.0001f, newPos[id].z);	
		newDir[id] = (float3) (newDir[id].x, newDir[id].y * (-1.0f), newDir[id].z);
	}
	if(newPos[id].z < 0.0f)
	{
		newPos[id] = (float3)(newPos[id].x, newPos[id].y, 0.0001f);	
		newDir[id] = (float3) (newDir[id].x, newDir[id].y, newDir[id].z * (-1.0f));
	}
	if(newPos[id].z > boxSize)
	{
		newPos[id] = (float3)(newPos[id].x, newPos[id].y, boxSize - 0.0001f);	
		newDir[id] = (float3) (newDir[id].x, newDir[id].y, newDir[id].z * (-1.0f));
	}

}
