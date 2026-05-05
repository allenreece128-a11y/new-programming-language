using System.Collections.Generic;

namespace NovaScript.Core
{
    public abstract record Expr;
    public record LiteralExpr(object? Value) : Expr;
    public record VariableExpr(Token Name) : Expr;
    public record BinaryExpr(Expr Left, Token Operator, Expr Right) : Expr;
    public record UnaryExpr(Token Operator, Expr Right) : Expr;
    public record CallExpr(Expr Callee, Token Paren, List<Expr> Arguments) : Expr;
    public record GetExpr(Expr Object, Token Name) : Expr;
    public record SetExpr(Expr Object, Token Name, Expr Value) : Expr;
    public record AssignExpr(Token Name, Expr Value) : Expr;
    public record LogicalExpr(Expr Left, Token Operator, Expr Right) : Expr;
    public record NewExpr(Token Name, List<Expr> Arguments) : Expr;

    public abstract record Stmt;
    public record ExpressionStmt(Expr Expression) : Stmt;
    public record OutputStmt(Expr Expression) : Stmt;
    public record VarStmt(Token Name, Expr? Initializer, bool IsMutable) : Stmt;
    public record ImportStmt(Token Keyword, Expr PathExpression) : Stmt;
    public record BlockStmt(List<Stmt> Statements) : Stmt;
    public record IfStmt(Expr Condition, Stmt ThenBranch, Stmt? ElseBranch) : Stmt;
    public record WhileStmt(Expr Condition, Stmt Body) : Stmt;
    public record BreakStmt(Token Keyword) : Stmt;
    public record ContinueStmt(Token Keyword) : Stmt;
    public record FunctionStmt(Token Name, List<Parameter> Params, List<Stmt> Body) : Stmt;
    public record ReturnStmt(Token Keyword, Expr? Value) : Stmt;
    public record StructureStmt(Token Name, List<VarStmt> Properties, List<FunctionStmt> Methods) : Stmt;
    public record TryCatchStmt(List<Stmt> TryBlock, Token? CatchVar, List<Stmt>? CatchBlock, List<Stmt>? FinallyBlock) : Stmt;

    public record Parameter(Token Name, bool IsMutable);
}
