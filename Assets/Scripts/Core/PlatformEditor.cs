using System;

namespace ShiftRunner
{
    internal sealed class PlatformEditor
    {
        private readonly RunnerWorld world;

        internal PlatformEditor(RunnerWorld world)
        {
            this.world = world;
        }

        internal void Build(float left, float right)
        {
            // A recompile replaces existing code inside its bounds. Unselected code survives.
            Cut(left, right, bridgesOnly: true);
            world.Add(PieceKind.Bridge, left, right, -0.35f, 0f);
        }

        internal bool Cut(float left, float right, bool bridgesOnly = false, bool includeFloor = false)
        {
            bool changed = false;
            for (int i = world.Blocks.Count - 1; i >= 0; i--)
            {
                Piece p = world.Blocks[i];
                if (!p.Overlaps(left, right)) continue;
                if (p.Kind == PieceKind.Ground && !includeFloor) continue;
                if (bridgesOnly && p.Kind != PieceKind.Bridge) continue;
                changed = true;
                if (p.Kind == PieceKind.Enemy || p.Kind == PieceKind.Hazard)
                {
                    world.Blocks.RemoveAt(i);
                    continue;
                }

                // Clip continuous geometry precisely, including selections inside one bridge.
                if (p.Kind == PieceKind.Ground)
                    world.AddGap(Math.Max(left, p.Left), Math.Min(right, p.Right));
                float oldRight = p.Right;
                if (p.Left < left)
                {
                    p.Right = left;
                    if (oldRight > right)
                        world.Insert(i + 1, p.Kind, right, oldRight, p.Bottom, p.Top);
                }
                else if (oldRight > right) p.Left = right;
                else world.Blocks.RemoveAt(i);
            }
            return changed;
        }

        internal float CrossGap(float x, out float span)
        {
            span = 0f;
            for (int i = 0; i < world.Gaps.Count; i++)
            {
                Gap gap = world.Gaps[i];
                if (gap.Rewarded || x < gap.Left || x > gap.Right) continue;
                foreach (Piece p in world.Blocks)
                {
                    if (p.Kind != PieceKind.Bridge || p.Left > gap.Left || p.Right < gap.Right)
                        continue;
                    gap.Rewarded = true;
                    span = gap.Right - gap.Left;
                    // A bridge twice the necessary length has no speed reward.
                    return Math.Max(0f, 1f - ((p.Right - p.Left) - span) / span);
                }
            }
            return 0f;
        }
    }
}
