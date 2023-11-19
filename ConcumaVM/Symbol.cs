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
            public Class(int symbol, int[] variables, int[] methods, ConcumaEnvironment env)
            {
                Value = symbol;
                Variables = variables;
                Methods = methods;
                Environment = env;
            }

            public int[] Variables { get; }
            public int[] Methods { get; }
            public ConcumaEnvironment Environment { get; }

            public override string ToString()
            {
                return $"<{VM.SymbolNameTable[(int)Value!]}>";
            }
        }

        public sealed class Env : Symbol
        {
            public Env(int symbol, ConcumaEnvironment env)
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

        public sealed class ExtFunction : Symbol
        {
            public ExtFunction(int symbol, Delegate action)
            {
                Value = symbol;
                Action = action;
            }

            public Delegate Action { get; }

            public override string ToString()
            {
                return $"<{VM.SymbolNameTable[(int)Value!]}>";
            }
        }

        public sealed class Binary : Symbol
        {
            public Binary(int symbol, int[] parameters, int action, ConcumaEnvironment env)
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
    }
}