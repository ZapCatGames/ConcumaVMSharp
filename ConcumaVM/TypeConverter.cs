namespace ConcumaVM
{
    public static class TypeConverter
    {
        public static object? Cast(object? a)
        {
            if (a is Symbol s) return s.Value;

            return a;
        }

        public static bool Compatible(object? a, object? b)
        {
            if (a is null && b is null) return true;
            if (a is null ^ b is null) return false;

            if (a!.GetType() == b!.GetType()) return true;

            if (a is int && b is double) return true;
            if (a is double && b is int) return true;

            return false;
        }

        public static bool Truthy(object? a)
        {
            a = Cast(a);

            if (a is null) return false;
            if (a is bool b) return b;
            if (Numeric(a)) return (double)a != 0;

            throw new RuntimeException("Attempting to get truthy value of non-truthable type.");
        }

        public new static bool Equals(object? a, object? b)
        {
            a = Cast(a);
            b = Cast(b);

            if (!Compatible(a, b)) return false;

            return a!.Equals(b);
        }

        public static bool Less(object? a, object? b)
        {
            a = Cast(a);
            b = Cast(b);

            if (!Numeric(a) && !Numeric(b)) throw new RuntimeException("Attempting to compare incompatible types.");

            return System.Convert.ToDouble(a!) < System.Convert.ToDouble(b!);
        }

        public static bool LessEqual(object? a, object? b)
        {
            a = Cast(a);
            b = Cast(b);

            if (!Numeric(a) && !Numeric(b)) throw new RuntimeException("Attempting to compare incompatible types.");

            return System.Convert.ToDouble(a) <= System.Convert.ToDouble(b);
        }

        public static bool Greater(object? a, object? b)
        {
            a = Cast(a);
            b = Cast(b);

            if (!Numeric(a) && !Numeric(b)) throw new RuntimeException("Attempting to compare incompatible types.");

            return System.Convert.ToDouble(a) > System.Convert.ToDouble(b);
        }

        public static bool GreaterEqual(object? a, object? b)
        {
            a = Cast(a);
            b = Cast(b);

            if (!Numeric(a) && !Numeric(b)) throw new RuntimeException("Attempting to compare incompatible types.");

            return System.Convert.ToDouble(a) >= System.Convert.ToDouble(b);
        }

        public static bool Numeric(object? item)
        {
            return item is int || item is double;
        }

        public static object Add(object? a, object? b)
        {
            a = Cast(a);
            b = Cast(b);

            if (!Compatible(a, b)) throw new RuntimeException("Attempting to add incompatible types.");

            if (a is int && b is int)
            {
                return AddInt(a, b);
            }
            else if (Numeric(a) && Numeric(b))
            {
                return AddDouble(a, b);
            }

            throw new RuntimeException("Attempting to add incompatible types.");
        }

        private static int AddInt(object? a, object? b)
        {
            return (int)a! + (int)b!;
        }

        private static double AddDouble(object? a, object? b)
        {
            return System.Convert.ToDouble(a!) + System.Convert.ToDouble(b!);
        }

        public static object Subtract(object? a, object? b)
        {
            a = Cast(a);
            b = Cast(b);

            if (!Compatible(a, b)) throw new RuntimeException("Attempting to add incompatible types.");

            if (a is int && b is int)
            {
                return SubtractInt(a, b);
            }
            else if (Numeric(a) && Numeric(b))
            {
                return SubtractDouble(a, b);
            }

            throw new RuntimeException("Attempting to add incompatible types.");
        }

        private static int SubtractInt(object? a, object? b)
        {
            return (int)a! - (int)b!;
        }

        private static double SubtractDouble(object? a, object? b)
        {
            return System.Convert.ToDouble(a!) - System.Convert.ToDouble(b!);
        }

        public static object Multiply(object? a, object? b)
        {
            a = Cast(a);
            b = Cast(b);

            if (!Compatible(a, b)) throw new RuntimeException("Attempting to add incompatible types.");

            if (a is int && b is int)
            {
                return MultiplyInt(a, b);
            }
            else if (Numeric(a) && Numeric(b))
            {
                return MultiplyDouble(a, b);
            }

            throw new RuntimeException("Attempting to add incompatible types.");
        }

        private static int MultiplyInt(object? a, object? b)
        {
            return (int)a! * (int)b!;
        }

        private static double MultiplyDouble(object? a, object? b)
        {
            return System.Convert.ToDouble(a!) * System.Convert.ToDouble(b!);
        }

        public static object Divide(object? a, object? b)
        {
            a = Cast(a);
            b = Cast(b);

            if (!Compatible(a, b)) throw new RuntimeException("Attempting to add incompatible types.");

            if (Numeric(a) && Numeric(b))
            {
                return DivideDouble(a, b);
            }

            throw new RuntimeException("Attempting to add incompatible types.");
        }

        private static double DivideDouble(object? a, object? b)
        {
            return System.Convert.ToDouble(a) / System.Convert.ToDouble(b);
        }
    }
}