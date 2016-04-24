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
        public Expression Condition { get; set; }
        public BlockStatement Body { get; set; }
    }

    public class BlockStatement : Statement
    {
        public Statement[] Statements { get; set; }
    }

    public class ReturnStatement : Statement
    {
        public Expression ReturnExpression { get; set; }
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
        public VariableDereferenceModifier[] Modifiers { get; set; }
        public Expression IndexerExpression { get; set; }
    }

    public class VariableDereferenceModifier : Node
    {
        public char Command { get; set; }
        public Expression Value { get; set; }
    }

    public class ExpressionListExpression : Expression
    {
        public Expression[] Expressions { get; set; }
    }

    public class AssignmentExpression : Expression
    {
        public Expression Left { get; set; }
        public Expression Right { get; set; }
        public Operator Operator { get; set; }
    }

    public enum Operator
    {
        Assignment
    }

    public class RuleDeclaration : Node
    {
        public string Name { get; set; }
        public BlockStatement Body { get; set; }
        public string[] Arguments { get; set; }
    }


    public class InvocationExpression : Expression
    {
        public Node RuleExpression { get; set; }
        public Expression[] Arguments { get; set; }
    }
}