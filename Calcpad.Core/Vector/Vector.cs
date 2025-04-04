﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Calcpad.Core
{
    internal class Vector : IValue, IEquatable<Vector>
    {
        internal enum Relation
        {
            Equal,
            NotEqual,
            LessThan,
            LessOrEqual,
            GreaterThan,
            GreaterOrEqual
        }

        internal const long MaxLength = 10000000;
        protected RealValue[] _values;
        protected int _size;

        internal virtual RealValue this[int index]
        {
            get => _values[index];
            set => _values[index] = value;
        }

        internal virtual ref RealValue ValueByRef(int index) => ref _values[index];
        internal virtual RealValue[] Values => _values;
        internal RealValue[] RawValues => _values;
        internal virtual int Length => _values.Length;
        internal int Size => _size;

        protected Vector() { }

        internal Vector(RealValue[] values)
        {
            if (values.Length > MaxLength)
                Throw.VectorSizeLimitException();

            _size = values.Length;
            _values = values;
        }

        internal Vector(int size)
        {
            if (size > MaxLength)
                Throw.VectorSizeLimitException();

            _size = size;
            _values = new RealValue[size];
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            for (int i = 0, n = Length; i < n; ++i)
                hashCode.Add(this[i]);

            return hashCode.ToHashCode();
        }

        public override bool Equals(object obj) => obj is Vector v && Equals(v);

        public bool Equals(Vector other) =>
            _values.AsSpan().SequenceEqual(other._values);

        private static Vector Clone(Vector a) =>
            a is LargeVector ?
            new LargeVector(a.Length, a.Size) :
            new Vector(a.Length);

        private static Vector Clone(Vector a, Vector b)
        {
            if (a.Length > b.Length)
                return Clone(a);

            return Clone(b);
        }

        internal Vector Copy()
        {
            var v = Clone(this);
            if (_values is not null)
                _values.AsSpan().CopyTo(v._values);
            else
                for (int i = _size - 1; i >= 0; --i)
                    v[i] = this[i];

            return v;
        }

        //Iterations are reversed to avoid multiple resizing 
        public static Vector operator -(Vector a)
        {
            var b = Clone(a);
            for (int i = a._size - 1; i >= 0; --i)
                b[i] = -a[i];

            return b;
        }

        public static Vector operator -(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            (var n1, var n2) = MinMax(na, nb);
            if (na > nb)
                for (int i = n2 - 1; i >= n1; --i)
                    c[i] = a[i];
            else if (nb > na)
                for (int i = n2 - 1; i >= n1; --i)
                    c[i] = -b[i];

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = a[i] - b[i];

            return c;
        }

        public static Vector operator -(Vector a, RealValue b)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            if (nc > na && !b.Equals(RealValue.Zero))
                c.Fill(-b, na, nc - na);

            for (int i = na - 1; i >= 0; --i)
                c[i] = a[i] - b;

            return c;
        }

        public static Vector operator -(RealValue a, Vector b)
        {
            var c = Clone(b);
            var nb = b._size;
            var nc = c.Length;
            if (nc > nb && !a.Equals(RealValue.Zero))
                c.Fill(a, nb, nc - nb);

            for (int i = nb - 1; i >= 0; --i)
                c[i] = a - b[i];

            return c;
        }

        public static Vector operator +(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            (var n1, var n2) = MinMax(na, nb);
            if (na > nb)
                for (int i = n2 - 1; i >= 0; --i)
                    c[i] = a[i];
            else if (b._size > a._size)
                for (int i = n2 - 1; i >= 0; --i)
                    c[i] = b[i];

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = a[i] + b[i];

            return c;
        }

        public static Vector operator +(Vector a, RealValue b)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            if (nc > na && !b.Equals(RealValue.Zero))
                c.Fill(b, na, nc - na);

            for (int i = na - 1; i >= 0; --i)
                c[i] = a[i] + b;

            return c;
        }

        public static Vector operator *(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            (var n1, var n2) = MinMax(na, nb);
            if (na > nb)
                for (int i = n2 - 1; i >= n1; --i)
                    c[i] = new(0, a[i].Units);
            else if (nb > na)
                for (int i = n2 - 1; i >= n1; --i)
                    c[i] = new(0, b[i].Units);

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = a[i] * b[i];

            return c;
        }

        public static Vector operator *(Vector a, RealValue b)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            if (nc > na && b.Units is not null)
                c.Fill(new(0, b.Units), na, nc - na);

            for (int i = na - 1; i >= 0; --i)
                c[i] = a[i] * b;

            return c;
        }

        public static Vector operator /(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            var nc = c.Length;
            (var n1, var n2) = MinMax(na, nb);
            c.Fill(RealValue.NaN, n2, nc - n2);
            if (na > nb)
                for (int i = n2 - 1; i >= n1; --i)
                    c[i] = a[i] / RealValue.Zero;
            else if (nb > na)
                for (int i = n2 - 1; i >= n1; --i)
                    c[i] = RealValue.Zero / b[i];

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = a[i] / b[i];

            return c;
        }

        public static Vector operator /(Vector a, RealValue b)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            if (nc > na)
            {
                var v = RealValue.Zero / b;
                if (!v.Equals(RealValue.Zero))
                    c.Fill(v, na, nc - na);
            }
            for (int i = na - 1; i >= 0; --i)
                c[i] = a[i] / b;

            return c;
        }

        public static Vector operator /(RealValue a, Vector b)
        {
            var c = Clone(b);
            var nb = b._size;
            var nc = c.Length;
            if (nc > nb)
                c.Fill(a / RealValue.Zero, nb, nc - nb);

            for (int i = nb - 1; i >= 0; --i)
                c[i] = a / b[i];

            return c;
        }

        public static Vector operator %(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            var nc = c.Length;
            (var n1, var n2) = MinMax(na, nb);
            c.Fill(RealValue.NaN, n2, nc - n2);
            if (na > nb)
                c.Fill(RealValue.NaN, n1, n2 - n1);
            else if (nb > na)
                for (int i = n2 - 1; i >= n1; --i)
                    c[i] = RealValue.Zero % b[i];

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = a[i] % b[i];

            return c;
        }

        public static Vector operator %(Vector a, RealValue b)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            if (nc > na && !b.Equals(RealValue.Zero))
                c.Fill(RealValue.NaN, na, nc - na);

            for (int i = na - 1; i >= 0; --i)
                c[i] = a[i] % b;

            return c;
        }

        public static Vector operator %(RealValue a, Vector b)
        {
            var c = Clone(b);
            var nb = b._size;
            var nc = c.Length;
            if (nc > nb)
                c.Fill(RealValue.NaN, nb, nc - nb);

            for (int i = nb - 1; i >= 0; --i)
                c[i] = a % b[i];

            return c;
        }

        public static Vector operator ==(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            var nc = c.Length;
            (var n1, var n2) = MinMax(na, nb);
            c.Fill(RealValue.One, n2, nc - n2);
            var zero = RealValue.Zero;
            if (na > nb)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    var u = a[i].Units;
                    if (u != zero.Units)
                        zero = new(0d, u);

                    c[i] = a[i] == zero;
                }
            else if (nb > na)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    var u = b[i].Units;
                    if (u != zero.Units)
                        zero = new(0d, u);

                    c[i] = zero == b[i];
                }

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = a[i] == b[i];

            return c;
        }

        public static Vector operator ==(Vector a, RealValue b)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            if (nc > na)
            {
                var zero = b.Units is null ? RealValue.Zero : new(0, b.Units);
                var v = zero == b;
                if (!v.Equals(RealValue.Zero))
                    c.Fill(v, na, nc - na);
            }
            for (int i = na - 1; i >= 0; --i)
                c[i] = a[i] == b;

            return c;
        }

        public static Vector operator !=(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            (var n1, var n2) = MinMax(na, nb);
            var zero = RealValue.Zero;
            if (na > nb)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    var u = a[i].Units;
                    if (u != zero.Units)
                        zero = new(0d, u);

                    c[i] = a[i] != zero;
                }
            else if (nb > na)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    var u = b[i].Units;
                    if (u != zero.Units)
                        zero = new(0d, u);

                    c[i] = zero != b[i];
                }

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = a[i] != b[i];

            return c;
        }

        public static Vector operator !=(Vector a, RealValue b)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            if (nc > na)
            {
                var zero = b.Units is null ? RealValue.Zero : new(0, b.Units);
                var v = b != zero;
                if (!v.Equals(RealValue.Zero))
                    c.Fill(v, na, nc - na);
            }
            for (int i = na - 1; i >= 0; --i)
                c[i] = a[i] != b;

            return c;
        }

        public static Vector operator <(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            (var n1, var n2) = MinMax(na, nb);
            var zero = RealValue.Zero;
            if (na > nb)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    var u = a[i].Units;
                    if (u != zero.Units)
                        zero = new(0d, u);

                    c[i] = a[i] < zero;
                }
            else if (nb > na)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    var u = b[i].Units;
                    if (u != zero.Units)
                        zero = new(0d, u);

                    c[i] = zero < b[i];
                }

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = a[i] < b[i];

            return c;
        }

        public static Vector operator <(Vector a, RealValue b)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            if (nc > na)
            {
                var zero = b.Units is null ? RealValue.Zero : new(0, b.Units);
                var v = zero < b;
                if (!v.Equals(RealValue.Zero))
                    c.Fill(v, na, nc - na);
            }
            for (int i = na - 1; i >= 0; --i)
                c[i] = a[i] < b;

            return c;
        }

        public static Vector operator <(RealValue a, Vector b)
        {
            var c = Clone(b);
            var nb = b._size;
            var nc = c.Length;
            if (nc > nb)
            {
                var zero = a.Units is null ? RealValue.Zero : new(0, a.Units);
                var v = a < zero;
                if (!v.Equals(RealValue.Zero))
                    c.Fill(v, nb, nc - nb);
            }
            for (int i = nb - 1; i >= 0; --i)
                c[i] = a < b[i];

            return c;
        }

        public static Vector operator >(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            (var n1, var n2) = MinMax(na, nb);
            var zero = RealValue.Zero;
            if (na > nb)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    var u = a[i].Units;
                    if (u != zero.Units)
                        zero = new(0d, u);

                    c[i] = a[i] > zero;
                }
            else if (nb > na)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    var u = b[i].Units;
                    if (u != zero.Units)
                        zero = new(0d, u);

                    c[i] = zero > b[i];
                }

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = a[i] > b[i];

            return c;
        }

        public static Vector operator >(Vector a, RealValue b)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            if (nc > na)
            {
                var zero = b.Units is null ? RealValue.Zero : new(0, b.Units);
                var v = zero > b;
                if (!v.Equals(RealValue.Zero))
                    c.Fill(v, na, nc - na);
            }
            for (int i = na - 1; i >= 0; --i)
                c[i] = a[i] > b;

            return c;
        }

        public static Vector operator >(RealValue a, Vector b)
        {
            var c = Clone(b);
            var nb = b._size;
            var nc = c.Length;
            if (nc > nb)
            {
                var zero = a.Units is null ? RealValue.Zero : new(0, a.Units);
                var v = a > zero;
                if (!v.Equals(RealValue.Zero))
                    c.Fill(v, nb, nc - nb);
            }
            for (int i = nb - 1; i >= 0; --i)
                c[i] = a > b[i];

            return c;
        }

        public static Vector operator <=(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            var nc = c.Length;
            (var n1, var n2) = MinMax(na, nb);
            c.Fill(RealValue.One, n2, nc - n2);
            var zero = RealValue.Zero;
            if (na > nb)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    var u = a[i].Units;
                    if (u != zero.Units)
                        zero = new(0d, u);

                    c[i] = a[i] <= zero;
                }
            else if (nb > na)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    var u = b[i].Units;
                    if (u != zero.Units)
                        zero = new(0d, u);

                    c[i] = zero <= b[i];
                }

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = a[i] <= b[i];

            return c;
        }

        public static Vector operator <=(Vector a, RealValue b)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            if (nc > na)
            {
                var zero = b.Units is null ? RealValue.Zero : new(0, b.Units);
                var v = zero <= b;
                if (!v.Equals(RealValue.Zero))
                    c.Fill(v, na, nc - na);
            }
            for (int i = na - 1; i >= 0; --i)
                c[i] = a[i] <= b;

            return c;
        }

        public static Vector operator <=(RealValue a, Vector b)
        {
            var c = Clone(b);
            var nb = b._size;
            var nc = c.Length;
            if (nc > nb)
            {
                var zero = a.Units is null ? RealValue.Zero : new(0, a.Units);
                var v = a <= zero;
                if (!v.Equals(RealValue.Zero))
                    c.Fill(v, nb, nc - nb);
            }
            for (int i = nb - 1; i >= 0; --i)
                c[i] = a <= b[i];

            return c;
        }

        public static Vector operator >=(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            var nc = c.Length;
            (var n1, var n2) = MinMax(na, nb);
            c.Fill(RealValue.One, n2, nc - n2);
            var zero = RealValue.Zero;
            if (na > nb)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    var u = a[i].Units;
                    if (u != zero.Units)
                        zero = new(0d, u);

                    c[i] = a[i] >= zero;
                }
            else if (nb > na)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    var u = b[i].Units;
                    if (u != zero.Units)
                        zero = new(0d, u);

                    c[i] = zero >= b[i];
                }

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = a[i] >= b[i];

            return c;
        }

        public static Vector operator >=(Vector a, RealValue b)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            if (nc > na)
            {
                var zero = b.Units is null ? RealValue.Zero : new(0, b.Units);
                var v = zero >= b;
                if (!v.Equals(RealValue.Zero))
                    c.Fill(v, na, nc - na);
            }
            for (int i = na - 1; i >= 0; --i)
                c[i] = a[i] >= b;

            return c;
        }

        public static Vector operator >=(RealValue a, Vector b)
        {
            var c = Clone(b);
            var nb = b._size;
            var nc = c.Length;
            if (nc > nb)
            {
                var zero = a.Units is null ? RealValue.Zero : new(0, a.Units);
                var v = a >= zero;
                if (!v.Equals(RealValue.Zero))
                    c.Fill(v, nb, nc - nb);
            }
            for (int i = nb - 1; i >= 0; --i)
                c[i] = a >= b[i];

            return c;
        }

        public static Vector operator &(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var n = a._size < b._size ? a._size : b._size;
            for (int i = n - 1; i >= 0; --i)
                c[i] = a[i] & b[i];

            return c;
        }

        public static Vector operator &(Vector a, RealValue b)
        {
            var c = Clone(a);
            for (int i = a._size - 1; i >= 0; --i)
                c[i] = a[i] & b;

            return c;
        }

        public static Vector operator |(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            (var n1, var n2) = MinMax(na, nb);
            if (na > nb)
                for (int i = n2 - 1; i >= n1; --i)
                    c[i] = a[i] | RealValue.Zero;
            else if (nb > na)
                for (int i = n2 - 1; i >= n1; --i)
                    c[i] = RealValue.Zero | b[i];

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = a[i] | b[i];

            return c;
        }

        public static Vector operator |(Vector a, RealValue b)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            var v = RealValue.Zero | b;
            if (nc > na && !v.Equals(RealValue.Zero))
                c.Fill(v, na, nc - na);

            for (int i = a._size - 1; i >= 0; --i)
                c[i] = a[i] | b;

            return c;
        }

        public static Vector operator ^(Vector a, Vector b)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            (var n1, var n2) = MinMax(na, nb);

            if (na > nb)
                for (int i = n2 - 1; i >= n1; --i)
                    c[i] = a[i] ^ RealValue.Zero;
            else if (nb > na)
                for (int i = n2 - 1; i >= n1; --i)
                    c[i] = RealValue.Zero ^ b[i];

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = a[i] ^ b[i];

            return c;
        }

        public static Vector operator ^(Vector a, RealValue b)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            var v = RealValue.Zero ^ b;
            if (nc > na && !v.Equals(RealValue.Zero))
                c.Fill(v, na, nc - na);

            for (int i = a._size - 1; i >= 0; --i)
                c[i] = a[i] ^ b;

            return c;
        }

        internal static Vector EvaluateOperator(RealCalculator.Operator<RealValue> op, Vector a, Vector b, bool requireConsistentUnits)
        {
            var c = Clone(a, b);
            var na = a._size;
            var nb = b._size;
            var nc = c.Length;
            (var n1, var n2) = MinMax(na, nb);
            var zero = RealValue.Zero;
            if (nc > n2)
            {
                var v = op(zero, zero);
                if (!v.Equals(zero))
                    c.Fill(v, n2, nc - n2);
            }
            if (na > nb)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    if (requireConsistentUnits)
                    {
                        var u = a[i].Units;
                        if (u != zero.Units)
                            zero = new(0d, u);
                    }
                    c[i] = op(a[i], zero);
                }
            else if (nb > na)
                for (int i = n2 - 1; i >= n1; --i)
                {
                    if (requireConsistentUnits)
                    {
                        var u = b[i].Units;
                        if (u != zero.Units)
                            zero = new(0d, u);
                    }
                    c[i] = op(zero, b[i]);
                }

            for (int i = n1 - 1; i >= 0; --i)
                c[i] = op(a[i], b[i]);

            return c;
        }

        internal static Vector EvaluateOperator(RealCalculator.Operator<RealValue> op, Vector a, in RealValue b, bool requireConsistentUnits)
        {
            var c = Clone(a);
            var na = a._size;
            var nc = c.Length;
            if (na < nc)
            {
                var zero =  requireConsistentUnits && b.Units is not null ? 
                    new(0, b.Units) : 
                    RealValue.Zero;
                var v = op(zero, b);
                if (!v.Equals(RealValue.Zero))
                    c.Fill(v, na, nc - na);
            }
            for (int i = na - 1; i >= 0; --i)
                c[i] = op(a[i], b);

            return c;
        }

        internal static Vector EvaluateOperator(RealCalculator.Operator<RealValue> op, in RealValue a, Vector b, bool requireConsistentUnits)
        {
            var c = Clone(b);
            var nb = b._size;
            var nc = c.Length;
            if (nb < nc)
            {
                var zero = requireConsistentUnits && a.Units is not null ?
                    new(0, a.Units) :
                    RealValue.Zero;
                var v = op(a, zero);
                if (!v.Equals(RealValue.Zero))
                    c.Fill(v, nb, nc - nb);
            }
            for (int i = nb - 1; i >= 0; --i)
                c[i] = op(a, b[i]);

            return c;
        }

        internal static Vector EvaluateFunction(RealCalculator.Function<RealValue> f, Vector a)
        {
            var b = Clone(a);
            var na = a._size;
            var nb = b.Length;
            if (nb > na)
            {
                var v = f(RealValue.Zero);
                if (!v.Equals(RealValue.Zero))
                    b.Fill(v, na, nb - na);
            }
            for (int i = na - 1; i >= 0; --i)
                b[i] = f(a[i]);

            return b;
        }

        internal void SetUnits(Unit units)
        {
            for (int i = _size - 1; i >= 0; --i)
                this[i] = new RealValue(this[i].D, units);
        }

        private static (int, int) MinMax(int n1, int n2)
        {
            if (n1 <= n2)
                return (n1, n2);

            return (n2, n1);
        }

        internal static Vector CrossProduct(Vector a, Vector b)
        {
            var na = a.Length;
            var nb = b.Length;
            if (na < 2 || na > 3 || nb < 2 || nb > 3)
                Throw.CrossProductVectorDimensionsException();

            Vector c = new(3);
            c[2] = a[0] * b[1] - a[1] * b[0];
            if (na == 3)
            {
                c[0] = -a[2] * b[1];
                c[1] = a[2] * b[0];
            }
            if (nb == 3)
            {
                c[0] += a[1] * b[2];
                c[1] -= a[0] * b[2];
            }
            return c;
        }

        internal static RealValue DotProduct(Vector a, Vector b)
        {
            var n = Math.Min(a._size, b._size);
            if (n == 0)
                return RealValue.Zero;

            var va = a._values;
            var vb = b._values;
            var sum = a[0] * b[0];
            if (va is null)
            {
                if (vb is null)
                    for (int i = 1; i < n; ++i)
                        sum += a[i] * b[i];
                else
                    for (int i = 1; i < n; ++i)
                        sum += a[i] * vb[i];
            }
            else
            {
                if (vb is null)
                    for (int i = 1; i < n; ++i)
                        sum += va[i] * b[i];
                else
                    for (int i = 1; i < n; ++i)
                        sum += va[i] * vb[i]; 
            }
            return sum;
        }

        internal static readonly Comparison<RealValue> ascending = new((x, y) => x.CompareTo(y));
        internal static readonly Comparison<RealValue> descending = new((x, y) => -x.CompareTo(y));
        
        internal Vector Sort(bool reverse = false)
        {
            var n = Length;
            Vector vector = new(n);
            var span = vector._values.AsSpan();
            _values.AsSpan(0, _size).CopyTo(span);
            if (reverse)
                span.Sort(descending);
            else
                span.Sort();
            return vector;
        }

        internal virtual Vector Reverse()
        {
            var n = Length;
            Vector vector = new(n);
            int n1 = n - 1;
            for (int i = 0; i < n; ++i)
                vector[i] = this[n1 - i];

            return vector;
        }

        internal Vector Order(bool reverse = false) => FromIndexes(GetOrderIndexes(reverse));

        internal int[] GetOrderIndexes(bool reverse)
        {
            var n = Length;
            var values = new RealValue[n];
            var span = values.AsSpan();
            if (_values is not null)
                _values.AsSpan(0, _size).CopyTo(span);
            else
                for (int i = 0; i < _size; ++i)
                    span[i] = this[i];

            var indexes = Enumerable.Range(0, n).ToArray();
            if (reverse)
                span.Sort<RealValue, int>(indexes, descending);
            else
                span.Sort<RealValue, int>(indexes);

            return indexes;
        }

        internal Vector First(int length)
        {
            if (length < 1)
                length = 1;
            else if (length >= Length)
                return Copy();

            Vector vector = new(length);
            if (length > _size)
                length = _size;

            _values.AsSpan(0, length).CopyTo(vector._values);
            return vector;
        }

        internal Vector Last(int length)
        {
            if (length < 1)
                length = 1;
            else if (length >= Length)
                return Copy();

            Vector vector = new(length);
            var start = Length - length;
            if (start < _size)
                _values.AsSpan(start, _size - start).CopyTo(vector._values);

            return vector;
        }

        internal Vector Slice(int start, int end)
        {
            if (start > end)
                (start, end) = (end, start);

            if (start > Length) start = Length;
            if (end > Length) end = Length;

            if (start == 1 && end == Length)
                return Copy();

            start--;
            Vector vector = new(end - start);
            if (end > _size)
                end = _size;
            if (start < end)
                _values.AsSpan(start, end - start).CopyTo(vector._values);

            return vector;
        }

        internal virtual Vector Fill(RealValue value)
        {
            _values.AsSpan().Fill(value);
            return this;
        }

        protected virtual void Fill(RealValue value, int start, int len) =>
            _values.AsSpan(start, len).Fill(value);

        internal static Vector Join(IValue[] items)
        {
            var n = items.Length;
            var len = 0;
            for (int i = 0; i < n; ++i)
            {
                if (items[i] is Vector vector)
                    len += vector.Length;
                else if (items[i] is RealValue)
                    len += 1;
                else if (items[i] is Matrix matrix)
                    len += matrix.ColCount * matrix.RowCount;
            }
            if (len > MaxLength)
                Throw.VectorSizeLimitException();

            var values = new RealValue[len];
            var index = 0;
            for (int k = 0; k < n; ++k)
            {
                if (items[k] is Vector vector)
                {
                    vector._values.AsSpan(0, vector._size).CopyTo(values.AsSpan(index, vector._size));
                    index += vector.Length;
                }
                else if (items[k] is RealValue real)
                {
                    values[index] = real;
                    ++index;
                }
                else if (items[k] is Matrix matrix)
                {
                    var N = matrix.ColCount;
                    for (int i = 0; i < matrix.RowCount; ++i)
                        for (int j = 0; j < N; ++j)
                            values[index++] = matrix[i, j];
                }
            }
            return new Vector(values);
        }

        internal static Vector Range(RealValue start, RealValue end, RealValue step)
        {
            if (step.D.AlmostEquals(0))
                Throw.StepCannotBeZeroException();

            var len = ((end - start) / step).D + 1;
            if (len <= 1)
                return new Vector([start, end]);

            if (len > MaxLength)
                Throw.VectorSizeLimitException();

            len = Math.Truncate(len);
            RealValue[] values = new RealValue[(int)len];
            for (int i = 0; i < len; ++i)
                values[i] = start + step * i;

            return new Vector(values);
        }

        internal RealValue Search(RealValue value, int start)
        {
            var n = Length;
            if (start < 1)
                start = 1;
            else if (start > _size)
            {
                if (start > n)
                    return RealValue.Zero;

                if (value.AlmostEquals(RealValue.Zero))
                    return new(start);

                return RealValue.Zero;
            }
            for (int i = start - 1; i < _size; ++i)
            {
                if (value.AlmostEquals(this[i]))
                    return new RealValue(i + 1);
            }
            if (_size < n && value.AlmostEquals(RealValue.Zero))
                return new RealValue(_size + 1);

            return RealValue.Zero;
        }

        internal Vector FindAll(RealValue value, int start, Relation rel) =>
            FromIndexes(FindAllIndexes(value, start, rel));

        internal Vector Lookup(Vector dest, RealValue value, Relation rel)
        {
            var indexes = FindAllIndexes(value, 1, rel);
            var vector = new Vector(indexes.Count());
            var j = 0;
            foreach (var í in indexes)
            {
                if (í > dest.Length)
                    Throw.IndexOutOfRangeException((í + 1).ToString());

                vector[j] = dest[í];
                ++j;
            }
            return vector;
        }

        internal static bool Relate(RealValue a, RealValue b, Relation rel) =>
            rel switch
            {
                Relation.Equal => a.AlmostEquals(b),
                Relation.NotEqual => !a.AlmostEquals(b),
                Relation.LessThan => a.CompareTo(b) < 0 && !a.AlmostEquals(b),
                Relation.LessOrEqual => a.CompareTo(b) <= 0 || a.AlmostEquals(b),
                Relation.GreaterThan => a.CompareTo(b) > 0 && !a.AlmostEquals(b),
                Relation.GreaterOrEqual => a.CompareTo(b) >= 0 || a.AlmostEquals(b),
                _ => false
            };

        private IEnumerable<int> FindAllIndexes(RealValue value, int start, Relation rel)
        {
            var n = Length;
            if (start < 1)
                start = 1;
            else if (start > _size)
            {
                if (start > n)
                    return [];

                if (Relate(value, RealValue.Zero, rel))
                    return Enumerable.Range(_size, n - _size);

                return [];
            }
            var indexes = new List<int>();
            for (int i = start - 1; i < _size; ++i)
            {
                if (Relate(this[i], value, rel))
                    indexes.Add(i);
            }
            if (_size < n && Relate(value, RealValue.Zero, rel))
                indexes.AddRange(Enumerable.Range(_size, n - _size));

            return indexes;
        }

        internal Vector Extract(Vector indexes)
        {
            var n = Length;
            var ni = indexes.Length;
            var vector = new Vector(ni);
            for (int i = 0; i < ni; ++i)
            {
                var d = indexes[i].D;
                if (d < 1 || d > int.MaxValue)
                    Throw.MustBePositiveIntegerException(Throw.Items.Index);
                int j = (int)d;
                if (j > n)
                    Throw.IndexOutOfRangeException(j.ToString());

                vector[i] = this[j - 1];
            }
            return vector;
        }

        internal RealValue Count(RealValue value, int start)
        {
            var count = 0;
            for (int i = start - 1; i < _size; ++i)
                if (value.AlmostEquals(this[i]))
                    ++count;

            if (value.Equals(RealValue.Zero))
                count += Length - _size;

            return new(count);
        }

        internal virtual Vector Resize(int newSize)
        {
            if (newSize > MaxLength)
                Throw.VectorSizeLimitException();

            if (newSize != _size)
            {
                _size = newSize;
                Array.Resize(ref _values, newSize);
            }
            return this;
        }

        //L1 or Manhattan norm  
        internal RealValue L1Norm()
        {
            if (_size == 0)
                return RealValue.Zero;

            var v = this[0];
            Unit u = v.Units;
            var norm = Math.Abs(v.D);
            for (int i = 1; i < _size; ++i)
            {
                v = this[i];
                norm += Math.Abs(v.D) * Unit.Convert(u, v.Units, '+');
            }
            return new(norm, u);
        }

        //L2 or Euclidean norm  
        internal RealValue Norm()
        {
            if (_size == 0)
                return RealValue.Zero;

            var v = this[0];
            var norm = v.D;
            norm *= norm;
            Unit u = v.Units;
            for (int i = 1; i < _size; ++i)
            {
                v = this[i];
                var d = v.D * Unit.Convert(u, v.Units, ',');
                norm += d * d;
            }
            return new(Math.Sqrt(norm), u);
        }

        //Lp norm   
        internal RealValue LpNorm(int p)
        {
            if (p < 1)
                Throw.InvalidLpNormArgumentException();

            if (_size == 0)
                return RealValue.Zero;

            var v = this[0];
            var u = v.Units;
            var norm = Math.Pow(Math.Abs(v.D), p);
            for (int i = 1; i < _size; ++i)
            {
                v = this[i];
                var d = Unit.Convert(u, v.Units, ',');
                norm += Math.Pow(Math.Abs(v.D) * d, p);
            }
            return new(Math.Pow(norm, 1d / p));
        }

        //L∞ (Infinity) or Chebyshev norm  
        internal RealValue InfNorm()
        {
            if (_size == 0)
                return RealValue.Zero;

            var v = this[0];
            var u = v.Units;
            var norm = Math.Abs(v.D);
            for (int i = 1; i < _size; ++i)
            {
                v = this[i];
                var c = Math.Abs(v.D) * Unit.Convert(u, v.Units, ',');
                if (c > norm)
                    norm = c;
            }
            return new(norm);
        }

        internal Vector Normalize() => this / Norm();

        internal static Vector FromIndexes(IEnumerable<int> indexes)
        {
            var n = indexes.Count();
            var vector = new Vector(n);
            var i = 0;
            foreach (var index in indexes)
            {
                vector[i] = new(index + 1);
                ++i;
            }
            return vector;
        }

        internal RealValue Min()
        {
            var v = this[0];
            var min = v.D;
            var u = v.Units;
            for (int i = 1; i < _size; ++i)
            {
                v = this[i];
                var b = v.D * Unit.Convert(u, v.Units, ',');
                if (b < min)
                    min = b;
            }
            if (Length > _size && min > 0)
                min = 0;

            return new(min, u);
        }

        internal RealValue Max()
        {
            var v = this[0];
            var max = v.D;
            var u = v.Units;
            for (int i = 1; i < _size; ++i)
            {
                v = this[i];
                var b = v.D * Unit.Convert(u, v.Units, ',');
                if (b > max)
                    max = b;
            }
            if (Length > _size && max < 0)
                max = 0;

            return new(max, u);
        }

        internal RealValue Sum()
        {
            var v = this[0];
            var sum = v.D;
            var u = v.Units;
            for (int i = 1; i < _size; ++i)
            {
                v = this[i];
                sum += v.D * Unit.Convert(u, v.Units, ',');
            }
            return new(sum, u);
        }

        internal RealValue SumSq()
        {
            var v = this[0];
            var sumsq = v.D;
            var u = v.Units;
            sumsq *= sumsq;
            for (int i = 1; i < _size; ++i)
            {
                v = this[i];
                var b = v.D * Unit.Convert(u, v.Units, ',');
                sumsq += b * b;
            }
            return new(sumsq, u?.Pow(2f));
        }

        internal RealValue Srss()
        {
            var v = this[0];
            var srss = v.D;
            var u = v.Units;
            srss *= srss;
            for (int i = 1; i < _size; ++i)
            {
                v = this[i];
                var b = v.D * Unit.Convert(u, v.Units, ',');
                srss += b * b;
            }
            return new(Math.Sqrt(srss), u);
        }

        internal RealValue Average()
        {
            var v = this[0];
            var average = v.D;
            var u = v.Units;
            for (int i = 1; i < _size; ++i)
            {
                v = this[i];
                average += v.D * Unit.Convert(u, v.Units, ',');
            }
            return new(average / Length, u);
        }

        internal RealValue Product()
        {
            var v = this[0];
            var product = v.D;
            var u = v.Units;
            for (int i = 1; i < _size; ++i)
            {
                v = this[i];
                u = Unit.Multiply(u, v.Units, out var b);
                product *= v.D * b;
            }
            if (Length > _size)
                return new(0d, u);

            return new(product, u);
        }

        internal RealValue Mean()
        {
            var product = Product();
            var result = Math.Pow(product.D, 1d / Length);
            var u = product.Units;
            if (u is null)
                return new(result);

            u = Unit.Root(u, Length);
            return new(result, u);
        }

        internal RealValue And()
        {
            if (Length > _size)
                return RealValue.Zero;

            for (int i = 0; i < _size; ++i)
                if (Math.Abs(this[i].D) < RealValue.LogicalZero)
                    return RealValue.Zero;

            return RealValue.One;
        }

        internal RealValue Or()
        {
            for (int i = 0; i < _size; ++i)
                if (Math.Abs(this[i].D) >= RealValue.LogicalZero)
                    return RealValue.One;

            return RealValue.Zero;
        }

        internal RealValue Xor()
        {
            var b = Math.Abs(this[0].D) >= RealValue.LogicalZero;
            for (int i = 1; i < _size; ++i)
                b = b != Math.Abs(this[i].D) >= RealValue.LogicalZero;

            return b ? RealValue.One : RealValue.Zero;
        }

        internal RealValue Gcd()
        {
            var v = this[0];
            var a = Calculator.AsLong(v.D);
            var u = v.Units;
            for (int i = 1; i < _size; ++i)
            {
                v = this[i];
                var b = Calculator.AsLong(v.D * Unit.Convert(u, v.Units, ','));
                a = Calculator.Gcd(a, b);
            }
            return new(a);
        }

        internal RealValue Lcm()
        {
            var v = this[0];
            var a = Calculator.AsLong(v.D);
            var u = v.Units;
            for (int i = 1; i < _size; ++i)
            {
                v = this[i];
                var b = Calculator.AsLong(v.D * Unit.Convert(u, v.Units, ','));
                a = a * b / Calculator.Gcd(a, b);
            }
            return new(a);
        }

        internal RealValue Take(in RealValue x)
        {
            var d = Math.Round(x.D, MidpointRounding.AwayFromZero);
            if (!double.IsNormal(d) || d < Calculator.DeltaMinus || d > Length * Calculator.DeltaPlus)
                return RealValue.NaN;

            return this[(int)d - 1];
        }

        internal RealValue Line(in RealValue x)
        {
            var d = x.D;
            if (!double.IsNormal(d) || d < Calculator.DeltaMinus || d > Length * Calculator.DeltaPlus)
                return RealValue.NaN;

            var i = (int)Math.Floor(d);
            var v1 = this[i - 1];
            if (i == d || d >= Length)
                return v1;

            return v1 + (this[i] - v1) * (d - i);
        }

        internal RealValue Spline(in RealValue x)
        {
            var d = x.D;
            if (!double.IsNormal(d) || d < Calculator.DeltaMinus || d > Length * Calculator.DeltaPlus)
                return RealValue.NaN;

            var i = (int)Math.Floor(d) - 1;
            var v = this[i];
            if (i == d || d >= Length)
                return v;

            var u = v.Units;
            var y0 = v.D;
            v = this[i + 1];
            var y1 = v.D * Unit.Convert(u, v.Units, ',');
            var dy = y1 - y0;
            var a = dy;
            var b = dy;
            dy = Math.Sign(dy);
            if (i > 0)
            {
                v = this[i - 1];
                var y2 = v.D * Unit.Convert(u, v.Units, ',');
                a = (y1 - y2) * (Math.Sign(y0 - y2) == dy ? 0.5 : 0.25);
            }
            if (i < Length - 2)
            {
                v = this[i + 2];
                var y2 = v.D * Unit.Convert(u, v.Units, ',');
                b = (y2 - y0) * (Math.Sign(y2 - y1) == dy ? 0.5 : 0.25);
            }
            if (i == 0)
                a += (a - b) / 2;

            if (i == Length - 2)
                b += (b - a) / 2;

            var t = d - i - 1d;
            d = y0 + ((y1 - y0) * (3 - 2 * t) * t + ((a + b) * t - a) * (t - 1)) * t;
            return new(d, u);
        }
    }
}
