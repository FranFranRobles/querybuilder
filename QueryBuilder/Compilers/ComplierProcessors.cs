using System;
using System.Collections.Generic;
using System.Text;
using SqlKata.Utils;

namespace SqlKata.Compilers
{
    public class TableProcessor :  IFromVisitor
    {
        private string table = null;
        SqlResult context;
        internal CompilerWrap helper = null;
        public SqlResult Context
        {
            get => context;
        }
        internal TableProcessor(SqlResult context, CompilerWrap wrapper)
        {
            helper = wrapper;
            this.context = context;
        }
        public void Visit(FromClause from)
        {
            table = helper.Wrap(from.Table);
        }
        public void Visit(RawFromClause from)
        {
            table = helper.WrapIdentifiers(from.Expression);
            context.Bindings.AddRange(from.Bindings);
        }
        public string GetTableExpression()
        {
            return table;
        }

        public void Visit(QueryFromClause from)
        {
            // process nothing
        }
    }
}
