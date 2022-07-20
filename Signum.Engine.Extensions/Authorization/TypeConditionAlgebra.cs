using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Authorization;


public abstract class TypeConditionNode
{
    public abstract bool? ConstantValue { get; }

    public static readonly TypeConditionNode True = new AndNode(new TypeConditionNode[0]);
    public static readonly TypeConditionNode False = new OrNode(new TypeConditionNode[0]);

    public abstract bool IsMoreSimpleAndGeneralThan(TypeConditionNode og);
}

public class AndNode : TypeConditionNode
{
    public HashSet<TypeConditionNode> Nodes { get; private set; }

    public AndNode(IEnumerable<TypeConditionNode> nodes)
    {
        Nodes = nodes.ToHashSet();
    }

    public override bool? ConstantValue => Nodes.Count == 0 ? true : null;

    public override bool IsMoreSimpleAndGeneralThan(TypeConditionNode og)
    {
        if (og is AndNode complex)
        {
            //AB                         //ABC
            return this.Nodes.All(n => complex.Nodes.Contains(n));
        }

        return false;
    }

    public override string ToString() => Nodes.Count == 0 ? "TRUE" : $"AND({Nodes.ToString(", ")})";

    public override int GetHashCode() => Nodes.Sum(a => a.GetHashCode()) + 5;

    public override bool Equals(object? obj) => obj is AndNode other && other.Nodes.ToHashSet().SetEquals(other.Nodes);

}

public class OrNode : TypeConditionNode
{
    public HashSet<TypeConditionNode> Nodes { get; private set; }

    public OrNode(IEnumerable<TypeConditionNode> nodes)
    {
        Nodes = nodes.ToHashSet();
    }

    public override bool? ConstantValue => Nodes.Count == 0 ? false : null;

    public override bool IsMoreSimpleAndGeneralThan(TypeConditionNode og) => false;

    public override string ToString() => Nodes.Count == 0 ? "FALSE" : $"OR({Nodes.ToString(a => " " + a + " ", ", ")})";

    public override int GetHashCode() => Nodes.Sum(a => a.GetHashCode());

    public override bool Equals(object? obj) => obj is OrNode other && other.Nodes.ToHashSet().SetEquals(other.Nodes);
}

public class NotNode : TypeConditionNode
{
    public TypeConditionNode Operand { get; private set; }

    public NotNode(TypeConditionNode operand)
    {
        this.Operand = operand;
    }

    public override bool? ConstantValue => !Operand.ConstantValue;

    public override bool IsMoreSimpleAndGeneralThan(TypeConditionNode og) => false;

    public override string ToString() => $"NOT({Operand})";

    public override int GetHashCode() => -this.Operand.GetHashCode();
}

public class SymbolNode : TypeConditionNode
{
    public TypeConditionSymbol Symbol { get; private set; }

    public SymbolNode(TypeConditionSymbol symbol)
    {
        this.Symbol = symbol;
    }

    public override bool? ConstantValue => null;

    public override bool IsMoreSimpleAndGeneralThan(TypeConditionNode og) => false;

    public override string ToString() => Symbol.ToString();

    public override int GetHashCode() => Symbol.GetHashCode();

    public override bool Equals(object? obj) => obj is SymbolNode sn && sn.Symbol.Is(Symbol);
}

public static class TypeConditionNodeExtensions
{
    public static TypeConditionNode ToTypeConditionNode(this TypeAllowedAndConditions tac, TypeAllowedBasic requested, bool inUserInterface)
    {
        var baseValue = tac.Fallback.Get(inUserInterface) >= requested ? TypeConditionNode.True : TypeConditionNode.False;

        return tac.ConditionRules.Aggregate(baseValue, (acum, tacRule) =>
        {
            var iExp = new AndNode(tacRule.TypeConditions.Select(a => (TypeConditionNode)new SymbolNode(a)).ToHashSet());

            if (tacRule.Allowed.Get(inUserInterface) >= requested)
                return new OrNode(new TypeConditionNode[] { iExp, acum });
            else
                return new AndNode(new TypeConditionNode[] { new NotNode(iExp), acum });
        });
    }

    public static TypeConditionNode Simplify(this TypeConditionNode node)
    {
        switch (node)
        {
            case SymbolNode s: return s;
            case AndNode { Nodes: var nodes }:
                {
                    var newNodes = nodes.Select(n => Simplify(n)).Where(a => a.ConstantValue != true).ToHashSet();

                    var newNodes2 = newNodes.SelectMany(nn => nn is AndNode and ? and.Nodes : new HashSet<TypeConditionNode> { nn }).ToHashSet();

                    if (newNodes2.Any(n => n.ConstantValue == false))
                        return TypeConditionNode.False;

                    return newNodes2.Only() ?? new AndNode(newNodes2);
                }
            case OrNode { Nodes: var nodes }:
                {
                    var newNodes = nodes.Select(n => Simplify(n)).Where(a => a.ConstantValue != false).ToHashSet();

                    var newNodes2 = newNodes.SelectMany(nn => nn is OrNode or ? or.Nodes : new HashSet<TypeConditionNode> { nn }).ToHashSet();

                    if (newNodes2.Any(n => n.ConstantValue == true))
                        return TypeConditionNode.True;

                    var newNodes3 = newNodes2.Where(og => !newNodes2.Any(og2 => og2 != og && og2.IsMoreSimpleAndGeneralThan(og))).ToHashSet();

                    return newNodes3.Only() ?? new OrNode(newNodes3);
                }
            case NotNode { Operand: var operand }:
                {
                    var simp = Simplify(operand);

                    return simp.ConstantValue == true ? TypeConditionNode.False :
                        simp.ConstantValue == false ? TypeConditionNode.True :
                        new NotNode(simp);
                }
            default: throw new UnexpectedValueException(node);
        }


    }


    public static Expression ToExpression(this TypeConditionNode node, Expression entity)
    {
        if (node.ConstantValue == true)
            return Expression.Constant(true);

        if (node.ConstantValue == false)
            return Expression.Constant(false);

        if(node is SymbolNode sn)
        {
            var lambda = TypeConditionLogic.GetCondition(entity.Type, sn.Symbol);
            return Expression.Invoke(lambda, entity);
        }

        if (node is NotNode nn)
            return Expression.Negate(nn.ToExpression(entity));

        if (node is AndNode and)
            return and.Nodes.Select(n => n.ToExpression(entity)).Aggregate(Expression.Add);

        if (node is OrNode or)
            return or.Nodes.Select(n => n.ToExpression(entity)).Aggregate(Expression.Or);

        throw new UnexpectedValueException(node);
    }
}



