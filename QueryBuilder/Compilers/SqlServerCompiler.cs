namespace SqlKata.Compilers
{
    public class SqlServerCompiler : Compiler
    {
        public SqlServerCompiler()
        {
            wrapper.OpeningIdentifier = "[";
            wrapper.ClosingIdentifier = "]";
            wrapper.LastId = "SELECT scope_identity() as Id";
        }

        public override string EngineCode { get; } = EngineCodes.SqlServer;
        public bool UseLegacyPagination { get; set; } = true;
        public string Wrap(string value)
        {
            return wrapper.Wrap(value);
        }

        protected override SqlResult CompileSelectQuery(Query query)
        {
            if (!UseLegacyPagination || !query.HasOffset(EngineCode))
            {
                return base.CompileSelectQuery(query);
            }

            query = query.Clone();

            SqlResult context = new SqlResult
            {
                Query = query,
            };

            int limit = query.GetLimit(EngineCode);
            int offset = query.GetOffset(EngineCode);


            if (!query.HasComponent("select"))
            {
                query.Select("*");
            }

            string order = CompileOrders(context) ?? "ORDER BY (SELECT 0)";

            query.SelectRaw($"ROW_NUMBER() OVER ({order}) AS [row_num]", context.Bindings.ToArray());

            query.ClearComponent("order");


            SqlResult result = base.CompileSelectQuery(query);

            if (limit == 0)
            {
                result.RawSql = $"SELECT * FROM ({result.RawSql}) AS [results_wrapper] WHERE [row_num] >= ?";
                result.Bindings.Add(offset + 1);
            }
            else
            {
                result.RawSql = $"SELECT * FROM ({result.RawSql}) AS [results_wrapper] WHERE [row_num] BETWEEN ? AND ?";
                result.Bindings.Add(offset + 1);
                result.Bindings.Add(limit + offset);
            }

            return result;
        }
        protected override string CompileColumns(SqlResult context)
        {
            string compiled = base.CompileColumns(context);

            if (!UseLegacyPagination)
            {
                return compiled;
            }

            // If there is a limit on the query, but not an offset, we will add the top
            // clause to the query, which serves as a "limit" type clause within the
            // SQL Server system similar to the limit keywords available in MySQL.
            int limit = context.Query.GetLimit(EngineCode);
            int offset = context.Query.GetOffset(EngineCode);
            
            if (limit > 0 && offset == 0)
            {
                // top bindings should be inserted first
                context.Bindings.Insert(0, limit);

                context.Query.ClearComponent("limit");

                // handle distinct
                string selectStr = string.Empty;
                if (compiled.IndexOf("SELECT DISTINCT") == 0)
                {
                    selectStr =  "SELECT DISTINCT TOP (?)" + compiled.Substring(15);
                }
                else
                {
                    selectStr = "SELECT TOP (?)" + compiled.Substring(6); ;
                }

                return selectStr;
            }

            return compiled;
        }

        public override string CompileLimit(SqlResult context)
        {
            if (UseLegacyPagination)
            {
                // in legacy versions of Sql Server, limit is handled by TOP
                // and ROW_NUMBER techniques
                return null;
            }

            int limit = context.Query.GetLimit(EngineCode);
            int offset = context.Query.GetOffset(EngineCode);

            if (limit == 0 && offset == 0)
            {
                return null;
            }

            string safeOrder = "";
            string safeOrderOffSet = string.Empty;

            if (!context.Query.HasComponent("order"))
            {
                safeOrder = "ORDER BY (SELECT 0) ";
            }

            if (limit == 0)
            {
                context.Bindings.Add(offset);
                safeOrderOffSet = $"{safeOrder}OFFSET ? ROWS";
            }
            else
            {
                context.Bindings.Add(offset);
                context.Bindings.Add(limit);
                safeOrderOffSet = $"{safeOrder}OFFSET ? ROWS FETCH NEXT ? ROWS ONLY"; 
            }

            return safeOrderOffSet;
        }

        public override string CompileRandom(string seed)
        {
            return "NEWID()";
        }

        public override string CompileTrue()
        {
            return "cast(1 as bit)";
        }

        public override string CompileFalse()
        {
            return "cast(0 as bit)";
        }

        protected override string CompileBasicDateCondition(SqlResult context, BasicDateCondition condition)
        {
            string column = wrapper.Wrap(condition.Column);
            string part = condition.Part.ToUpperInvariant();

            string left;

            if (part == "TIME" || part == "DATE")
            {
                left = $"CAST({column} AS {part.ToUpperInvariant()})";
            }
            else
            {
                left = $"DATEPART({part.ToUpperInvariant()}, {column})";
            }

            string sql = $"{left} {condition.Operator} {Parameter(context, condition.Value)}";

            if (condition.IsNot)
            {
                sql = $"NOT ({sql})";
            }

            return sql;
        }
    }
}
