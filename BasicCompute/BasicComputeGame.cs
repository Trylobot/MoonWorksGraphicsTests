﻿using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
    class BasicComputeGame : Game
    {
        private GraphicsPipeline drawPipeline;
        private Texture texture;
        private Sampler sampler;
        private Buffer vertexBuffer;

        public BasicComputeGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
        {
            ShaderModule computeShaderModule = new ShaderModule(
                GraphicsDevice,
                TestUtils.GetShaderPath("FillTextureCompute.spv")
            );

            ComputePipeline computePipeline = new ComputePipeline(
                GraphicsDevice,
                ComputeShaderInfo.Create(computeShaderModule, "main", 0, 1)
            );

            ShaderModule vertShaderModule = new ShaderModule(
                GraphicsDevice,
                TestUtils.GetShaderPath("TexturedQuadVert.spv")
            );

            ShaderModule fragShaderModule = new ShaderModule(
                GraphicsDevice,
                TestUtils.GetShaderPath("TexturedQuadFrag.spv")
            );

            GraphicsPipelineCreateInfo drawPipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
                vertShaderModule,
                fragShaderModule
            );
            drawPipelineCreateInfo.VertexInputState = new VertexInputState(
                VertexBinding.Create<PositionTextureVertex>(),
                VertexAttribute.Create<PositionTextureVertex>("Position", 0),
                VertexAttribute.Create<PositionTextureVertex>("TexCoord", 1)
            );
            drawPipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;

            drawPipeline = new GraphicsPipeline(
                GraphicsDevice,
                drawPipelineCreateInfo
            );

            vertexBuffer = Buffer.Create<PositionTextureVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 6);

            // Create the texture that will be filled in by the compute pipeline
            texture = Texture.CreateTexture2D(
                GraphicsDevice,
                MainWindow.Width,
                MainWindow.Height,
                TextureFormat.R8G8B8A8,
                TextureUsageFlags.Compute | TextureUsageFlags.Sampler
            );
            sampler = new Sampler(GraphicsDevice, new SamplerCreateInfo());

            // Populate the vertex buffer and run the compute shader to generate the texture data
            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

            cmdbuf.SetBufferData(vertexBuffer, new PositionTextureVertex[]
            {
                new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
                new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
                new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
                new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
                new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
                new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1)),
            });

            cmdbuf.BindComputePipeline(computePipeline);
            cmdbuf.BindComputeTextures(texture);
            cmdbuf.DispatchCompute(MainWindow.Width / 8, MainWindow.Height / 8, 1, 0);

            GraphicsDevice.Submit(cmdbuf);
        }

        protected override void Update(System.TimeSpan delta) { }

        protected override void Draw(double alpha)
        {
            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
            if (backbuffer != null)
            {
                cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.CornflowerBlue));
                cmdbuf.BindGraphicsPipeline(drawPipeline);
                cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(texture, sampler));
                cmdbuf.BindVertexBuffers(vertexBuffer);
                cmdbuf.DrawPrimitives(0, 2, 0, 0);
                cmdbuf.EndRenderPass();
            }
            GraphicsDevice.Submit(cmdbuf);
        }

        public static void Main(string[] args)
        {
            BasicComputeGame game = new BasicComputeGame();
            game.Run();
        }
    }
}
