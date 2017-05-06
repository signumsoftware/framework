using Microsoft.SqlServer.Types;
using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Tree
{
    [Serializable]
    public abstract class TreeEntity : Entity
    {
        [UniqueIndex, InTypeScript(false)]
        public SqlHierarchyId Route { get; set; }


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

        [SqlDbType(Size = 255)]
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 255)]
        public string Name { get; set; }

        [NotNullable, SqlDbType(Size = 1000)]
        [StringLengthValidator(AllowNulls = true, Min = 1, Max = 1000)] //Set by BL
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
        LevelShouldNotBeGreaterThan0
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
}
