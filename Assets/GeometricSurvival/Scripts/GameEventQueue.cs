using System;
using System.Collections.Generic;

namespace GeometricSurvival
{
    // Event Queue: producers enqueue facts; the consumer processes them later.
    // This is separate from Observer notifications, which run synchronously.
    public sealed class GameEventQueue<T>
    {
        private readonly Queue<T> pending = new Queue<T>();
        public void Enqueue(T gameEvent) { pending.Enqueue(gameEvent); }
        public void Drain(Action<T> consume)
        {
            int count = pending.Count;
            for (int i = 0; i < count; i++) consume(pending.Dequeue());
        }
    }
    public readonly struct EnemyKilledEvent
    {
        public readonly int Points;
        public EnemyKilledEvent(int points) { Points = points; }
    }
}
