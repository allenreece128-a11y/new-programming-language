using System;
using System.Collections.Generic;

namespace NovaScript.Core
{
    public enum TokenType
    {
        // Iconic Keywords
        MutableVar, // $
        ImmutableVar, // #
        Output, // !!
        If, // ~
        Else, // ?
        Function, // &
        Loop, // *
        Structure, // @@
        Try, // ^
        Catch, // !
        Finally, // !!^
        Return, // >>
        BlockStart, // ::
        BlockEnd, // ;;
        Break, // !!>
        Continue, // !!<
        New, // new
        Import, // import
        
        // Booleans & Null
        True, // (+)
        False, // (-)
        Null, // (_)
        
        // Literals
        Identifier,
        String,
        Number,
        
        // Operators
        Plus, Minus, Star, Slash,
        Greater, Less, GreaterEqual, LessEqual, EqualEqual, BangEqual,
        Equal, // =
        
        // Punctuation
        Semicolon,
        Comma,
        LeftParen, RightParen,
        Dot,
        
        EOF
    }

    public record Token(TokenType Type, string Value, int Line);

    public class Lexer
    {
        private readonly string _source;
        private int _position = 0;
        private int _line = 1;

        public Lexer(string source)
        {
            _source = source;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            while (!IsAtEnd())
            {
                char c = Peek();
                if (char.IsWhiteSpace(c))
                {
                    if (c == '\n') _line++;
                    Advance();
                }
                else if (c == '/' && PeekNext() == '/')
                {
                    while (Peek() != '\n' && !IsAtEnd()) Advance();
                }
                else if (char.IsDigit(c))
                {
                    tokens.Add(ReadNumber());
                }
                else if (c == '"')
                {
                    tokens.Add(ReadString());
                }
                else if (char.IsLetter(c) || c == '_')
                {
                    tokens.Add(ReadIdentifier());
                }
                else
                {
                    Token? token = ReadSymbol();
                    if (token != null) tokens.Add(token);
                    else Advance(); // Skip unknown
                }
            }
            tokens.Add(new Token(TokenType.EOF, "", _line));
            return tokens;
        }

        private Token ReadNumber()
        {
            int start = _position;
            while (char.IsDigit(Peek()) || (Peek() == '.' && char.IsDigit(PeekNext())))
            {
                Advance();
            }
            return new Token(TokenType.Number, _source.Substring(start, _position - start), _line);
        }

        private Token ReadString()
        {
            Advance(); // Skip "
            int start = _position;
            while (Peek() != '"' && !IsAtEnd())
            {
                if (Peek() == '\n') _line++;
                Advance();
            }
            string value = _source.Substring(start, _position - start);
            Advance(); // Skip "
            return new Token(TokenType.String, value, _line);
        }

        private Token ReadIdentifier()
        {
            int start = _position;
            while (char.IsLetterOrDigit(Peek()) || Peek() == '_')
            {
                Advance();
            }
            string value = _source.Substring(start, _position - start);
            
            // Keyword check
            if (value == "new") return new Token(TokenType.New, value, _line);
            if (value == "import") return new Token(TokenType.Import, value, _line);
            
            return new Token(TokenType.Identifier, value, _line);
        }

        private Token? ReadSymbol()
        {
            char c = Advance();
            switch (c)
            {
                case '$': return new Token(TokenType.MutableVar, "$", _line);
                case '#': return new Token(TokenType.ImmutableVar, "#", _line);
                case '~': return new Token(TokenType.If, "~", _line);
                case '?': return new Token(TokenType.Else, "?", _line);
                case '&': return new Token(TokenType.Function, "&", _line);
                case '*': return new Token(TokenType.Star, "*", _line); // Can be Loop or Multiply
                case '^': return new Token(TokenType.Try, "^", _line);
                case '!':
                    if (Match('!'))
                    {
                        if (Match('^')) return new Token(TokenType.Finally, "!!^", _line);
                        if (Match('>')) return new Token(TokenType.Break, "!!>", _line);
                        if (Match('<')) return new Token(TokenType.Continue, "!!<", _line);
                        return new Token(TokenType.Output, "!!", _line);
                    }
                    if (Match('=')) return new Token(TokenType.BangEqual, "!=", _line);
                    return new Token(TokenType.Catch, "!", _line);
                case '@':
                    if (Match('@')) return new Token(TokenType.Structure, "@@", _line);
                    break;
                case '>':
                    if (Match('>')) return new Token(TokenType.Return, ">>", _line);
                    if (Match('=')) return new Token(TokenType.GreaterEqual, ">=", _line);
                    return new Token(TokenType.Greater, ">", _line);
                case '<':
                    if (Match('=')) return new Token(TokenType.LessEqual, "<=", _line);
                    return new Token(TokenType.Less, "<", _line);
                case ':':
                    if (Match(':')) return new Token(TokenType.BlockStart, "::", _line);
                    break;
                case ';':
                    if (Match(';')) return new Token(TokenType.BlockEnd, ";;", _line);
                    return new Token(TokenType.Semicolon, ";", _line);
                case '(':
                    if (Match('+'))
                    {
                        if (Match(')')) return new Token(TokenType.True, "(+)", _line);
                    }
                    if (Match('-'))
                    {
                        if (Match(')')) return new Token(TokenType.False, "(-)", _line);
                    }
                    if (Match('_'))
                    {
                        if (Match(')')) return new Token(TokenType.Null, "(_)", _line);
                    }
                    return new Token(TokenType.LeftParen, "(", _line);
                case ')': return new Token(TokenType.RightParen, ")", _line);
                case '=':
                    if (Match('=')) return new Token(TokenType.EqualEqual, "==", _line);
                    return new Token(TokenType.Equal, "=", _line);
                case '+': return new Token(TokenType.Plus, "+", _line);
                case '-': return new Token(TokenType.Minus, "-", _line);
                case '/': return new Token(TokenType.Slash, "/", _line);
                case ',': return new Token(TokenType.Comma, ",", _line);
                case '.': return new Token(TokenType.Dot, ".", _line);
            }
            return null;
        }

        private bool IsAtEnd() => _position >= _source.Length;

        private char Advance() => _source[_position++];

        private bool Match(char expected)
        {
            if (IsAtEnd() || _source[_position] != expected) return false;
            _position++;
            return true;
        }

        private char Peek() => IsAtEnd() ? '\0' : _source[_position];

        private char PeekNext() => _position + 1 >= _source.Length ? '\0' : _source[_position + 1];
    }
}
