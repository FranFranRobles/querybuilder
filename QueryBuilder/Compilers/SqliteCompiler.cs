using System.Collections.Generic;
using SqlKata;
using SqlKata.Compilers;

namespace SqlKata.Compilers
{
    public class SqliteCompiler : Compiler
    {
        public override string EngineCode { get; } = EngineCodes.Sqlite;

        public SqliteCompiler() : base()
        {
            wrapper = new SqlLiteWrap();
        }

        public override string CompileTrue()
        {
            return "1";
        }

        public override string CompileFalse()
        {
            return "0";
        }

        public override string CompileLimit(SqlResult context)
        {
            int limit = context.Query.GetLimit(EngineCode);
            int offset = context.Query.GetOffset(EngineCode);

            if (limit == 0 && offset > 0)
            {
                context.Bindings.Add(offset);
                return "LIMIT -1 OFFSET ?";
            }

            return base.CompileLimit(context);
        }

        protected override string CompileBasicDateCondition(SqlResult context, BasicDateCondition condition)
        {
            string column = wrapper.Wrap(condition.Column);
            string value = Parameter(context, condition.Value);

            Dictionary<string,string> formatMap = new Dictionary<string, string> {
                {"date", "%Y-%m-%d"},
                {"time", "%H:%M:%S"},
                {"year", "%Y"},
                {"month", "%m"},
                {"day", "%d"},
                {"hour", "%H"},
                {"minute", "%M"},
            };

            if (!formatMap.ContainsKey(condition.Part))
            {
                return $"{column} {condition.Operator} {value}";
            }

            string sql = $"strftime('{formatMap[condition.Part]}', {column}) {condition.Operator} cast({value} as text)";

            if (condition.IsNot)
            {
                return $"NOT ({sql})";
            }

            return sql;
        }

    }
}
