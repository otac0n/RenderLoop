namespace RenderLoop.Input
{
    using System;
    using System.Numerics;

    public static class InputShapes
    {
        public static Func<double, double> Clamp(double min, double max) =>
            x => Math.Clamp(x, min, max);

        public static Func<float, float> Clamp(float min, float max) =>
            x => Math.Clamp(x, min, max);

        public static Func<T, T> Clamp<T>(T min, T max)
            where T : IFloatingPoint<T> =>
                x =>
                    x < min ? min :
                    x > max ? max :
                    x;

        public static T ShapeDeadZone<T>(T value, T deadZone, T max)
            where T : IFloatingPoint<T>
        {
            value = T.Abs(value);
            return
                value < deadZone ? T.Zero :
                value >= max ? max / value :
                (value - deadZone) / ((max - deadZone) * value);
        }
    }
}
