namespace ConcumaVM
{
    public partial class VM
    {
        public static class Blackboard
        {
            private static readonly Dictionary<string, Delegate> _concumaValues = new();

            public static void Clear()
            {
                _concumaValues.Clear();
            }

            public static void Add(string name, Delegate value)
            {
                _concumaValues.Add(name, value);
            }

            internal static Delegate? Get(string name)
            {
                if (_concumaValues.TryGetValue(name, out Delegate? v)) return v;
                return null;
            }
        }
    }
}
