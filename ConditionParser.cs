using System;
using System.Collections.Generic;
using System.Linq;
using jamconverter.AST;

namespace jamconverter
{
    partial class Parser
    {
        interface IEvaluationStackNode
        {
        }

        class ValueNode : IEvaluationStackNode
        {
            public readonly NodeList<Expression> Expressions;

            public ValueNode(NodeList<Expression> expressions)
            {
                Expressions = expressions;
            }
        }

        class ParenthesisOpenNode : IEvaluationStackNode
        {
        }

        class OperatorNode : IEvaluationStackNode
        {
            public readonly Operator Operator;

            public OperatorNode(Operator @operator)
            {
                Operator = @operator;
            }
        }

        public Expression ParseCondition()
        {
            var stack = new Stack<IEvaluationStackNode>();
            Operator? mostRecentOperator = null;

            while (true)
            {
                var nextToken = _scanResult.Peek();
                if (nextToken.literal == "(")
                {
                    stack.Push(new ParenthesisOpenNode());
                    _scanResult.Next();
                    continue;
                }

                stack.Push(ParseValue(mostRecentOperator));

                var nextTokenType = _scanResult.Peek().tokenType;

                if (nextTokenType == TokenType.ParenthesisClose)
                {
                    CollapseStack(stack);
                    _scanResult.Next();
                    nextTokenType = _scanResult.Peek().tokenType; ;
                }

                if (!IsBinaryOperator(nextTokenType))
                {
                    CollapseStack(stack);
                    return ((ValueNode)stack.Single()).Expressions.Single();
                }

                _scanResult.Next();

                var nextOperator = OperatorFor(nextTokenType);
                if (mostRecentOperator.HasValue)
                    if (PrecendenceFor(nextOperator) < PrecendenceFor(mostRecentOperator.Value))
                        CollapseStack(stack);

                stack.Push(new OperatorNode(nextOperator));
                mostRecentOperator = nextOperator;
            }
        }

        private void CollapseStack(Stack<IEvaluationStackNode> stack)
        {
            while (stack.Count() > 1)
            {
                if (stack.Peek() is ParenthesisOpenNode)
                {
                    stack.Pop();
                    return;
                }

                var rightNode = (ValueNode)stack.Pop();
                var evaluationStackNode = stack.Pop();
                if (evaluationStackNode is ParenthesisOpenNode)
                {
                    stack.Push(rightNode);
                    return;
                }

                var @operator = (OperatorNode)evaluationStackNode;
                var left = ((ValueNode)stack.Pop()).Expressions;

                var boe = new BinaryOperatorExpression() { Left = left.Single(), Operator = @operator.Operator, Right = rightNode.Expressions };
                stack.Push(new ValueNode(new NodeList<Expression>() { boe }));
            }
        }

        ValueNode ParseValue(Operator? @operator)
        {
            if (@operator == Operator.In)
                return new ValueNode(new NodeList<Expression>(ParseExpressionList()));

            var expression = ParseExpression();
            if (expression == null)
                throw new Exception();
            return new ValueNode(new NodeList<Expression> { expression });
        }

        private static int PrecendenceFor(Operator o)
        {
            var order = new List<Operator>()
            {
                Operator.Or,
                Operator.And,

                Operator.Subtract,
                Operator.AssignmentIfEmpty,
                Operator.Append,

                Operator.In,
                Operator.Assignment,
                Operator.NotEqual,
                Operator.GreaterThan,
                Operator.LessThan
            };

            return order.IndexOf(o);
        }
    }
}
