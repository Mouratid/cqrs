using System;

namespace Mediator
{
    /// <summary>
    /// Represents a void type for requests that don't return a value.
    /// </summary>
    public readonly struct Unit : IEquatable<Unit>
    {
        public static Unit Value { get; }

        public override bool Equals(object obj)
        {
            return obj is Unit;
        }

        public bool Equals(Unit other)
        {
            return true;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public static bool operator ==(Unit left, Unit right)
        {
            return true;
        }

        public static bool operator !=(Unit left, Unit right)
        {
            return false;
        }
    }
}
