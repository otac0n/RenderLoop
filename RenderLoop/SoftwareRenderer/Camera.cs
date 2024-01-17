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
                    this.ComputeMatrices();
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
                    this.ComputeMatrices();
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
                    this.ComputeMatrices();
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
                    this.ClearMatrices();
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
                    this.ClearMatrices();
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
                    this.ClearMatrices();
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
                    this.ClearMatrices();
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
                    this.ClearMatrices();
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
                    this.ClearMatrices();
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
                    this.ClearMatrices();
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
                    this.ClearMatrices();
                }
            }
        }

        /// <summary>
        /// Transform points from World space to Camera space.
        /// </summary>
        /// <param name="world">The position in World space.</param>
        /// <returns>The coordinates in Camera space.</returns>
        public Vector3 TransformToCameraSpace(Vector3 world) =>
            Vector3.Transform(world, this.WorldToCamera);

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
        public Vector4 TransformToNDCSpace(Vector3 world) =>
            TransformClipToNDC(this.TransformToClipSpace(world));

        /// <summary>
        /// Transform points from Clip space to NDC (Normalized Device Coordinate) space.
        /// </summary>
        /// <param name="clip">The position in Clip space.</param>
        /// <returns>The coordinates in NDC space. The <see cref="W"/> coordinate is unmodified from Clip Space.</returns>
        /// <remarks>The <see cref="Vector4.W"/> field is preserved unmodified in order to maintain perspective information.</remarks>
        public static Vector4 TransformClipToNDC(Vector4 clip) =>
            new(clip.X / clip.W, clip.Y / clip.W, clip.Z / Math.Abs(clip.W), clip.W);

        /// <summary>
        /// Transform points from World space to Screen space.
        /// </summary>
        /// <param name="world">The position in World space.</param>
        /// <returns>The coordinates in Screen space. The <see cref="W"/> coordinate is unmodified from Clip Space.</returns>
        public Vector4 TransformToScreenSpace(Vector3 world) =>
            this.TransformNDCToScreen(this.TransformToNDCSpace(world));

        /// <summary>
        /// Transform points from NDC (Normalized Device Coordinate) space to Screen space.
        /// </summary>
        /// <param name="ndc">The position in NDC space.</param>
        /// <returns>The coordinates in Screen space. The <see cref="W"/> coordinate is unmodified from Clip Space.</returns>
        public Vector4 TransformNDCToScreen(Vector4 ndc) =>
            new((ndc.X + 1) / 2 * this.Width, (1 - ndc.Y) / 2 * this.Height, ndc.Z, ndc.W);

        private void ClearMatrices()
        {
            this.worldToCamera = null;
            this.projection = null;
            this.matrix = null;
        }

        private void ComputeMatrices()
        {
            this.worldToCamera = Matrix4x4.CreateLookAt(this.Position, this.Position + this.Direction, this.Up);
            this.projection = Matrix4x4.CreatePerspectiveFieldOfView(this.FieldOfView, this.AspectRatio, this.NearPlane, this.FarPlane);
            this.matrix = this.worldToCamera * this.projection;
        }
    }
}
