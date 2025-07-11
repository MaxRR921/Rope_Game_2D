using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

public class RopeRenderFeature : ScriptableRendererFeature
{
    class RopePass : ScriptableRenderPass
    {
        public RopePass() =>
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        public override void Execute(ScriptableRenderContext ctx, ref RenderingData data)
        {
            if (RopeGPU.Ropes.Count == 0) return;

            CommandBuffer cmd = CommandBufferPool.Get("GPU-Ropes");

            foreach (var rope in RopeGPU.Ropes)
            {
                if (rope == null || !rope.enabled) continue;

                var mat = rope.ropeMaterial;
                if (mat == null) continue;

                mat.SetBuffer("_Points", rope.pointBuffer);
                mat.SetBuffer("_Constraints", rope.constraintBuffer);
                mat.SetFloat("_Thickness", rope.thickness);
                mat.SetColor("_Color", rope.ropeColor);

                cmd.DrawProcedural(Matrix4x4.identity, mat, 0,
                                   MeshTopology.Triangles,
                                   6, rope.numConstraints);
            }

            ctx.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    RopePass pass;
    public override void Create() => pass = new RopePass();
    public override void AddRenderPasses(ScriptableRenderer r, ref RenderingData d)
                                            => r.EnqueuePass(pass);
}

