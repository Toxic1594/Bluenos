/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using System;

namespace OpenNos.PathFinder
{
    public class Node : GridPos, IComparable<Node>, IEquatable<Node>
    {
        #region Instantiation

        public Node(GridPos node)
        {
            Value = node.Value;
            X = node.X;
            Y = node.Y;
        }

        public Node()
        {
        }

        #endregion

        #region Properties

        public bool Closed { get; internal set; }

        public double F { get; internal set; }

        public double N { get; internal set; }

        public bool Opened { get; internal set; }

        public Node Parent { get; internal set; }

        #endregion

        #region Methods

        public int CompareTo(Node other) => F > other.F ? 1 : F < other.F ? -1 : 0;

        public bool Equals(Node other) => ReferenceEquals(this, other);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is null)
            {
                return false;
            }

            return false;
        }

        public override int GetHashCode() => GetHashCode();

        public static bool operator ==(Node left, Node right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(Node left, Node right) => !(left == right);

        public static bool operator <(Node left, Node right) => left is null ? !(right is null) : left.CompareTo(right) < 0;

        public static bool operator <=(Node left, Node right) => left is null || left.CompareTo(right) <= 0;

        public static bool operator >(Node left, Node right) => !(left is null) && left.CompareTo(right) > 0;

        public static bool operator >=(Node left, Node right) => left is null ? right is null : left.CompareTo(right) >= 0;

        #endregion
    }
}