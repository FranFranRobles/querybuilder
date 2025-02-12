using System;
using SqlKata.Utils;

namespace SqlKata
{
    public abstract class AbstractFrom : AbstractClause, IFromVisitorAcceptor
    {
        protected string _alias;

        /// <summary>
        /// Try to extract the Alias for the current clause.
        /// </summary>
        /// <returns></returns>
        public virtual string Alias { get => _alias; set => _alias = value; }

        public abstract void Accept(IFromVisitor visitor);
    }

    /// <summary>
    /// Represents a "from" clause.
    /// </summary>
    public class FromClause : AbstractFrom
    {
        public string Table { get; set; }

        public override string Alias
        {
            get
            {
                if (Table.IndexOf(" as ", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string[] segments = Table.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    return segments[2];
                }

                return Table;
            }
        }

        public override void Accept(IFromVisitor visitor)
        {
            visitor.Visit(this);
        }

        /// <inheritdoc />
        public override AbstractClause Clone()
        {
            return new FromClause
            {
                Engine = Engine,
                Alias = Alias,
                Table = Table,
                Component = Component,
            };
        }
    }

    /// <summary>
    /// Represents a "from subquery" clause.
    /// </summary>
    public class QueryFromClause : AbstractFrom
    {
        public Query Query { get; set; }

        public override string Alias
        {
            get
            {
                return string.IsNullOrEmpty(_alias) ? Query.QueryAlias : _alias;
            }
        }
        public override void Accept(IFromVisitor visitor)
        {
            visitor.Visit(this);
        }
        /// <inheritdoc />
        public override AbstractClause Clone()
        {
            return new QueryFromClause
            {
                Engine = Engine,
                Alias = Alias,
                Query = Query.Clone(),
                Component = Component,
            };
        }
    }

    public class RawFromClause : AbstractFrom
    {
        public string Expression { get; set; }
        public object[] Bindings { set; get; }
        public override void Accept(IFromVisitor visitor)
        {
            visitor.Visit(this);
        }
        /// <inheritdoc />
        public override AbstractClause Clone()
        {
            return new RawFromClause
            {
                Engine = Engine,
                Alias = Alias,
                Expression = Expression,
                Bindings = Bindings,
                Component = Component,
            };
        }
    }
}