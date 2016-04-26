using System;

namespace jamconverter.AST
{

    public class Node
    {
    }

    public class Expression : Node
    {
    }

    public class Statement : Node
    {
    }

    public class IfStatement : Statement
    {
        public Condition Condition { get; set; }
        public BlockStatement Body { get; set; }
        public BlockStatement Else { get; set; }
    }

    public class BlockStatement : Statement
    {
        public Statement[] Statements { get; set; }
    }

    public class ReturnStatement : Statement
    {
        public ExpressionList ReturnExpression { get; set; }
    }

    public class ExpressionStatement : Statement
    {
        public Expression Expression { get; set; }
    }

    public class LiteralExpression : Expression
    {
        public string Value { get; set; }
    }

    public class CombineExpression : Expression
    {
        public Expression[] Elements { get; set; }
    }

    public class VariableDereferenceExpression : Expression
    {
        public Expression VariableExpression { get; set; }
        public VariableDereferenceModifier[] Modifiers { get; set; } = {};
        public Expression IndexerExpression { get; set; }
    }

    public class VariableDereferenceModifier : Node
    {
        public char Command { get; set; }
        public Expression Value { get; set; }
    }

    public class ExpressionList : Node
    {
        public Expression[] Expressions { get; set; }
    }

    public class BinaryOperatorExpression : Expression
    {
        public Expression Left { get; set; }
        public ExpressionList Right { get; set; }
        public Operator Operator { get; set; }
    }

    public enum Operator
    {
        Assignment,
        Append,
        Subtract
    }

    public class RuleDeclarationStatement : Statement
    {
        public string Name { get; set; }
        public BlockStatement Body { get; set; }
        public string[] Arguments { get; set; }
    }

    public class ActionsDeclarationStatement : Statement
    {
        public string Name { get; set; }
        public ExpressionList Modifiers { get; set; }
        public string[] Actions { get; set; }
    }

    public class OnStatement : Statement
    {
        public ExpressionList Targets { get; set; }
        public BlockStatement Body { get; set; }
    }

    public class WhileStatement : Statement
    {
        public Condition Condition { get; set; }
        public BlockStatement Body { get; set; }
    }

    public class InvocationExpression : Expression
    {
        public Expression RuleExpression { get; set; }
        public ExpressionList[] Arguments { get; set; } = {};
    }

    public class VariableOnTargetExpression : Expression
    {
        public Expression Variable { get; set; }
        public ExpressionList Targets { get; set; }
    }

    public class Condition : Node
    {
        public bool Negated { get; set; }
        public Expression Left { get; set; }
        public Operator Operator { get; set; }
        public ExpressionList Right { get; set; }
    }

    public static class ASTExtensions
    {
        public static T As<T>(this Expression expression) where T : Expression
        {
            if (!(expression is T))
                throw new ArgumentException($"Expected expression type {typeof(T).Name} but got: {expression.GetType().Name}");
            return (T) expression;
        }

        public static T As<T>(this Statement statement) where T : Statement
        {
            if (!(statement is T))
                throw new ArgumentException($"Expected statement type {typeof(T).Name} but got: {statement.GetType().Name}");
            return (T)statement;
        }
    }
}