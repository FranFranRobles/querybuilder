using System;
using System.Collections.Generic;
using System.Text;

namespace SqlKata.Utils
{
    public interface IFromVisitor
    {
        void Visit(FromClause from);
        void Visit(RawFromClause from);
        void Visit(QueryFromClause from);
    }
}
