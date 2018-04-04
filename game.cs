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
using System.Windows.Forms;
using System.Drawing;

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
        OpenCLBuffer<float> velBuffer;
        Random rng;
        // create an OpenGL texture to which OpenCL can send data
        OpenCLImage<int> image = new OpenCLImage<int>( ocl, 512, 512 );
        public Surface screen;
        Stopwatch timer = new Stopwatch();
        int seed = 0;
        int tickCount = 0;
        int particleCount = 0;
        int sprayTicks = 0;
        int boxes = 0;
        float particleSpeed = 0;
        float coneAngle = 0;
        float deltaTime = 0;
        float pvalue = 0f;
        float boxSize = 0f;
        int[,,] boxValues;
        float[] lastSDs;
        float sdMinCount = 100f;
        int goodTicks = 0;
        Random random;
        SimData inSimData;
        
        int simCount = 0;
        static int totalSims = 1;
        float[] results = new float[totalSims];
        bool notDone = true;



        // output
        List<string> lines = new List<string>();
    
        public void Init(SimData simData)
        {
            tickCount = 0;
            goodTicks = 0;
            
            lastSDs = new float[(int)sdMinCount];
            inSimData = simData;   
            seed = simData.seed;
            particleCount = simData.particleCount;
            particleSpeed = simData.particleSpeed;
            coneAngle = simData.coneAngle;
            sprayTicks = simData.sprayTicks;
            deltaTime = simData.deltaTime;
            pvalue = simData.pvalue;
            boxes = simData.boxes;
            boxSize = simData.boxSize;
            boxValues = new int[boxes,boxes,boxes];
            random = new Random(seed);
            oldPosBuffer = new OpenCLBuffer<float3>(ocl, particleCount);
            newPosBuffer = new OpenCLBuffer<float3>(ocl, particleCount);
            oldDirBuffer = new OpenCLBuffer<float3>(ocl, particleCount);
            newDirBuffer = new OpenCLBuffer<float3>(ocl, particleCount);
            velBuffer = new OpenCLBuffer<float>(ocl, particleCount);
            
            
            for (int i = 0; i < particleCount; i++)
            {
                oldPosBuffer[i] = new float3(0,0,0f);
                oldDirBuffer[i] = new float3(0,0,0f);
                newPosBuffer[i] = new float3(0,0,0f);
                newDirBuffer[i] = new float3(0,0,0f);
                velBuffer[i] = particleSpeed - 0.2f + (float)(random.NextDouble() * 0.4d);
            }
            velBuffer.CopyToDevice();
        }

        public float[] runSim()
        {
            while (notDone)
            {
                /*if (tickCount == 100)
                    MessageBox.Show("OK 100");*/
                if (tickCount < sprayTicks)
                {
                    int particlePerTick = particleCount / sprayTicks;
                    for (int i = 0; i < particlePerTick; i++)
                    {
                        oldDirBuffer[particlePerTick * tickCount + i] = createRandomVector();
                        newDirBuffer[particlePerTick * tickCount + i] = oldDirBuffer[particlePerTick * tickCount + i];
                    }
                }
                tickCount++;
                GL.Finish();
                // clear the screen
                //screen.Clear(0);
                // do opencl stuff

                kernel.SetArgument(0, oldPosBuffer);
                kernel.SetArgument(1, newPosBuffer);
                kernel.SetArgument(2, oldDirBuffer);
                kernel.SetArgument(3, newDirBuffer);
                kernel.SetArgument(4, velBuffer);
                kernel.SetArgument(5, particleCount);
                kernel.SetArgument(6, deltaTime);
                kernel.SetArgument(7, boxSize);
                // execute kernel
                int worksize = (int)Math.Sqrt(particleCount);
                worksize = worksize + 32 - (worksize % 32);
                kernel.SetArgument(8, worksize);
                long[] workSize = { worksize, worksize };
                long[] localSize = { 32, 4 };

                // NO INTEROP PATH:
                // Use OpenCL to fill a C# pixel array, encapsulated in an
                // OpenCLBuffer<int> object (buffer). After filling the buffer, it
                // is copied to the screen surface, so the template code can show
                // it in the window.
                // execute the kernel
                oldDirBuffer.CopyToDevice();
                oldPosBuffer.CopyToDevice();

                kernel.Execute(workSize, localSize);
                // get the data from the device to the host
                newPosBuffer.CopyFromDevice();
                newDirBuffer.CopyFromDevice();



                // pressure calcs

                if (tickCount > sprayTicks)
                {
                    if (calcPressure())
                    {
                        //return tickCount * deltaTime;
                    }
                }

                var tempBuffer = oldDirBuffer;
                oldDirBuffer = newDirBuffer;
                newDirBuffer = tempBuffer;

                tempBuffer = oldPosBuffer;
                oldPosBuffer = newPosBuffer;
                newPosBuffer = tempBuffer;

                /*for(int i = 0; i < 100; i++)
                    for(int j = 0; j < 100; j++)
                    {
                        screen.pixels[i * 512 + j] = (int)((float)oldPosBuffer[i + j * 100].x * 256f) ;
                    }*/
            }
            return results;
        }
        

        public void Tick()
        {
            
        }
        
        

        private bool calcPressure()
        {
            boxValues = new int[boxes, boxes, boxes];
            for(int i = 0; i < particleCount; i++)
            {
                float3 particle = newPosBuffer[i];
                float singleBoxSize = boxSize / (float)boxes;
                int x = (int)((particle.x + boxSize/2) / singleBoxSize);
                int y = (int)((particle.y + boxSize / 2) / singleBoxSize);
                int z = (int)((particle.z) / singleBoxSize);
                boxValues[x, y, z]++;
            }
            float popvar = 0f;
            float mean = (float)particleCount / (float)(boxes * boxes * boxes);
            for (int i = 0; i < boxes; i++)
                for (int j = 0; j < boxes; j++)
                    for (int k = 0; k < boxes; k++)
                        popvar += (boxValues[i,j,k] - mean) * (boxValues[i, j, k] - mean);
            popvar /= boxes * boxes * boxes;
            double standarddeviation = Math.Sqrt(popvar);
            
            lastSDs[(int)(tickCount % sdMinCount)] = (float)standarddeviation;

            if (tickCount > sdMinCount)
            {
                float avg = 0;
                for (int i = 0; i < sdMinCount; i++)
                    avg += lastSDs[i];
                avg /= sdMinCount;
                if (avg < mean * pvalue)
                {
                    
                    goodTicks++;
                    if(goodTicks > 100)
                    {
                        results[simCount] = tickCount * deltaTime;
                        simCount++;
                        //lines.Add((tickCount * deltaTime).ToString());
                        //writeOrReinitialize();
                        return true;
                    }
                    
                }
            }
            return false;
        }

        

        private void writeOrReinitialize()
        {
            // todo: restart with other variables
            if (lines.Count >= totalSims)
            {
                //System.IO.File.WriteAllLines("output.csv", lines);
                //System.Diagnostics.Process.Start("output.csv");
                //Environment.Exit(1);
                notDone = false;
                    //System.Threading.Tasks.
            }
            else
            {
                SimData newSimData = new SimData { seed = inSimData.seed + 12345, coneAngle = inSimData.coneAngle, particleCount = inSimData.particleCount, particleSpeed = inSimData.particleSpeed, sprayTicks = inSimData.sprayTicks, deltaTime = inSimData.deltaTime, boxSize = inSimData.boxSize, boxes = inSimData.boxes };
                Init(newSimData);
            }
        }
        private float2 randomPointOnCircle()
        {
            double randomfloat = (random.NextDouble() * 2f * Math.PI);
            double randomradius = random.NextDouble();
            return new float2((float)(Math.Sqrt(randomradius) * Math.Cos(randomfloat)), (float)(Math.Sqrt( randomradius) * Math.Sin(randomfloat)));
        }


        private float3 createRandomVector()
        {
            float2 point = randomPointOnCircle();
            float adjacent = 1f/(float)Math.Tan(coneAngle);
            Vector3 final = new Vector3(point.x, point.y, adjacent);
            final.Normalize();

            return new float3(final.X, final.Y, final.Z);
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
        public float deltaTime;
        public float boxSize;
        public int boxes;
        public float pvalue;

        public SimData()
        {

        }

        public SimData(SimData defaultData)
        {
            seed = defaultData.seed;
            particleCount = defaultData.particleCount;
            particleSpeed = defaultData.particleSpeed;
            coneAngle = defaultData.coneAngle;
            sprayTicks = defaultData.sprayTicks;
            deltaTime = defaultData.deltaTime;
            boxSize = defaultData.boxSize;
            boxes = defaultData.boxes;
            pvalue = defaultData.pvalue;
        }
    }

} // namespace Template
