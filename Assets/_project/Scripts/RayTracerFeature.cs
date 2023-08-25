using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RayTracerFeature : ScriptableRendererFeature
{
    class RayTracerPass : ScriptableRenderPass
    {

        const string ProfilerTag = "Ray-tracing test";

        private ComputeShader RayTracingShader;
        private RTHandle source { get; set; }
        private RTHandle destination { get; set; }

        private RenderTargetIdentifier targetIdentifier;
        private RenderTargetIdentifier convergedIdentifier;


        public void Setup(ComputeShader RayTracingShader, RenderTargetIdentifier targetIdentifier, RenderTargetIdentifier convergedIdentifier, RTHandle source, RTHandle destination)
        {
            this.RayTracingShader = RayTracingShader;
            this.targetIdentifier = targetIdentifier;
            this.convergedIdentifier = convergedIdentifier;
            this.source = source;
            this.destination = destination;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            if(RayTracingShader == null)
            {
                Debug.LogError("Compute shader is null");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(ProfilerTag);

            using (new ProfilingScope(cmd, new ProfilingSampler(ProfilerTag)))
            {
                cmd.SetComputeMatrixParam(RayTracingShader, "_CameraToWorld", renderingData.cameraData.camera.cameraToWorldMatrix);
                cmd.SetComputeMatrixParam(RayTracingShader, "_CameraInverseProjection", renderingData.cameraData.camera.projectionMatrix.inverse);

                if(RayTraceScene.Instance != null)
                {
                    cmd.SetComputeBufferParam(RayTracingShader, 0, "_MeshObjects", RayTraceScene.Instance.MeshObjectBuffer);
                    cmd.SetComputeBufferParam(RayTracingShader, 0, "_Vertices", RayTraceScene.Instance.VertexBuffer);
                    cmd.SetComputeBufferParam(RayTracingShader, 0, "_Indices", RayTraceScene.Instance.IndexBuffer);
                    cmd.SetComputeBufferParam(RayTracingShader, 0, "_Spheres", RayTraceScene.Instance.SphereBuffer);

                    Vector3 l = RayTraceScene.Instance.sun.transform.forward;
                    cmd.SetComputeVectorParam(RayTracingShader, "_DirectionalLight", new Vector4(l.x, l.y, l.z, RayTraceScene.Instance.sun.intensity));

                    cmd.SetComputeTextureParam(RayTracingShader, 0, "_SkyboxTexture", RayTraceScene.Instance.ProbeIdentifier);
                }

                cmd.SetComputeFloatParam(RayTracingShader, "_Seed", Random.value * 4000);
                cmd.SetComputeTextureParam(RayTracingShader, 0, "Result", targetIdentifier);

                cmd.DispatchCompute(
                    RayTracingShader,
                    0,
                    Mathf.CeilToInt(Screen.width / 8f),
                    Mathf.CeilToInt(Screen.height / 8f),
                    1
                );

                // Blit from the color buffer to a temporary buffer
                cmd.Blit(targetIdentifier, source);
            }

            //CoreUtils.SetRenderTarget(cmd, renderingData.cameraData.renderer.GetCameraColorBackBuffer(cmd));
            //CoreUtils.DrawFullScreen(cmd, );
            // Execute the command buffer and release it.
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    public ComputeShader RayTracingShader;

    public bool scenePreview = false;
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
    public RenderTexture target;
    public RenderTargetIdentifier targetIdentifier; 
    public RenderTexture converged;
    public RenderTargetIdentifier convergedIdentifier;

    RayTracerPass rayTracePass;

    /// <inheritdoc/>
    public override void Create()
    {
        rayTracePass = new RayTracerPass();

        // Configures where the render pass should be injected.
        rayTracePass.renderPassEvent = renderPassEvent;
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (target == null)
        {
            target = new RenderTexture(renderingData.cameraData.cameraTargetDescriptor)
            {
                enableRandomWrite = true
            };

            bool createdTexture = target.Create();

            targetIdentifier = new RenderTargetIdentifier(target);
        }

        if (converged == null)
        {
            converged = new RenderTexture(renderingData.cameraData.cameraTargetDescriptor)
            {
                enableRandomWrite = true
            };

            bool createdTexture = converged.Create();

            convergedIdentifier = new RenderTargetIdentifier(converged);
        }

        rayTracePass.Setup(RayTracingShader, targetIdentifier, convergedIdentifier, renderer.cameraColorTargetHandle, renderer.cameraColorTargetHandle);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(!Application.isPlaying && !scenePreview)
        {
            return;
        }

        renderer.EnqueuePass(rayTracePass);
    }

}


