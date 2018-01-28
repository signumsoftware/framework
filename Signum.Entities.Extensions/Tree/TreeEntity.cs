using Microsoft.SqlServer.Types;
using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Signum.Entities.Tree
{
    [Serializable]
    public abstract class TreeEntity : Entity
    {
        [UniqueIndex]
        SqlHierarchyId route;
        [InTypeScript(false)]
        public SqlHierarchyId Route
        {
            get { return this.Get(route); }
            set
            {
                if (this.Set(ref route, value))
                    this.ParentRoute = value.GetAncestor(1).ToString();
            }
        }


        static Expression<Func<TreeEntity, string>> RouteToStringExpression =
        @this => @this.Route.ToString();
        [ExpressionField]
        public string RouteToString
        {
            get { return RouteToStringExpression.Evaluate(this); }
        }

        [NotNullable, SqlDbType(Size = 255, SqlDbType = SqlDbType.VarChar)]
        public string ParentRoute { get; set; }

        static Expression<Func<TreeEntity, int?>> LevelExpression = @this => (int?)@this.Route.GetLevel();

        [Ignore]
        int? level; 
        [ExpressionField("LevelExpression"), InTypeScript(true)]
        public int? Level
        {
            get { return level; }
            set { level = value; }
        }

        protected override void PostRetrieving()
        {
            this.level = (int)this.Route.GetLevel();
        }

        [Ignore, ImplementedByAll]
        public Lite<TreeEntity> ParentOrSibling { get; set; }

        [Ignore]
        public bool IsSibling { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 255)]
        public string Name { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 1, Max = int.MaxValue, DisabledInModelBinder = true)]
        public string FullName { get; private set; }

        public void SetFullName(string newFullName)
        {
            this.FullName = newFullName;
        }

        static Expression<Func<TreeEntity, string>> ToStringExpression = e => e.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class TreeOperation
    {
        public static readonly ConstructSymbol<TreeEntity>.Simple CreateRoot;
        public static readonly ConstructSymbol<TreeEntity>.From<TreeEntity> CreateChild;
        public static readonly ConstructSymbol<TreeEntity>.From<TreeEntity> CreateNextSibling;
        public static readonly ExecuteSymbol<TreeEntity> Save;
        public static readonly ExecuteSymbol<TreeEntity> Move;
        public static readonly ConstructSymbol<TreeEntity>.From<TreeEntity> Copy;
        public static readonly DeleteSymbol<TreeEntity> Delete;
    }



    public enum TreeMessage
    {
        Tree,
        Descendants,
        Parent,
        Ascendants,
        Children,
        Level,
        TreeType,
        [Description("Level should not be greater than {0}")]
        LevelShouldNotBeGreaterThan0,
        [Description("Impossible to move {0} inside of {1}")]
        ImpossibleToMove0InsideOf1,
        [Description("Impossible to move {0} {1} of {2}")]
        ImpossibleToMove01Of2,
        [Description("Move {0}")]
        Move0,
        [Description("Copy {0}")]
        Copy0,
    }


    public enum TreeViewerMessage
    {
        Search,
        AddRoot,
        AddChild,
        AddSibling,
        Remove,
        None,
    }

    [Serializable]
    public class MoveTreeModel : ModelEntity
    {
        [ImplementedByAll]
        public Lite<TreeEntity> NewParent { get; set; }
        public InsertPlace InsertPlace { get; set; }

        [ImplementedByAll]
        public Lite<TreeEntity> Sibling { get; set; }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if(pi.Name == nameof(Sibling) && Sibling == null && 
                (InsertPlace == InsertPlace.After || InsertPlace == InsertPlace.Before))
            {
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
            }

            return base.PropertyValidation(pi);
        }
    }

    public enum InsertPlace
    {
        FirstNode,
        After,
        Before,
        LastNode,
    }

}
