using System.Reflection;

namespace ConcumaVM
{
    public sealed partial class VM
    {
        public static readonly Dictionary<int, string> SymbolNameTable = new();

        private readonly byte[] _bytes;
        private int _current = 0;
        private int _stLoc = 0;

        private ConcumaEnvironment _currentEnv = new(null);

        public VM(byte[] bytes)
        {
            _bytes = bytes;
        }

        public void Run()
        {
            SymbolNameTable.Clear();

            _current += 4;
            _stLoc = BitConverter.ToInt32(_bytes, _current - 4);
            _current = _stLoc;

            while (!IsEnd())
            {
                _current += 4;
                int symbolNameLen = BitConverter.ToInt32(_bytes, _current - 4);
                string value = "";
                for (int i = 0; i < symbolNameLen; i++)
                {
                    value += (char)Advance();
                }
                _current += 4;
                int symbol = BitConverter.ToInt32(_bytes, _current - 4);
                SymbolNameTable.Add(symbol, value);
            }

            _current = 4;

            try
            {
                while (!IsStatementEnd())
                {
                    EvaluateStatement();
                }
            }
            catch (RuntimeException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"RuntimeError: {e.Message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private void SkipStatement()
        {
            switch (Advance())
            {
                case 0x01: //Print
                    {
                        SkipExpression();
                        break;
                    }
                case 0x02: //If
                    {
                        SkipExpression();
                        SkipStatement();
                        if (Peek() == 0x00)
                        {
                            Advance();
                        }
                        else
                        {
                            SkipStatement();
                        }
                        break;
                    }
                case 0x03: //Block
                    {
                        _current += 4;
                        int len = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < len; i++)
                        {
                            SkipStatement();
                        }
                        break;
                    }
                case 0x04: // Declaration
                    {
                        Advance();
                        _current += 4;
                        if (Peek() != 0x00)
                        {
                            SkipExpression();
                        }
                        else
                        {
                            Advance();
                        }
                        break;
                    }
                case 0x05: //Definition
                    {
                        _current += 4;
                        SkipExpression();
                        break;
                    }
                case 0x06: //For
                    {
                        if (Peek() != 0x00) SkipStatement();
                        else Advance();
                        SkipExpression();
                        SkipStatement();
                        if (Peek() != 0x00) SkipStatement();
                        else Advance();
                        break;
                    }
                case 0x07: //Break
                    break;
                case 0x08: //Function Declaration
                    {
                        _current += 8;
                        int len = BitConverter.ToInt32(_bytes, _current - 4);
                        _current += len * 4;
                        SkipStatement();
                        break;
                    }
                case 0x09: //Function Call
                    {
                        _current += 8;
                        int len = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < len; i++)
                        {
                            SkipExpression();
                        }
                        break;
                    }
                case 0x0A: //Return
                    {
                        if (Peek() != 0x00) SkipExpression();
                        else Advance();
                        break;
                    }
                case 0x0B: //Class
                    {
                        _current += 8;
                        int varLen = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < varLen; i++)
                        {
                            SkipStatement();
                        }
                        _current += 4;
                        int methodLen = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < methodLen; i++)
                        {
                            SkipStatement();
                        }
                        break;
                    }
                case 0x0C: //Module
                    {
                        _current += 8;
                        int varLen = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < varLen; i++)
                        {
                            SkipStatement();
                        }
                        _current += 4;
                        int methodLen = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < methodLen; i++)
                        {
                            SkipStatement();
                        }
                        break;
                    }
            }
        }

        private void EvaluateStatement()
        {
            switch (Advance())
            {
                case 0x01: //Print
                    {
                        Console.WriteLine(EvaluateExpression());
                        break;
                    }
                case 0x02: //If
                    {
                        bool conditionMet = false;
                        object? condition = EvaluateExpression();
                        if (condition is bool b)
                        {
                            conditionMet = b;
                        }

                        if (conditionMet)
                        {
                            EvaluateStatement();
                            SkipStatement();
                        }
                        else
                        {
                            SkipStatement();

                            if (Peek() == 0x00)
                            {
                                Advance();
                                break;
                            }

                            EvaluateStatement();
                        }

                        break;
                    }
                case 0x03: // Block
                    {
                        _current += 4;
                        int len = BitConverter.ToInt32(_bytes, _current - 4);
                        _currentEnv = new ConcumaEnvironment(_currentEnv);
                        for (int i = 0; i < len; i++)
                        {
                            EvaluateStatement();
                        }
                        _currentEnv = _currentEnv.Exit()!;
                        break;
                    }
                case 0x04: // Declaration
                    {
                        bool isConst = Advance() == 0x01;
                        _current += 4;
                        int symbol = BitConverter.ToInt32(_bytes, _current - 4);
                        object? initializer = null;
                        if (Peek() != 0x00)
                        {
                            initializer = TypeConverter.Cast(EvaluateExpression());
                        }
                        _currentEnv.Add(symbol, new Symbol.Var(isConst, initializer));
                        break;
                    }
                case 0x05: // Definition
                    {
                        _current += 4;
                        int symbol = BitConverter.ToInt32(_bytes, _current - 4);
                        if ((_currentEnv.Find(symbol) as Symbol.Var)!.Const)
                        {
                            throw new RuntimeException("Cannot redefine constant variable.");
                        }
                        _currentEnv.Find(symbol).Value = TypeConverter.Cast(EvaluateExpression());
                        break;
                    }
                case 0x06: // For Loop
                    {
                        _currentEnv = new ConcumaEnvironment(_currentEnv);
                        if (Peek() != 0x00) EvaluateStatement();
                        int condition = _current;

                        while (true)
                        {
                            _current = condition;
                            object? c = EvaluateExpression();
                            if (TypeConverter.Truthy(c))
                            {
                                try
                                {
                                    EvaluateStatement();
                                }
                                catch (BreakException)
                                {
                                    EvaluateStatement();
                                    break;
                                }

                                if (Peek() != 0x00) EvaluateStatement();
                            }
                            else
                            {
                                SkipStatement();
                                if (Peek() != 0x00) SkipStatement();
                                break;
                            }
                        }

                        _currentEnv = _currentEnv.Exit()!;

                        break;
                    }
                case 0x07: //Break
                    {
                        throw new BreakException();
                    }
                case 0x08: //Function Declaration
                    {
                        _current += 4;
                        int symbol = BitConverter.ToInt32(_bytes, _current - 4);
                        _currentEnv = new ConcumaEnvironment(_currentEnv);
                        _current += 4;
                        int paramLen = BitConverter.ToInt32(_bytes, _current - 4);
                        int[] parameters = new int[paramLen];
                        for (int i = 0; i < paramLen; i++)
                        {
                            _current += 4;
                            int pSymbol = BitConverter.ToInt32(_bytes, _current - 4);
                            parameters[i] = pSymbol;
                            _currentEnv.Add(pSymbol, new Symbol.Var(false, null));
                        }
                        ConcumaEnvironment env = _currentEnv;
                        _currentEnv = _currentEnv.Exit()!;
                        _currentEnv.Add(symbol, new Symbol.Function(symbol, parameters, _current, env));
                        SkipStatement();
                        break;
                    }
                case 0x09: //Function call
                    {
                        Call();
                        break;
                    }
                case 0x0A: //Return
                    {
                        object? value = null;
                        if (Peek() != 0x00) value = TypeConverter.Cast(EvaluateExpression());
                        throw new ReturnException(value);
                    }
                case 0x0B: //Class
                    {
                        _current += 4;
                        int symbol = BitConverter.ToInt32(_bytes, _current - 4);
                        _currentEnv = new ConcumaEnvironment(_currentEnv);
                        _current += 4;
                        int varLen = BitConverter.ToInt32(_bytes, _current - 4);
                        int[] variables = new int[varLen];
                        for (int i = 0; i < varLen; i++)
                        {
                            variables[i] = _current;
                            SkipStatement();
                        }
                        _current += 4;
                        int methodLen = BitConverter.ToInt32(_bytes, _current - 4);
                        int[] methods = new int[methodLen];
                        for (int i = 0; i < methodLen; i++)
                        {
                            methods[i] = _current;
                            SkipStatement();
                        }
                        ConcumaEnvironment env = _currentEnv;
                        _currentEnv = _currentEnv.Exit()!;
                        _currentEnv.Add(symbol, new Symbol.Class(symbol, variables, methods, env));
                        break;
                    }
                case 0x0C: //Module
                    {
                        _current += 4;
                        int symbol = BitConverter.ToInt32(_bytes, _current - 4);
                        _currentEnv = new ConcumaEnvironment(_currentEnv);
                        _current += 4;
                        int varLen = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < varLen; i++)
                        {
                            EvaluateStatement();
                        }
                        _current += 4;
                        int methodLen = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < methodLen; i++)
                        {
                            EvaluateStatement();
                        }
                        ConcumaEnvironment env = _currentEnv;
                        _currentEnv = _currentEnv.Exit()!;
                        _currentEnv.Add(symbol, new Symbol.Env(symbol, env));
                        break;
                    }
                case 0x0D: //Import
                    {
                        _current += 4;
                        int symbol = BitConverter.ToInt32(_bytes, _current - 4);
                        int aliasSymbol = symbol;
                        if (Peek() != 0x00)
                        {
                            _current += 4;
                            aliasSymbol = BitConverter.ToInt32(_bytes, _current - 4);
                        }
                        else
                        {
                            Advance();
                        }
                        string name = SymbolNameTable[symbol];
                        Delegate? del = Blackboard.Get(name);
                        if (del is null) throw new RuntimeException("Tried to import non-existent method.");
                        foreach (ParameterInfo p in del.Method.GetParameters())
                        {
                            if (p.ParameterType != typeof(object)) throw new RuntimeException("External method arguments have to be of type <object>.");
                        }
                        _currentEnv.Add(aliasSymbol, new Symbol.ExtFunction(aliasSymbol, del));
                        break;
                    }
            }
        }

        private void SkipExpression()
        {
            switch (Advance())
            {
                case 0x01: // Unary
                    Advance();
                    SkipExpression();
                    break;
                case 0x02: // Binary
                    Advance();
                    SkipExpression();
                    SkipExpression();
                    break;
                case 0x03: // Group
                    SkipExpression();
                    break;
                case 0x04: // Literal
                    Literal();
                    break;
                case 0x05: // Var
                    _current += 4;
                    break;
                case 0x06: // Call
                    {
                        _current += 8;
                        int len = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < len; i++)
                        {
                            SkipExpression();
                        }
                        break;
                    }
                default:
                    Literal();
                    break;
            }
        }

        private object? EvaluateExpression()
        {
            switch (Advance())
            {
                case 0x01: // Unary
                    return Unary();
                case 0x02: // Binary
                    return Binary();
                case 0x03: // Group
                    return EvaluateExpression();
                case 0x04: // Literal
                    return Literal();
                case 0x05: // Var
                    return Var();
                case 0x06: // Call
                    return Call();
                case 0x07:
                    return Accessor();
                default:
                    throw new RuntimeException("Unknown expression type.");
            }
        }

        private object? Accessor()
        {
            object? left = EvaluateExpression();
            if (left is Symbol.Env env)
            {
                _currentEnv = env.Environment;
            }
            object? right = EvaluateExpression();
            return right;
        }

        private object? Call()
        {
            _current += 4;
            int symbol = BitConverter.ToInt32(_bytes, _current - 4);
            _current += 4;
            int paramLen = BitConverter.ToInt32(_bytes, _current - 4);
            object?[] parameters = new object?[paramLen];
            for (int i = 0; i < paramLen; i++)
            {
                parameters[i] = EvaluateExpression();
                if (parameters[i] is Symbol.Var v)
                {
                    parameters[i] = v.Value;
                }
            }
            Symbol sym = _currentEnv.Find(symbol);
            if (sym is Symbol.Function funSym)
            {
                ConcumaEnvironment prevEnv = _currentEnv;
                _currentEnv = funSym.Environment;
                ConcumaFunction func = new(funSym.Parameters, funSym.Action);
                int currentLine = _current;
                _current = func.Call(parameters, _currentEnv);
                try
                {
                    EvaluateStatement();
                }
                catch (ReturnException r)
                {
                    _current = currentLine;
                    _currentEnv = prevEnv;
                    return r.Value;
                }
                _current = currentLine;
                _currentEnv = prevEnv;
            }
            else if (sym is Symbol.ExtFunction extSym)
            {
                if (parameters.Length != extSym.Action.Method.GetParameters().Length) throw new RuntimeException("Invalid number of parameters for external function.");
                return extSym.Action.DynamicInvoke(parameters);
            }
            return null;
        }

        private object? Var()
        {
            _current += 4;
            int symbol = BitConverter.ToInt32(_bytes, _current - 4);
            return _currentEnv.Find(symbol);
        }

        private object Unary()
        {
            byte op = Advance();
            object? right = EvaluateExpression();

            switch (op)
            {
                case 0x01: // !
                    {
                        if (right is bool b)
                            return !b;

                        if (right is int i)
                        {
                            if (i == 0) return 1;
                            else return 0;
                        }

                        if (right is double d)
                        {
                            if (d == 0) return 1;
                            else return 0;
                        }

                        break;
                    }
                case 0x02: // -
                    {
                        if (right is int i)
                            return -i;

                        if (right is double d)
                            return -d;

                        break;
                    }
            }

            throw new Exception();
        }

        private object? Binary()
        {
            byte op = Advance();
            object? left = EvaluateExpression();
            object? right = EvaluateExpression();

            switch (op)
            {
                case 0x01: // +
                    {
                        return TypeConverter.Add(left, right);
                    }
                case 0x02: // -
                    {
                        return TypeConverter.Subtract(left, right);
                    }
                case 0x03: // *
                    {
                        return TypeConverter.Multiply(left, right);
                    }
                case 0x04: // /
                    {
                        return TypeConverter.Divide(left, right);
                    }
                case 0x05: // ==
                    {
                        return TypeConverter.Equals(left, right);
                    }
                case 0x06: // !=
                    {
                        return !TypeConverter.Equals(left, right);
                    }
                case 0x07: // <
                    {
                        return TypeConverter.Less(left, right);
                    }
                case 0x08: // <=
                    {
                        return TypeConverter.LessEqual(left, right);
                    }
                case 0x09: // >
                    {
                        return TypeConverter.Greater(left, right);
                    }
                case 0x0A: // >=
                    {
                        return TypeConverter.GreaterEqual(left, right);
                    }
                case 0x0B: // .
                    {
                        if (left is not Symbol ls) throw new RuntimeException("Expected symbol on left hand of accessor.");
                        if (ls.Value is Symbol.Env)
                        {
                            if (right is not Symbol rs) throw new RuntimeException("Expected symbol on right hand of accessor.");
                            return rs.Value;
                        }
                        throw new RuntimeException("Cannot apply accessor.");
                    }
            }

            throw new Exception();
        }

        private object? Literal()
        {
            byte type = Advance();

            switch (type)
            {
                case 0x00: //Null
                    return null;
                case 0x01: //Bool
                    return Advance() == 0x01;
                case 0x02: //Int
                    _current += 4;
                    return BitConverter.ToInt32(_bytes, _current - 4);
                case 0x03: //Double
                    _current += 8;
                    return BitConverter.ToDouble(_bytes, _current - 8);
                case 0x04: //String
                    _current += 4;
                    int length = BitConverter.ToInt32(_bytes, _current - 4);
                    string value = "";
                    for (int i = 0; i < length; i++)
                    {
                        value += (char)Advance();
                    }
                    return value;
            }

            throw new RuntimeException("Unknown literal type.");
        }

        private bool IsStatementEnd() => _current >= _stLoc;
        private bool IsEnd() => _current >= _bytes.Length;

        private byte Peek() => _bytes[_current];
        private byte Advance()
        {
            if (IsEnd()) return 0x00;
            return _bytes[_current++];
        }
    }
}
