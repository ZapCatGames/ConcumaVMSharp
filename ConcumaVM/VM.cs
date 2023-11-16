using ConcumaVM;

namespace ConcumaRuntimeFramework
{
    public sealed class VM
    {
        private readonly byte[] _bytes;
        private int _current = 0;

        private readonly Dictionary<int, Symbol> _symbols = new();

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
                        EvaluateExpression();
                        break;
                    }
                case 0x02: //If
                    {
                        EvaluateExpression();
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
                            EvaluateExpression();
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
                        EvaluateExpression();
                        break;
                    }
                case 0x06: //For
                    {
                        if (Peek() != 0x00) SkipStatement();
                        EvaluateExpression();
                        SkipStatement();
                        if (Peek() != 0x00) SkipStatement();
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
                        for (int i = 0; i < len; i++)
                        {
                            EvaluateStatement();
                        }
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
                        _symbols.Add(symbol, new Symbol.Var(isConst, initializer));
                        break;
                    }
                case 0x05: // Definition
                    {
                        _current += 4;
                        int symbol = BitConverter.ToInt32(_bytes, _current - 4);
                        if ((_symbols[symbol] as Symbol.Var)!.Const)
                        {
                            throw new RuntimeException("Cannot redefine constant variable.");
                        }
                        _symbols[symbol].Value = EvaluateExpression();
                        break;
                    }
                case 0x06: // For Loop
                    {
                        if (Peek() != 0x00) EvaluateStatement();
                        int condition = _current;

                        while (true)
                        {
                            _current = condition;
                            object? c = EvaluateExpression();
                            if (TypeConverter.Truthy(c))
                            {
                                EvaluateStatement();
                                if (Peek() != 0x00) EvaluateStatement();
                            }
                            else
                            {
                                SkipStatement();
                                if (Peek() != 0x00) SkipStatement();
                                break;
                            }
                        }

                        break;
                    }
            }
        }

        private object? EvaluateExpression()
        {
            switch (Advance())
            {
                case 0x01: // Unary
                    return Unary();
                case 0x02:
                    return Binary();
                case 0x03:
                    return EvaluateExpression();
                case 0x04:
                    return Literal();
                case 0x05:
                    return Var();
                default:
                    return Literal();
            }
        }

        private object? Var()
        {
            _current += 4;
            int symbol = BitConverter.ToInt32(_bytes, _current - 4);
            return _symbols[symbol].Value;
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
