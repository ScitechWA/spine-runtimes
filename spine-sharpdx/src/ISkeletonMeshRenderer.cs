using SharpDX.Toolkit.Graphics;
using System;
namespace Spine
{
    public interface ISkeletonMeshRenderer
    {
        global::Spine.MeshBatcher Batcher { get; }
        void Begin();
        void Begin(global::SharpDX.Matrix transformMatrix);
        global::SharpDX.Rectangle calcBoundingRect(global::Spine.Skeleton skeleton);
        global::SharpDX.Toolkit.Graphics.BlendState DefaultBlendState { get; set; }
        void Draw(global::Spine.Skeleton skeleton);
        global::SharpDX.Toolkit.Graphics.Effect Effect { get; set; }
        void End();
        bool getRectForSlot(global::Spine.Skeleton skeleton, string SlotName, out global::SharpDX.Rectangle resultRect);
        bool PremultipliedAlpha { get; set; }
        RasterizerState RasterizerState { get; set; }
    }
}
