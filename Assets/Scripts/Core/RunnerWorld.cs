using System;
using System.Collections.Generic;

namespace ShiftRunner
{
    public enum PieceKind { Ground, Bridge, Wall, Enemy, Hazard }

    public sealed class Piece
    {
        public int Id { get; private set; }
        public PieceKind Kind { get; private set; }
        public float Left { get; internal set; }
        public float Right { get; internal set; }
        public float Bottom { get; private set; }
        public float Top { get; private set; }

        internal Piece(int id, PieceKind kind, float left, float right, float bottom, float top)
        {
            Id = id;
            Kind = kind;
            Left = left;
            Right = right;
            Bottom = bottom;
            Top = top;
        }

        internal bool Overlaps(float left, float right)
        {
            return Left < right && Right > left;
        }
    }

    public sealed class Gap
    {
        public readonly float Left, Right;
        internal bool Rewarded;

        internal Gap(float left, float right)
        {
            Left = left;
            Right = right;
        }
    }

    public sealed class RunnerWorld
    {
        internal readonly List<Piece> Blocks = new List<Piece>();
        private readonly List<Gap> gaps = new List<Gap>();
        public IReadOnlyList<Piece> Pieces { get { return Blocks; } }
        public IReadOnlyList<Gap> Gaps { get { return gaps; } }

        private Random random;
        private float floorEnd;
        private int segment, nextId;

        internal void Reset(int seed)
        {
            Blocks.Clear();
            gaps.Clear();
            random = new Random(seed);
            segment = 0;
            floorEnd = 23f;
            Add(PieceKind.Ground, -35f, floorEnd, -3f, 0f);
            Stream(0f);
        }

        internal void Stream(float playerX)
        {
            // Keep a modest buffer on both sides, even after a long run.
            while (floorEnd < playerX + 110f)
                GenerateSegment();

            float cutoff = playerX - 45f;
            for (int i = Blocks.Count - 1; i >= 0; i--)
                if (Blocks[i].Right < cutoff) Blocks.RemoveAt(i);
            for (int i = gaps.Count - 1; i >= 0; i--)
                if (gaps[i].Right < cutoff) gaps.RemoveAt(i);
        }

        private void GenerateSegment()
        {
            float difficulty = Math.Min(1f, floorEnd / 650f);
            int gapWidth = segment == 0 ? 5 : random.Next(4, 8 + (int)(5f * difficulty));
            float start = floorEnd + gapWidth;
            int length = segment == 0 ? 22 : random.Next(17, 28);
            AddGap(floorEnd, start);
            floorEnd = start + length;
            Add(PieceKind.Ground, start, floorEnd, -3f, 0f);

            // Introduce one obstacle at a time, with room to edit after each gap.
            if (segment < 3 || random.NextDouble() < 0.64 + 0.33 * difficulty)
            {
                int type = segment < 3 ? segment : random.Next(3);
                float x = start + (segment == 0 ? 10 : random.Next(6, 11));
                AddObstacle(type, x);
                if (segment >= 4 && floorEnd - x >= 14f && random.NextDouble() < 0.48 * difficulty)
                    AddObstacle(random.Next(3), x + 8f);
            }
            segment++;
        }

        private void AddObstacle(int type, float x)
        {
            if (type == 0) Add(PieceKind.Enemy, x, x + 1f, 0.12f, 1.12f);
            else if (type == 1) Add(PieceKind.Wall, x, x + 1.5f, 0f, 2.5f);
            else Add(PieceKind.Hazard, x, x + 1.5f, 0f, 0.8f);
        }

        internal void Add(PieceKind kind, float left, float right, float bottom, float top)
        {
            Insert(Blocks.Count, kind, left, right, bottom, top);
        }

        internal void Insert(int index, PieceKind kind, float left, float right, float bottom, float top)
        {
            Blocks.Insert(index, new Piece(++nextId, kind, left, right, bottom, top));
        }

        // A removed strip can join several holes. Rendering, snapping and rewards all
        // use this single sorted list of continuous spans.
        internal void AddGap(float left, float right)
        {
            int first = 0;
            while (first < gaps.Count && gaps[first].Right < left) first++;
            int end = first;
            bool rewarded = false;
            while (end < gaps.Count && gaps[end].Left <= right)
            {
                Gap joined = gaps[end++];
                left = Math.Min(left, joined.Left);
                right = Math.Max(right, joined.Right);
                rewarded |= joined.Rewarded;
            }
            gaps.RemoveRange(first, end - first);
            gaps.Insert(first, new Gap(left, right) { Rewarded = rewarded });
        }

        internal float Snap(float x)
        {
            // Small magnet at the actual lips makes an exact compile practical at speed.
            float closest = (float)Math.Round(x * 4f) / 4f;
            float radius = 0.55f;
            foreach (Gap gap in gaps)
            {
                float leftDistance = Math.Abs(x - gap.Left);
                float rightDistance = Math.Abs(x - gap.Right);
                float distance = Math.Min(leftDistance, rightDistance);
                if (distance <= radius)
                {
                    closest = leftDistance <= rightDistance ? gap.Left : gap.Right;
                    radius = distance;
                }
            }
            return closest;
        }
    }
}
