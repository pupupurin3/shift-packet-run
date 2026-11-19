using System;

namespace ShiftRunner
{
    /// <summary>A lane is one row of blocks. Select its columns in either direction.</summary>
    public sealed class RangeSelection
    {
        // Placing an anchor inside the foundation includes it in an erase command.
        public const float FloorDepth = 0.5f;
        public bool HasStart { get; private set; }
        public bool HasEnd { get; private set; }
        public float StartX { get; private set; }
        public float StartY { get; private set; }
        public float EndX { get; private set; }
        public float EndY { get; private set; }
        public float Left { get { return Math.Min(StartX, EndX); } }
        public float Right { get { return Math.Max(StartX, EndX); } }
        public bool IncludesFloor { get { return HasStart && Math.Min(StartY, EndY) <= -FloorDepth; } }

        internal void Set(float x, float y, bool extend)
        {
            if (!extend)
            {
                HasStart = true;
                HasEnd = false;
                StartX = EndX = x;
                StartY = EndY = y;
            }
            else if (HasStart)
            {
                EndX = x;
                EndY = y;
                HasEnd = true;
            }
        }

        internal void Clear()
        {
            HasStart = HasEnd = false;
        }
    }
}
