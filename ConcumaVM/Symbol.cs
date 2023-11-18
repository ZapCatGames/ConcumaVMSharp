namespace ConcumaVM
{
    public abstract class Symbol
    {
        public object? Value { get; set; }

        public sealed class Var : Symbol
        {
            public Var(bool @const, object? value)
            {
                Const = @const;
                Value = value;
            }

            public bool Const { get; }

            public override string ToString()
            {
                return Value?.ToString() ?? "null";
            }
        }

        public sealed class Function : Symbol
        {
            public Function(int symbol, int[] parameters, int action, ConcumaEnvironment env)
            {
                Value = symbol;
                Parameters = parameters;
                Action = action;
                Environment = env;
            }

            public int[] Parameters { get; }
            public int Action { get; }
            public ConcumaEnvironment Environment { get; }

            public override string ToString()
            {
                return $"<{VM.SymbolNameTable[(int)Value!]}>";
            }
        }

        public sealed class Class : Symbol
        {
            public Class(int symbol, ConcumaEnvironment env)
            {
                Value = symbol;
                Environment = env;
            }

            public ConcumaEnvironment Environment { get; }

            public override string ToString()
            {
                return $"<{VM.SymbolNameTable[(int)Value!]}>";
            }
        }
    }
}
