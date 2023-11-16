using ConcumaVM;

namespace ConcumaRuntimeFramework
{
    public sealed class VM
    {
        private readonly byte[] _bytes;
        private int _current = 0;

        private ConcumaEnvironment _currentEnv = new(null);

        public VM(byte[] bytes)
        {
            _bytes = bytes;
        }

        public void Run()
        {
            try
            {
                while (!IsEnd())
                {
                    EvaluateStatement();
                }
            }
            catch (RuntimeException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
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
                case 0x08: //Function Definition
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
                            initializer = EvaluateExpression();
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
                        _currentEnv.Find(symbol).Value = EvaluateExpression();
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
                        _currentEnv = new ConcumaEnvironment(_currentEnv);
                        _current += 4;
                        int symbol = BitConverter.ToInt32(_bytes, _current - 4);
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
                        _currentEnv.Add(symbol, new Symbol.Function(paramLen, parameters, _current, env));
                        SkipStatement();
                        break;
                    }
                case 0x09: //Function call
                    {
                        _current += 4;
                        int symbol = BitConverter.ToInt32(_bytes, _current - 4);
                        _current += 4;
                        int paramLen = BitConverter.ToInt32(_bytes, _current - 4);
                        object?[] parameters = new object?[paramLen];
                        for (int i = 0; i < paramLen; i++)
                        {
                            parameters[i] = EvaluateExpression();
                        }
                        Symbol.Function funSym = (_currentEnv.Find(symbol) as Symbol.Function)!;
                        ConcumaEnvironment prevEnv = _currentEnv;
                        _currentEnv = funSym.Environment;
                        ConcumaFunction func = new(funSym.Parameters, funSym.Action);
                        int currentLine = _current;
                        _current = func.Call(parameters, _currentEnv);
                        try
                        {
                            EvaluateStatement();
                        }
                        catch (ReturnException)
                        {
                            _current = currentLine;
                            _currentEnv = prevEnv;
                            break;
                        }
                        _current = currentLine;
                        _currentEnv = prevEnv;
                        break;
                    }
                case 0x0A: //Return
                    {
                        object? value = null;
                        if (Peek() != 0x00) value = EvaluateExpression();
                        throw new ReturnException(value);
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
                default:
                    return Literal();
            }
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
            }
            Symbol.Function funSym = (_currentEnv.Find(symbol) as Symbol.Function)!;
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
            return null;
        }

        private object? Var()
        {
            _current += 4;
            int symbol = BitConverter.ToInt32(_bytes, _current - 4);
            return _currentEnv.Find(symbol).Value;
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

        private object Binary()
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
            }

            throw new Exception();
        }

        private object? Literal()
        {
            byte type = Advance();

            switch (type)
            {
                case 0x00:
                    return null;
                case 0x01:
                    return Advance() == 0x01;
                case 0x02:
                    _current += 4;
                    return BitConverter.ToInt32(_bytes, _current - 4);
                case 0x03:
                    _current += 8;
                    return BitConverter.ToDouble(_bytes, _current - 8);
                case 0x04:
                    byte length = Advance();
                    string value = "";
                    for (int i = 0; i < length; i++)
                    {
                        value += (char)Advance();
                    }
                    return value;
            }

            throw new Exception();
        }

        private bool IsEnd() => _current >= _bytes.Length;

        private byte Peek() => _bytes[_current];
        private byte Advance()
        {
            if (IsEnd()) return 0x00;
            return _bytes[_current++];
        }
    }
}
