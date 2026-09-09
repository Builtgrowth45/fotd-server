using FOMServer.Shared.Interop.FOMNetwork.Structs;

namespace FOMServer.World.Core.World
{
    internal class ServerPosition
    {
        private readonly Lock _syncRoot = new();

        private short _x;
        private short _y;
        private short _z;
        private ushort _rotation;

        public short X
        {
            get
            {
                lock (_syncRoot)
                {
                    return _x;
                }
            }
        }

        public short Y
        {
            get
            {
                lock (_syncRoot)
                {
                    return _y;
                }
            }
        }

        public short Z
        {
            get
            {
                lock (_syncRoot)
                {
                    return _z;
                }
            }
        }

        public ushort Rotation
        {
            get
            {
                lock (_syncRoot)
                {
                    return _rotation;
                }
            }
        }

        /// <summary>
        /// TEMPORARY, FOR TESTING, in the future, use translation and return distance between move
        /// </summary>
        public void ApplyUpdate(in PositionInterop p)
        {
            lock (_syncRoot)
            {
                _x = p.X;
                _y = p.Y;
                _z = p.Z;
            }
        }

        /// <summary>
        /// TEMPORARY, FOR TESTING, in the future, use translation and return distance between move
        /// </summary>
        public void ApplyUpdate(in PositionRotationInterop p)
        {
            lock (_syncRoot)
            {
                _x = p.Pos.X;
                _y = p.Pos.Y;
                _z = p.Pos.Z;
                _rotation = p.Rot;
            }
        }

        /// <summary>
        /// Whether this position lies within <paramref name="radius"/> of another.
        /// </summary>
        /// <remarks>
        /// Compares squared distances so there is no square root and no floating
        /// point. Coordinates are shorts, so the squared distance of the widest
        /// possible separation still fits comfortably in a long.
        /// </remarks>
        public bool IsWithinRadius(ServerPosition other, ushort radius)
        {
            short otherX,
                otherY,
                otherZ;
            lock (other._syncRoot)
            {
                otherX = other._x;
                otherY = other._y;
                otherZ = other._z;
            }

            lock (_syncRoot)
            {
                long dx = _x - otherX;
                long dy = _y - otherY;
                long dz = _z - otherZ;

                return (dx * dx) + (dy * dy) + (dz * dz) <= (long)radius * radius;
            }
        }

        public void WriteTo(ref PositionInterop p)
        {
            lock (_syncRoot)
            {
                p.X = _x;
                p.Y = _y;
                p.Z = _z;
            }
        }

        public void WriteTo(ref PositionRotationInterop p)
        {
            lock (_syncRoot)
            {
                p.Pos.X = _x;
                p.Pos.Y = _y;
                p.Pos.Z = _z;
                p.Rot = _rotation;
            }
        }

        public override string ToString()
        {
            lock (_syncRoot)
            {
                return $"X: {_x}, Y: {_y}, Z: {_z}, Rotation: {_rotation}";
            }
        }
    }
}
