using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Cloo;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Windows;

namespace Template {

    class Game
    {

	    // when GLInterop is set to true, the fractal is rendered directly to an OpenGL texture
	    bool GLInterop = false;
	    // load the OpenCL program; this creates the OpenCL context
	    static OpenCLProgram ocl = new OpenCLProgram( "../../program.cl" );
	    // find the kernel named 'device_function' in the program
	    OpenCLKernel kernel = new OpenCLKernel( ocl, "device_function" );
        // create a regular buffer; by default this resides on both the host and the device
        OpenCLBuffer<float3> newPosBuffer;// = new OpenCLBuffer<int>( ocl, 512 * 512 );
        OpenCLBuffer<float3> oldPosBuffer;
        OpenCLBuffer<float3> newDirBuffer;
        OpenCLBuffer<float3> oldDirBuffer;
        Random rng;
	    // create an OpenGL texture to which OpenCL can send data
	    OpenCLImage<int> image = new OpenCLImage<int>( ocl, 512, 512 );
	    public Surface screen;
	    Stopwatch timer = new Stopwatch();
        int seed = 0;
        int tickCount = 0;
        int particleCount = 0;
        int sprayTicks = 0;
        float particleSpeed = 0;
        float coneAngle = 0;
        Random random;

	    public void Init(SimData simData)
	    {

            
            seed = simData.seed;
            particleCount = simData.particleCount;
            particleSpeed = simData.particleSpeed;
            coneAngle = simData.coneAngle;
            sprayTicks = simData.sprayTicks;
            random = new Random(seed);
            oldPosBuffer = new OpenCLBuffer<float3>(ocl, particleCount * particleCount);
            newPosBuffer = new OpenCLBuffer<float3>(ocl, particleCount * particleCount);
            oldDirBuffer = new OpenCLBuffer<float3>(ocl, particleCount * particleCount);
            newDirBuffer = new OpenCLBuffer<float3>(ocl, particleCount * particleCount);
            for(int i = 0; i < particleCount * particleCount; i++)
            {
                oldPosBuffer[i] = new float3();
                oldDirBuffer[i] = new float3();
                newPosBuffer[i] = new float3();
                newDirBuffer[i] = new float3();
            }
        }

	    public void Tick()
	    {
            if(tickCount < sprayTicks)
            {
                int particlePerTick = particleCount / tickCount;
                for(int i = 0; i < particlePerTick; i++)
                {
                    oldDirBuffer[particlePerTick * tickCount + i] = createRandomVector();
                }
            }
            tickCount++;
		    GL.Finish();
		    // clear the screen
		    screen.Clear( 0 );
		    // do opencl stuff
		    
            kernel.SetArgument(0, oldPosBuffer);
            kernel.SetArgument(1, newPosBuffer);
            kernel.SetArgument(2, oldDirBuffer);
            kernel.SetArgument(3, newDirBuffer);;
            kernel.SetArgument(4, particleCount);
            // execute kernel
            int worksize = (int)Math.Sqrt(particleCount);
            long [] workSize = { worksize + 32 - (worksize % 32), worksize + 32 - (worksize % 32) };
		    long [] localSize = { 32, 4 };

			// NO INTEROP PATH:
			// Use OpenCL to fill a C# pixel array, encapsulated in an
			// OpenCLBuffer<int> object (buffer). After filling the buffer, it
			// is copied to the screen surface, so the template code can show
			// it in the window.
			// execute the kernel
			kernel.Execute( workSize, localSize );
			// get the data from the device to the host
			newPosBuffer.CopyFromDevice();
			// plot pixels using the data on the host
			/*for( int y = 0; y < 512; y++ ) for( int x = 0; x < 512; x++ )
			{
				screen.pixels[x + y * screen.width] = newPosBuffer[x + y * 512];
			}*/
		    
	    }

        private float3 createRandomVector()
        {
            
            return new float3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
        }

        public void Render()
        {

        }

        
    }

    public class SimData
    {
        public int seed;
        public int particleCount;
        public float particleSpeed;
        public float coneAngle;
        public int sprayTicks;
    }

} // namespace Template