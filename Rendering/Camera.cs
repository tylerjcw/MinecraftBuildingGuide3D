using System;
using OpenTK.Mathematics;

namespace MinecraftBuildingGuide3D.Rendering
{
    /// <summary>
    /// Orbital camera that rotates around a target point
    /// </summary>
    public class Camera
    {
        // Camera position in spherical coordinates
        private float _distance = 80f;
        private float _yaw = 45f;      // Horizontal rotation (degrees)
        private float _pitch = 30f;    // Vertical rotation (degrees)

        // Target point the camera orbits around
        public Vector3 Target { get; set; } = Vector3.Zero;

        // View parameters
        public float FieldOfView { get; set; } = 45f;
        public float AspectRatio { get; set; } = 1.0f;
        public float NearPlane { get; set; } = 0.1f;
        public float FarPlane { get; set; } = 2000f;

        // Limits
        public float MinDistance { get; set; } = 5f;
        public float MaxDistance { get; set; } = 1000f;
        public float MinPitch { get; set; } = -89f;
        public float MaxPitch { get; set; } = 89f;

        // Sensitivity
        public float RotationSensitivity { get; set; } = 0.5f;
        public float ZoomSensitivity { get; set; } = 2f;
        public float PanSensitivity { get; set; } = 0.1f;

        public float Distance
        {
            get => _distance;
            set => _distance = Math.Clamp(value, MinDistance, MaxDistance);
        }

        public float Yaw
        {
            get => _yaw;
            set => _yaw = value % 360f;
        }

        public float Pitch
        {
            get => _pitch;
            set => _pitch = Math.Clamp(value, MinPitch, MaxPitch);
        }

        /// <summary>
        /// Calculate camera position from spherical coordinates
        /// </summary>
        public Vector3 Position
        {
            get
            {
                float yawRad = MathHelper.DegreesToRadians(_yaw);
                float pitchRad = MathHelper.DegreesToRadians(_pitch);

                float x = _distance * MathF.Cos(pitchRad) * MathF.Sin(yawRad);
                float y = _distance * MathF.Sin(pitchRad);
                float z = _distance * MathF.Cos(pitchRad) * MathF.Cos(yawRad);

                return Target + new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Get the view matrix
        /// </summary>
        public Matrix4 ViewMatrix => Matrix4.LookAt(Position, Target, Vector3.UnitY);

        /// <summary>
        /// Get the projection matrix
        /// </summary>
        public Matrix4 ProjectionMatrix => Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(FieldOfView),
            AspectRatio,
            NearPlane,
            FarPlane
        );

        /// <summary>
        /// Combined view-projection matrix
        /// </summary>
        public Matrix4 ViewProjectionMatrix => ViewMatrix * ProjectionMatrix;

        /// <summary>
        /// Rotate camera around target
        /// </summary>
        public void Rotate(float deltaYaw, float deltaPitch)
        {
            Yaw += deltaYaw * RotationSensitivity;
            Pitch += deltaPitch * RotationSensitivity;
        }

        /// <summary>
        /// Zoom in/out
        /// </summary>
        public void Zoom(float delta)
        {
            Distance -= delta * ZoomSensitivity;
        }

        /// <summary>
        /// Pan the camera (move target point)
        /// </summary>
        public void Pan(float deltaX, float deltaY)
        {
            float yawRad = MathHelper.DegreesToRadians(_yaw);

            // Calculate right and up vectors relative to camera orientation
            Vector3 right = new Vector3(MathF.Cos(yawRad), 0, -MathF.Sin(yawRad));
            Vector3 up = Vector3.UnitY;

            Target += right * deltaX * PanSensitivity * (_distance / 50f);
            Target += up * deltaY * PanSensitivity * (_distance / 50f);
        }

        /// <summary>
        /// Reset camera to default position
        /// </summary>
        public void Reset()
        {
            _distance = 80f;
            _yaw = 45f;
            _pitch = 30f;
            Target = Vector3.Zero;
        }

        /// <summary>
        /// Set camera to look at a specific bounding box
        /// </summary>
        public void FocusOnBounds(Vector3 min, Vector3 max)
        {
            Vector3 center = (min + max) / 2f;
            float size = (max - min).Length;

            Target = center;
            Distance = size * 1.5f;
        }

        /// <summary>
        /// Set camera to predefined view angles
        /// </summary>
        public void FocusOnBounds(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            // Calculate center of bounding box
            float centerX = (minX + maxX) / 2f;
            float centerY = (minY + maxY) / 2f;
            float centerZ = (minZ + maxZ) / 2f;

            Target = new Vector3(centerX, centerY, centerZ);

            // Calculate required distance to fit the shape
            float sizeX = maxX - minX;
            float sizeY = maxY - minY;
            float sizeZ = maxZ - minZ;
            float maxSize = Math.Max(sizeX, Math.Max(sizeY, sizeZ));

            // Set distance based on shape size with some padding
            _distance = Math.Max(MinDistance, maxSize * 1.5f);
            _distance = Math.Min(_distance, MaxDistance);

            // Reset to isometric-ish view
            _yaw = 45f;
            _pitch = 30f;
        }

        public void SetView(CameraView view)
        {
            switch (view)
            {
                case CameraView.Front:
                    _yaw = 0f; _pitch = 0f;
                    break;
                case CameraView.Back:
                    _yaw = 180f; _pitch = 0f;
                    break;
                case CameraView.Left:
                    _yaw = 90f; _pitch = 0f;
                    break;
                case CameraView.Right:
                    _yaw = -90f; _pitch = 0f;
                    break;
                case CameraView.Top:
                    _yaw = 0f; _pitch = 89f;
                    break;
                case CameraView.Bottom:
                    _yaw = 0f; _pitch = -89f;
                    break;
                case CameraView.Isometric:
                    _yaw = 45f; _pitch = 30f;
                    break;
            }
        }
    }

    public enum CameraView
    {
        Front,
        Back,
        Left,
        Right,
        Top,
        Bottom,
        Isometric
    }
}