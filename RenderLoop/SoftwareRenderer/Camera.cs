namespace RenderLoop.SoftwareRenderer
{
    using System;
    using System.Numerics;

    public class Camera
    {
        private Vector3 position = Vector3.Zero;
        private Vector3 direction = Vector3.UnitX;
        private Vector3 up = Vector3.UnitZ;
        private float fieldOfView = (float)(Math.Tau / 5);
        private float width = 1.0f;
        private float height = 1.0f;
        private float nearPlane = 0.1f;
        private float farPlane = 10.0f;
        private Matrix4x4? matrix;
        private Matrix4x4? worldToCamera;
        private Matrix4x4? projection;

        public Matrix4x4 WorldToCamera
        {
            get
            {
                if (this.worldToCamera == null)
                {
                    this.ComputeMatrix();
                }

                return this.worldToCamera!.Value;
            }
        }

        public Matrix4x4 Projection
        {
            get
            {
                if (this.projection == null)
                {
                    this.ComputeMatrix();
                }

                return this.projection!.Value;
            }
        }

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

        public Vector3 Right => Vector3.Cross(this.direction, this.up);

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

        public float Width
        {
            get => this.width;
            set
            {
                if (this.width != value)
                {
                    this.width = value;
                    this.matrix = null;
                }
            }
        }

        public float Height
        {
            get => this.height;
            set
            {
                if (this.height != value)
                {
                    this.height = value;
                    this.matrix = null;
                }
            }
        }

        public float AspectRatio => this.width / this.height;

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

        /// <summary>
        /// Transform points from World space to Camera space.
        /// </summary>
        /// <param name="world">The position in World space.</param>
        /// <returns>The coordinates in Camera space.</returns>
        public Vector4 TransformToCameraSpace(Vector3 world) =>
            Vector4.Transform(world, this.WorldToCamera);

        /// <summary>
        /// Transform points from World space to Clip space.
        /// </summary>
        /// <param name="world">The position in World space.</param>
        /// <returns>The coordinates in Clip space.</returns>
        public Vector4 TransformToClipSpace(Vector3 world) =>
            Vector4.Transform(world, this.Matrix);

        /// <summary>
        /// Transform points from Camera space to Clip space.
        /// </summary>
        /// <param name="camera">The position in Camera space.</param>
        /// <returns>The coordinates in Clip space.</returns>
        public Vector4 TransformCameraToClip(Vector3 camera) =>
            Vector4.Transform(camera, this.Projection);

        /// <summary>
        /// Transform points from Camera space to Clip space.
        /// </summary>
        /// <param name="camera">The position in Camera space.</param>
        /// <returns>The coordinates in Clip space.</returns>
        public Vector4 TransformCameraToClip(Vector4 camera) =>
            Vector4.Transform(camera, this.Projection);

        /// <summary>
        /// Transform points from World space to NDC (Normalized Device Coordinate) space.
        /// </summary>
        /// <param name="world">The position in World space.</param>
        /// <returns>The coordinates in NDC space.</returns>
        /// <remarks>Projective information is lost by dividing by the <see cref="Vector4.W"/> component.</remarks>
        public Vector3 TransformToNDCSpace(Vector3 world) =>
            TransformClipToNDC(this.TransformToClipSpace(world));

        /// <summary>
        /// Transform points from Clip space to NDC (Normalized Device Coordinate) space.
        /// </summary>
        /// <param name="clip">The position in Clip space.</param>
        /// <returns>The coordinates in NDC space.</returns>
        /// <remarks>This operation destroys perspective information.</remarks>
        public static Vector3 TransformClipToNDC(Vector4 clip) =>
            new(clip.X / clip.W, clip.Y / clip.W, clip.Z / clip.W);

        /// <summary>
        /// Transform points from World space to Screen space.
        /// </summary>
        /// <param name="world">The position in World space.</param>
        /// <returns>The coordinates in Screen space.</returns>
        /// <remarks>Projective information is lost by dividing by the <see cref="Vector4.W"/> component.</remarks>
        public Vector3 TransformToScreenSpace(Vector3 world) =>
            this.TransformNDCToScreen(this.TransformToNDCSpace(world));

        /// <summary>
        /// Transform points from NDC (Normalized Device Coordinate) space to Screen space.
        /// </summary>
        /// <param name="ndc">The position in NDC space.</param>
        /// <returns>The coordinates in Screen space.</returns>
        public Vector3 TransformNDCToScreen(Vector3 ndc)
        {
            var s = ndc;
            s.X = (s.X + 1) / 2 * this.Width;
            s.Y = (1 - s.Y) / 2 * this.Height;
            return s;
        }

        private void ComputeMatrix()
        {
            this.worldToCamera = Matrix4x4.CreateLookAt(this.Position, this.Position + this.Direction, this.Up);
            this.projection = Matrix4x4.CreatePerspectiveFieldOfView(this.FieldOfView, this.AspectRatio, this.NearPlane, this.FarPlane);
            this.matrix = this.worldToCamera * this.projection;
        }
    }
}
