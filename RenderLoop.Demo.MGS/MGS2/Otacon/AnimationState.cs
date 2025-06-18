// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS2.Otacon
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Sprite = (int Sprite, int Index);

    internal class AnimationState
    {
        public enum State
        {
            Invisible,
            Neutral,
            Shrug,
            ThumbsUp,
            Laughing,
            Smile,
            Angry,
        }

        [Flags]
        private enum Direction
        {
            None,
            Left,
            Center,
            Right,
        }

        [Flags]
        private enum Expression
        {
            Neutral,
            Blink,
            Wink,
            Happy,
            Sad,
            Angry,
            Reserved,
            Concerned,
            Laughing,
            Embarassed,
            Sheepish,
            Frustrated,
            Frightened,
            Terrified,
        }

        [Flags]
        private enum Posture
        {
            Neutral,
            Pronated,
            Supinated,
            Abducting,
            Adjusting,
            Pondering,
            Guarding,
            Shrugging,
            DoubledOver,
            ThumbsUp,
            ThumbOut,
            ThumbBack,
            Clenched,
            Raised,
        }

        private static readonly Dictionary<Sprite, (Direction Face, Direction Eyes, Expression Expression, Direction Body, Posture Posture, Direction Legs)> Metadata = new()
        {
            { (0, 0),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Neutral,                   Direction.Left) },
            { (0, 1),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Pronated,                  Direction.Left) },
            { (0, 2),  (Direction.Left,                    Direction.Center, Expression.Sad,                          Direction.Left,   Posture.Supinated,                 Direction.Left) },
            { (0, 3),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Adjusting,                 Direction.Left) },
            { (0, 4),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Pondering,                 Direction.Left) },
            { (0, 5),  (Direction.Left,                    Direction.Center, Expression.Neutral | Expression.Blink,   Direction.Left,   Posture.Pondering,                 Direction.Left) },
            { (0, 6),  (Direction.Left,                    Direction.Left,   Expression.Neutral,                      Direction.Left,   Posture.Pondering,                 Direction.Left) },
            { (0, 7),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Pondering,                 Direction.Right | Direction.Center) },
            { (0, 8),  (Direction.Center,                  Direction.Center, Expression.Neutral,                      Direction.Center, Posture.Adjusting,                 Direction.Center) },
            { (0, 9),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Pondering,                 Direction.Right) },
            { (0, 10), (Direction.Left,                    Direction.Center, Expression.Neutral | Expression.Blink,   Direction.Left,   Posture.Pondering,                 Direction.Right) },
            { (0, 11), (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Pondering,                 Direction.Left | Direction.Center) },
            { (1, 0),  (Direction.Center,                  Direction.Center, Expression.Neutral,                      Direction.Center, Posture.ThumbsUp,                  Direction.Center) },
            { (1, 1),  (Direction.Center,                  Direction.Center, Expression.Neutral,                      Direction.Center, Posture.ThumbsUp | Posture.Raised, Direction.Center) },
            { (1, 2),  (Direction.Center,                  Direction.Center, Expression.Happy | Expression.Wink,      Direction.Center, Posture.ThumbsUp,                  Direction.Center) },
            { (1, 3),  (Direction.Center,                  Direction.Center, Expression.Happy | Expression.Wink,      Direction.Center, Posture.ThumbsUp | Posture.Raised, Direction.Center) },
            { (1, 4),  (Direction.Center,                  Direction.Center, Expression.Reserved,                     Direction.Center, Posture.Abducting,                 Direction.Center) },
            { (1, 5),  (Direction.Center,                  Direction.Center, Expression.Concerned,                    Direction.Center, Posture.Shrugging,                 Direction.Center) },
            { (1, 6),  (Direction.Center,                  Direction.Center, Expression.Happy,                        Direction.Center, Posture.Neutral,                   Direction.Center) },
            { (1, 7),  (Direction.Left,                    Direction.Center, Expression.Neutral | Expression.Wink,    Direction.Left,   Posture.ThumbOut,                  Direction.Left) }, // Opposite eye
            { (1, 8),  (Direction.Left,                    Direction.Center, Expression.Sheepish,                     Direction.Left,   Posture.Supinated,                 Direction.Left) },
            { (1, 9),  (Direction.Left,                    Direction.Center, Expression.Concerned,                    Direction.Left,   Posture.Supinated,                 Direction.Left) },
            { (1, 10), (Direction.Left,                    Direction.Center, Expression.Concerned | Expression.Blink, Direction.Left,   Posture.Supinated,                 Direction.Left) },
            { (1, 11), (Direction.Left,                    Direction.None,   Expression.Frustrated,                   Direction.Left,   Posture.Supinated,                 Direction.Left) },
            { (2, 0),  (Direction.Center,                  Direction.Center, Expression.Frightened,                   Direction.Center, Posture.Guarding,                  Direction.Center) },
            { (2, 1),  (Direction.Left,                    Direction.Center, Expression.Concerned,                    Direction.Left,   Posture.Supinated,                 Direction.Left) },
            { (2, 2),  (Direction.Left,                    Direction.Center, Expression.Laughing | Expression.Blink,  Direction.Left,   Posture.DoubledOver,               Direction.Center) },
            { (2, 3),  (Direction.Left,                    Direction.Center, Expression.Laughing | Expression.Blink,  Direction.Left,   Posture.DoubledOver,               Direction.Center) },
            { (2, 4),  (Direction.Left,                    Direction.Center, Expression.Laughing | Expression.Blink,  Direction.Left,   Posture.DoubledOver,               Direction.Center) },
            { (2, 5),  (Direction.Center,                  Direction.Center, Expression.Embarassed,                   Direction.Center, Posture.Guarding,                  Direction.Center) },
            { (2, 6),  (Direction.Left | Direction.Center, Direction.Center, Expression.Embarassed,                   Direction.Center, Posture.Adjusting,                 Direction.Center) },
            { (2, 7),  (Direction.Left,                    Direction.Center, Expression.Embarassed,                   Direction.Left,   Posture.Neutral,                   Direction.Left) },
            { (2, 8),  (Direction.Left,                    Direction.Left,   Expression.Embarassed,                   Direction.Left,   Posture.Neutral,                   Direction.Left) },
            { (2, 9),  (Direction.Center,                  Direction.None,   Expression.Terrified,                    Direction.Center, Posture.Guarding,                  Direction.Center) }, // Pale
            { (2, 10), (Direction.Right,                   Direction.None,   Expression.Frightened,                   Direction.Right,  Posture.Adjusting,                 Direction.Center) }, // Ghost
            { (2, 11), (Direction.None, Direction.None, Expression.Neutral, Direction.None, Posture.Neutral, Direction.None) }, // Blank
            { (3, 0),  (Direction.Right,                   Direction.None,   Expression.Frightened,                   Direction.Right,  Posture.DoubledOver,               Direction.Center) },
            { (3, 1),  (Direction.Right,                   Direction.None,   Expression.Frightened,                   Direction.Right,  Posture.DoubledOver,               Direction.Center) },
            { (3, 2),  (Direction.Center,                  Direction.None,   Expression.Angry,                        Direction.Center, Posture.Clenched,                  Direction.Center) },
            { (3, 3),  (Direction.Center,                  Direction.None,   Expression.Angry,                        Direction.Center, Posture.Clenched | Posture.Raised, Direction.Center) },
            { (3, 4),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Pondering,                 Direction.Right) },
            { (3, 5),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Adjusting,                 Direction.Right) },
            { (3, 6),  (Direction.Left,                    Direction.Center, Expression.Neutral | Expression.Wink,    Direction.Left,   Posture.ThumbOut,                  Direction.Right) },
            { (3, 7),  (Direction.Left,                    Direction.Center, Expression.Happy,                        Direction.Left,   Posture.ThumbBack,                 Direction.Right) },
            { (3, 8),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Neutral,                   Direction.Left) }, // Animate in
            { (3, 9),  (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Neutral,                   Direction.Left) }, // Animate in
            { (3, 10), (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Neutral,                   Direction.Left) }, // Animate in
            { (3, 11), (Direction.Left,                    Direction.Center, Expression.Neutral,                      Direction.Left,   Posture.Neutral,                   Direction.Left) }, // Animate in
        };

        private static readonly Dictionary<(State, State), Dictionary<Sprite, (TimeSpan Time, Sprite[] State)>> Transitions = new()
        {
            [(State.Invisible, State.Neutral)] = new()
            {
                { (2, 11), (TimeSpan.Zero, [(3, 8)]) },
                { (3, 8), (TimeSpan.FromMilliseconds(33), [(3, 9)]) },
                { (3, 9), (TimeSpan.FromMilliseconds(33), [(3, 10)]) },
                { (3, 10), (TimeSpan.FromMilliseconds(33), [(3, 11)]) },
                { (3, 11), (TimeSpan.FromMilliseconds(33), [(0, 0)]) },
            },

            [(State.Neutral, State.Neutral)] = new()
            {
                { (0, 0), (Timeout.InfiniteTimeSpan, []) },
            },

            [(State.Neutral, State.Shrug)] = new()
            {
                { (0, 0), (TimeSpan.Zero, [(1, 4)]) },
            },

            [(State.Shrug, State.Shrug)] = new()
            {
                { (1, 4), (TimeSpan.FromMilliseconds(66), [(1, 5)]) },
                { (1, 5), (Timeout.InfiniteTimeSpan, []) },
            },

            [(State.Shrug, State.Neutral)] = new()
            {
                { (1, 5), (TimeSpan.FromMilliseconds(66), [(1, 4)]) },
                { (1, 4), (TimeSpan.FromMilliseconds(66), [(0, 0)]) },
            },

            [(State.Neutral, State.Angry)] = new()
            {
                { (0, 0), (TimeSpan.Zero, [(3, 2), (3, 3)]) },
            },

            [(State.Angry, State.Angry)] = new()
            {
                { (3, 2), (TimeSpan.FromMilliseconds(200), [(3, 3)]) },
                { (3, 3), (TimeSpan.FromMilliseconds(200), [(3, 2)]) },
            },

            [(State.Angry, State.Neutral)] = new()
            {
                { (3, 2), (TimeSpan.FromMilliseconds(66), [(0, 0)]) },
                { (3, 3), (TimeSpan.FromMilliseconds(66), [(0, 0)]) },
            },

            [(State.Neutral, State.Laughing)] = new()
            {
                { (0, 0), (TimeSpan.Zero, [(2, 2), (2, 3), (2, 4)]) },
            },

            [(State.Laughing, State.Laughing)] = new()
            {
                { (2, 2), (TimeSpan.FromMilliseconds(200), [(2, 3), (2, 4)]) },
                { (2, 3), (TimeSpan.FromMilliseconds(200), [(2, 2), (2, 4)]) },
                { (2, 4), (TimeSpan.FromMilliseconds(200), [(2, 2), (2, 3)]) },
            },

            [(State.Laughing, State.Neutral)] = new()
            {
                { (2, 2), (TimeSpan.FromMilliseconds(66), [(0, 0)]) },
                { (2, 3), (TimeSpan.FromMilliseconds(66), [(2, 2)]) },
                { (2, 4), (TimeSpan.FromMilliseconds(66), [(2, 2)]) },
            },

            [(State.Neutral, State.ThumbsUp)] = new()
            {
                { (0, 0), (TimeSpan.Zero, [(1, 0)]) },
            },

            [(State.ThumbsUp, State.ThumbsUp)] = new()
            {
                { (1, 0), (TimeSpan.FromMilliseconds(200), [(1, 1)]) },
                { (1, 1), (TimeSpan.FromMilliseconds(200), [(1, 2)]) },
                { (1, 2), (TimeSpan.FromMilliseconds(200), [(1, 3)]) },
                { (1, 3), (Timeout.InfiniteTimeSpan, []) },
            },

            [(State.ThumbsUp, State.Smile)] = new()
            {
                { (1, 0), (TimeSpan.FromMilliseconds(200), [(1, 1)]) },
                { (1, 1), (TimeSpan.FromMilliseconds(200), [(1, 2)]) },
                { (1, 2), (TimeSpan.FromMilliseconds(200), [(1, 3)]) },
                { (1, 3), (TimeSpan.FromMilliseconds(200), [(1, 6)]) },
            },

            [(State.ThumbsUp, State.Neutral)] = new()
            {
                { (1, 0), (TimeSpan.FromMilliseconds(66), [(0, 0)]) },
                { (1, 1), (TimeSpan.FromMilliseconds(66), [(0, 0)]) },
                { (1, 2), (TimeSpan.FromMilliseconds(66), [(1, 6)]) },
                { (1, 3), (TimeSpan.FromMilliseconds(66), [(1, 6)]) },
                { (1, 6), (TimeSpan.FromMilliseconds(66), [(0, 0)]) },
            },

            [(State.Neutral, State.Smile)] = new()
            {
                { (0, 0), (TimeSpan.Zero, [(1, 6)]) },
            },

            [(State.Smile, State.Smile)] = new()
            {
                { (1, 6), (Timeout.InfiniteTimeSpan, []) },
            },

            [(State.Smile, State.Neutral)] = new()
            {
                { (1, 6), (TimeSpan.Zero, [(0, 0)]) },
            },
        };

        private readonly Stopwatch selectedTime = new();
        private (int Sprite, int Index) currentSprite;

        public AnimationState()
        {
            this.CurrentSprite = (2, 11);
            this.CurrentState = State.Invisible;
            this.TargetState = State.Neutral;
        }

        public State CurrentState { get; private set; }

        public State TargetState { get; set; }

        public Sprite CurrentSprite
        {
            get => this.currentSprite;
            private set
            {
                this.currentSprite = value;
                this.selectedTime.Restart();
            }
        }

        public bool Update()
        {
            var targetKey = (this.TargetState, this.TargetState);
            Transitions.TryGetValue(targetKey, out var targetSprites);
            void UpdateCurrentState()
            {
                if (this.CurrentState != this.TargetState && targetSprites != null && targetSprites.ContainsKey(this.CurrentSprite))
                {
                    this.CurrentState = this.TargetState;
                }
            }

            UpdateCurrentState();

            var key = (this.CurrentState, this.TargetState);
            var found = false;
            if (Transitions.TryGetValue(key, out var animation))
            {
                if (animation.TryGetValue(this.CurrentSprite, out var next))
                {
                    found = true;
                    if (next.Time != Timeout.InfiniteTimeSpan && this.selectedTime.Elapsed >= next.Time)
                    {
                        this.CurrentSprite = next.State[Random.Shared.Next(next.State.Length)];
                        UpdateCurrentState();
                        return true;
                    }
                }
            }

            if (!found)
            {
                if (targetSprites != null && targetSprites.Count > 0)
                {
                    this.CurrentSprite = targetSprites.Keys.First();
                    this.CurrentState = this.TargetState;
                    return true;
                }
            }

            return false;
        }
    }
}
