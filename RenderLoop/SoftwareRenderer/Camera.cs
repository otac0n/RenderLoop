namespace RenderLoop.SofwareRenderer
{
    using System;
    using System.Numerics;

    public class Camera
    {
        private Vector3 position = Vector3.Zero;
        private Vector3 direction = Vector3.UnitX;
        private Vector3 up = Vector3.UnitZ;
        private float fieldOfView = (float)(Math.Tau / 5);
        private float aspectRatio = 1.0f;
        private float nearPlane = 0.1f;
        private float farPlane = 10.0f;
        private Matrix4x4? matrix;

        public Matrix4x4 Matrix
        {
            get
            {
                if (this.matrix == null)
                {
                    this.ComputeMatrix();
                }

                return this.matrix!.Value;
            }
        }

        public Vector3 Up
        {
            get => this.up;
            set
            {
                value = Vector3.Normalize(value);
                if (this.up != value)
                {
                    this.up = value;
                    this.matrix = null;
                }
            }
        }

        public Vector3 Position
        {
            get => this.position;
            set
            {
                if (this.position != value)
                {
                    this.position = value;
                    this.matrix = null;
                }
            }
        }

        public float FieldOfView
        {
            get => this.fieldOfView;
            set
            {
                if (this.fieldOfView != value)
                {
                    this.fieldOfView = value;
                    this.matrix = null;
                }
            }
        }

        public float AspectRatio
        {
            get => this.aspectRatio;
            set
            {
                if (this.aspectRatio != value)
                {
                    this.aspectRatio = value;
                    this.matrix = null;
                }
            }
        }

        public float NearPlane
        {
            get => this.nearPlane;
            set
            {
                if (this.nearPlane != value)
                {
                    this.nearPlane = value;
                    this.matrix = null;
                }
            }
        }

        public float FarPlane
        {
            get => this.farPlane;
            set
            {
                if (this.farPlane != value)
                {
                    this.farPlane = value;
                    this.matrix = null;
                }
            }
        }

        public Vector3 Direction
        {
            get => this.direction;
            set
            {
                if (this.direction != value)
                {
                    this.direction = value;
                    this.matrix = null;
                }
            }
        }

        public Vector3 Transform(Vector3 position)
        {
            var vp = Vector4.Transform(position, this.Matrix);
            return new Vector3(vp.X, vp.Y, vp.Z) / Math.Abs(vp.W);
        }

        private void ComputeMatrix()
        {
            this.matrix = Matrix4x4.CreateLookAt(this.Position, this.Position + this.Direction, this.Up) * Matrix4x4.CreatePerspectiveFieldOfView(this.FieldOfView, this.AspectRatio, this.NearPlane, this.FarPlane);
        }
    }
}
