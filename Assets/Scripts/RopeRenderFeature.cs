using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RopeRenderFeature : ScriptableRendererFeature
{
    class RopeRenderPass : ScriptableRenderPass
    {
        Material ropeMaterial;
        ComputeBuffer pointBuffer;
        int pointCount;
        static readonly ProfilingSampler profilingSampler =
            new ProfilingSampler("RopeRenderPass");

        public RopeRenderPass(Material mat, ComputeBuffer buf, int count)
        {
            ropeMaterial = mat;
            pointBuffer = buf;
            pointCount = count;
            // Run after all transparent objects
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (ropeMaterial == null || pointBuffer == null || pointCount == 0)
                return;

            // Bind the GPU buffer
            ropeMaterial.SetBuffer("points", pointBuffer);

            // Issue draw
            var cmd = CommandBufferPool.Get("DrawRope");
            using (new ProfilingScope(cmd, profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                cmd.DrawProcedural(
                    Matrix4x4.identity,
                    ropeMaterial,
                    0,                            // shader pass index
                    MeshTopology.Points,
                    pointCount
                );
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    [System.Serializable]
    public class RopeSettings
    {
        public Material ropeMaterial;
        public ComputeBuffer pointBuffer;
        public int pointCount;
    }

    public RopeSettings settings = new RopeSettings();
    RopeRenderPass ropePass;

    public override void Create()
    {
        ropePass = new RopeRenderPass(
            settings.ropeMaterial,
            settings.pointBuffer,
            settings.pointCount
        );
        ropePass.ConfigureInput(ScriptableRenderPassInput.None);
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData
    )
    {
        if (settings.ropeMaterial != null && settings.pointBuffer != null)
            renderer.EnqueuePass(ropePass);
    }
}

