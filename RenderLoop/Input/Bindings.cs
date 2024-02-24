namespace RenderLoop.Input
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DevDecoder.HIDDevices;

    public class Bindings<T>
    {
        private readonly List<EachBinding> eachBindings = new();
        private readonly List<CurrentBinding> currentBindings = new();

        public IEnumerable<(long Timestamp, double Value, T Binding)> Bind(ControlChangeTracker tracker, ControlChange[] changes)
        {
            foreach (var c in changes)
            {
                if (double.IsNaN(c.Value))
                {
                    continue;
                }

                foreach (var eachBinding in this.eachBindings)
                {
                    if (eachBinding.Predicate(c.Control))
                    {
                        if (c.Value >= 0.75 && c.PreviousValue < 0.75)
                        {
                            yield return (c.Timestamp, c.Value, eachBinding.Value);
                        }
                    }
                }
            }

            foreach (var currentBinding in this.currentBindings)
            {
                var matching = tracker.Controls
                    .Where(currentBinding.Predicate)
                    .Select(c => tracker[c]);

                var lastTime = 0L;
                var lastValue = double.NaN;
                foreach (var c in matching)
                {
                    if (double.IsNaN(c.Value))
                    {
                        if (double.IsNaN(lastValue))
                        {
                            lastTime = c.Timestamp;
                        }
                    }
                    else if (c.Timestamp > lastTime)
                    {
                        lastTime = c.Timestamp;
                        lastValue = c.Value;
                    }
                }

                yield return (lastTime, lastValue, currentBinding.Value);
            }
        }

        public void BindCurrent(Func<Control, bool> predicate, T value)
        {
            this.currentBindings.Add(new CurrentBinding(predicate, value));
        }

        public void BindEach(Func<Control, bool> predicate, T value)
        {
            this.eachBindings.Add(new EachBinding(predicate, value));
        }

        private record struct EachBinding(Func<Control, bool> Predicate, T Value);

        private record struct CurrentBinding(Func<Control, bool> Predicate, T Value);
    }
}
