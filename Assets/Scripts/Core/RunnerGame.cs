using System;

namespace ShiftRunner
{
    public enum GameCue { Attack, Build, Erase, Death, Boost }

    /// <summary>Fixed-step runner simulation. World units are metres. Y is positive upwards.</summary>
    public sealed class RunnerGame
    {
        public const float PacketSize = 0.8f;
        public const float AttackReach = 2.65f;
        private const float Half = PacketSize / 2f;
        private const float Step = 1f / 120f;

        public readonly RunnerWorld World = new RunnerWorld();
        public readonly RangeSelection Selection = new RangeSelection();
        public event Action<GameCue> Cue;
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Speed { get; private set; }
        public float Distance { get { return X; } }
        public float Best { get; set; }
        public float Boost01 { get; private set; }
        public float BoostTime { get; private set; }
        public float AttackTime { get; private set; }
        public bool Grounded { get; private set; }
        public int Deaths { get; private set; }

        private readonly PlatformEditor editor;
        private readonly int seed;
        private int run;
        private float fallSpeed, boostStrength, remainder;

        public RunnerGame(int seed = 1)
        {
            this.seed = seed;
            editor = new PlatformEditor(World);
            Restart();
        }

        public void Restart()
        {
            World.Reset(unchecked(seed + run++ * 4099));
            Selection.Clear();
            X = 0f;
            Y = Half;
            Grounded = true;
            fallSpeed = boostStrength = Boost01 = BoostTime = AttackTime = remainder = 0f;
            Speed = BaseSpeed();
        }

        public void Click(float worldX, float worldY, bool shift)
        {
            Selection.Set(World.Snap(worldX), worldY, shift);
            if (shift) return;
            AttackTime = 0.17f;
            StrikeEnemies();
            Emit(GameCue.Attack);
        }

        public bool Build()
        {
            if (!Selection.HasEnd || Selection.Right <= Selection.Left) return false;
            editor.Build(Selection.Left, Selection.Right);
            Emit(GameCue.Build);
            return true;
        }

        public bool Erase()
        {
            if (!Selection.HasEnd || Selection.Right <= Selection.Left) return false;
            bool changed = editor.Cut(Selection.Left, Selection.Right, includeFloor: Selection.IncludesFloor);
            if (changed) Emit(GameCue.Erase);
            return changed;
        }

        public void Tick(float seconds)
        {
            // A suspended browser tab should never fast-forward through an entire level.
            remainder += Math.Min(0.25f, Math.Max(0f, seconds));
            while (remainder >= Step)
            {
                remainder -= Step;
                Advance();
            }
        }

        private float BaseSpeed()
        {
            return 4.5f + Math.Min(5.5f, X * 0.008f);
        }

        private void Advance()
        {
            BoostTime = Math.Max(0f, BoostTime - Step);
            Boost01 = boostStrength * Math.Min(1f, BoostTime / 0.25f);
            Speed = BaseSpeed() * (1f + 0.72f * Boost01);
            X += Speed * Step;
            Best = Math.Max(Best, Distance);
            World.Stream(X);

            float previousFeet = Y - Half;
            fallSpeed -= 29f * Step;
            Y += fallSpeed * Step;
            Grounded = false;

            // Land only from above. A late bridge cannot teleport a falling packet up.
            foreach (Piece p in World.Blocks)
            {
                if (p.Kind != PieceKind.Ground && p.Kind != PieceKind.Bridge) continue;
                if (!p.Overlaps(X - Half, X + Half)) continue;
                if (previousFeet >= p.Top - 0.0001f && Y - Half <= p.Top)
                {
                    Y = p.Top + Half;
                    fallSpeed = 0f;
                    Grounded = true;
                    break;
                }
            }

            if (AttackTime > 0f)
            {
                StrikeEnemies();
                AttackTime = Math.Max(0f, AttackTime - Step);
            }
            if (Y < -4.4f || HitObstacle())
            {
                Deaths++;
                Restart();
                Emit(GameCue.Death);
                return;
            }

            if (Grounded)
            {
                float span;
                float quality = editor.CrossGap(X, out span);
                if (quality > 0f)
                {
                    boostStrength = Boost01 = quality;
                    Speed = BaseSpeed() * (1f + 0.72f * quality);
                    BoostTime = span / Speed + 0.8f;
                    Emit(GameCue.Boost);
                }
            }
        }

        private bool HitObstacle()
        {
            foreach (Piece p in World.Blocks)
            {
                if (p.Kind == PieceKind.Ground || p.Kind == PieceKind.Bridge) continue;
                if (p.Overlaps(X - Half, X + Half) && Y + Half >= p.Bottom && Y - Half <= p.Top)
                    return true;
            }
            return false;
        }

        private void StrikeEnemies()
        {
            for (int i = World.Blocks.Count - 1; i >= 0; i--)
            {
                Piece p = World.Blocks[i];
                if (p.Kind == PieceKind.Enemy && p.Overlaps(X, X + Half + AttackReach)
                    && Y + Half + 0.35f > p.Bottom && Y - Half - 0.35f < p.Top)
                    World.Blocks.RemoveAt(i);
            }
        }

        private void Emit(GameCue cue)
        {
            if (Cue != null) Cue(cue);
        }
    }
}
