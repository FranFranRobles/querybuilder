using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlKata.Compilers
{
    public partial class Compiler
    {
        private readonly ConditionsCompilerProvider _compileConditionMethodsProvider;
        protected virtual string parameterPlaceholder { get; set; } = "?";
        protected virtual string parameterPrefix { get; set; } = "@p";
        protected virtual string OpeningIdentifier { get; set; } = "\"";
        protected virtual string ClosingIdentifier { get; set; } = "\"";
        protected virtual string ColumnAsKeyword { get; set; } = "AS ";
        protected virtual string TableAsKeyword { get; set; } = "AS ";
        protected virtual string LastId { get; set; } = "";
        protected virtual string EscapeCharacter { get; set; } = "\\";

        protected Compiler()
        {
            _compileConditionMethodsProvider = new ConditionsCompilerProvider(this);
        }

        public virtual string EngineCode { get; }


        /// <summary>
        /// A list of white-listed operators
        /// </summary>
        /// <value></value>
        protected readonly HashSet<string> operators = new HashSet<string>
        {
            "=", "<", ">", "<=", ">=", "<>", "!=", "<=>",
            "like", "not like",
            "ilike", "not ilike",
            "like binary", "not like binary",
            "rlike", "not rlike",
            "regexp", "not regexp",
            "similar to", "not similar to"
        };

        /// <summary>
        /// user defined white listed operators to be used with Where/Have Methods.
        /// This will will prevent passing arbitrary operators that could be used for a SQL injection

        /// </summary>
        protected HashSet<string> userOperators = new HashSet<string> { }; // why not ()?

        //methods Begin

        protected Dictionary<string, object> generateNamedBindings(object[] bindings)
        {
            return Helper.Flatten(bindings).Select((v, i) => new { i, v })
                .ToDictionary(x => parameterPrefix + x.i, x => x.v);
        }

        protected SqlResult PrepareResult(SqlResult ctx)
        {
            ctx.NamedBindings = generateNamedBindings(ctx.Bindings.ToArray());
            ctx.Sql = Helper.ReplaceAll(ctx.RawSql, parameterPlaceholder, i => parameterPrefix + i);
            return ctx;
        }


        private Query TransformAggregateQuery(Query query)
        {
            var clause = query.GetOneComponent<AggregateClause>("aggregate", EngineCode);

            if (clause.Columns.Count == 1 && !query.IsDistinct) return query;

            if (query.IsDistinct)
            {
                query.ClearComponent("aggregate", EngineCode);
                query.ClearComponent("select", EngineCode);
                query.Select(clause.Columns.ToArray());
            }
            else
            {
                foreach (var column in clause.Columns)
                {
                    query.WhereNotNull(column);
                }
            }

            var outerClause = new AggregateClause()
            {
                Columns = new List<string> { "*" },
                Type = clause.Type
            };

            return new Query()
                .AddComponent("aggregate", outerClause)
                .From(query, $"{clause.Type}Query");
        }
        /// <summary>
        /// Compiles the Query into A sqlresult that is an insert, update, delete, aggeregate statement
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        protected virtual SqlResult CompileRaw(Query query)
        {
            SqlResult sqlResult;

            if (query.Method == "insert")
            {
                sqlResult = CompileInsertQuery(query);
            }
            else if (query.Method == "update")
            {
                sqlResult = CompileUpdateQuery(query);
            }
            else if (query.Method == "delete")
            {
                sqlResult = CompileDeleteQuery(query);
            }
            else
            {
                if (query.Method == "aggregate")
                {
                    query.ClearComponent("limit")
                        .ClearComponent("order")
                        .ClearComponent("group");

                    query = TransformAggregateQuery(query);
                }

                sqlResult = CompileSelectQuery(query);
            }

            // handle CTEs
            if (query.HasComponent("cte", EngineCode))
            {
                sqlResult = CompileCteQuery(sqlResult, query);
            }

            sqlResult.RawSql = Helper.ExpandParameters(sqlResult.RawSql, "?", sqlResult.Bindings.ToArray());

            return sqlResult;
        }

        /// <summary>
        /// Add the passed operator(s) to the white list so they can be used with
        /// the Where/Having methods, this prevents passing arbitrary operators
        /// that opens the door for SQL injections.
        /// </summary>
        /// <param name="operators"> operators to be white listed</param>
        /// <returns></returns>
        public Compiler Whitelist(params string[] operators)
        {
            foreach (var op in operators)
            {
                this.userOperators.Add(op);
            }

            return this;  
        }
        /// <summary>
        /// Compiles the query into a SQL Result
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public virtual SqlResult Compile(Query query)
        {
            SqlResult sqlResult = CompileRaw(query);

            sqlResult = PrepareResult(sqlResult);

            return sqlResult;
        }

        public virtual SqlResult Compile(IEnumerable<Query> queries)
        {
            var compiled = queries.Select(CompileRaw).ToArray();
            var bindings = compiled.Select(r => r.Bindings).ToArray();
            var totalBindingsCount = bindings.Select(b => b.Count).Aggregate((a, b) => a + b);

            var combinedBindings = new List<object>(totalBindingsCount);
            foreach (var cb in bindings)
            {
                combinedBindings.AddRange(cb);
            }

            var ctx = new SqlResult
            {
                RawSql = compiled.Select(r => r.RawSql).Aggregate((a, b) => a + ";\n" + b),
                Bindings = combinedBindings,
            };

            ctx = PrepareResult(ctx);

            return ctx;
        }

        protected virtual SqlResult CompileSelectQuery(Query query)
        {
            SqlResult context = new SqlResult
            {
                Query = query.Clone(),
            };

            List<string> results = new[] {
                    this.CompileColumns(context),
                    this.CompileFrom(context),
                    this.CompileJoins(context),
                    this.CompileWheres(context),
                    this.CompileGroups(context),
                    this.CompileHaving(context),
                    this.CompileOrders(context),
                    this.CompileLimit(context),
                    this.CompileUnion(context),
                }
               .Where(x => x != null)
               .Where(x => !string.IsNullOrEmpty(x))
               .ToList();

            string sql = string.Join(" ", results);

            context.RawSql = sql;

            return context;
        }
        /// <summary>
        /// Creates a new SQL Result object and sets the SqlResult query property
        /// </summary>
        /// <param name="query">query to intialize SQL Result obj with</param>
        /// <param name="errorMsg">Msg of which operation failed</param>
        /// <returns></returns>
        private SqlResult IntializeSQL_Result(Query query, string errorMsg)
        {
            if(!query.HasComponent("from", EngineCode))
            {
                throw new InvalidOperationException("No table set to " + errorMsg);
            }
            SqlResult sqlResult = new SqlResult
            {
                Query = query
            };

            return sqlResult;
        }
        private string MakeTable(SqlResult context)
        {
            AbstractFrom fromClause = context.Query.GetOneComponent<AbstractFrom>("from", EngineCode);

            // this is only found on only one method. that is the CompileInsertQuery
            // added it and still passes all unit tests
            if (fromClause is null)
            {
                throw new InvalidOperationException("Invalid table expression");
            }
            
            string table = null;

            if (fromClause is FromClause fromClauseCast)
            {
                table = Wrap(fromClauseCast.Table);
            }
            else if (fromClause is RawFromClause rawFromClause) // made into else  since table gets over written
            {
                table = WrapIdentifiers(rawFromClause.Expression);
                context.Bindings.AddRange(rawFromClause.Bindings);
            }

            if (table is null)
            {
                throw new InvalidOperationException("Invalid table expression");
            }

            return table;
        }

        private SqlResult CompileDeleteQuery(Query query)
        {
            SqlResult ctx = IntializeSQL_Result(query,"delete");

            string table = MakeTable(ctx);

            string where = CompileWheres(ctx);

            if (!string.IsNullOrEmpty(where))
            {
                where = " " + where;
            }

            ctx.RawSql = $"DELETE FROM {table}{where}";

            return ctx;
        }
        private List<string> MakeUpdateParts(InsertClause toUpdate)
        {
            var parts = new List<string>();

            for (var i = 0; i < toUpdate.Columns.Count; i++)
            {
                parts.Add($"{Wrap(toUpdate.Columns[i])} = ?");
            }
            return parts;
        }
        private SqlResult CompileUpdateQuery(Query query)
        {
            var ctx = IntializeSQL_Result(query, "update");

            string table = MakeTable(ctx);

            InsertClause toUpdate = ctx.Query.GetOneComponent<InsertClause>("update", EngineCode);

            List<string> parts = MakeUpdateParts(toUpdate);

            ctx.Bindings.AddRange(toUpdate.Values);

            var where = CompileWheres(ctx);

            if (!string.IsNullOrEmpty(where))
            {
                where = " " + where;
            }

            var sets = string.Join(", ", parts);

            ctx.RawSql = $"UPDATE {table} SET {sets}{where}";

            return ctx;
        }

        private SqlResult IfInsertCase(SqlResult context, string table, InsertClause insertClause)
        {
            var columns = string.Join(", ", WrapArray(insertClause.Columns));
            var values = string.Join(", ", Parameterize(context, insertClause.Values));

            context.RawSql = $"INSERT INTO {table} ({columns}) VALUES ({values})";

            if (insertClause.ReturnId && !string.IsNullOrEmpty(LastId))
            {
                context.RawSql += ";" + LastId;
            }

            return context;
        }
        private SqlResult ElseInsertCase(SqlResult context, string table, List<AbstractInsertClause> inserts)
        {
            InsertQueryClause clause = inserts[0] as InsertQueryClause;

            string columns = "";

            if (clause.Columns.Any()) // if columns have anyting inside
            {
                columns = $" ({string.Join(", ", WrapArray(clause.Columns))}) ";
            }

            SqlResult subContext = CompileSelectQuery(clause.Query);
            context.Bindings.AddRange(subContext.Bindings);

            context.RawSql = $"INSERT INTO {table}{columns}{subContext.RawSql}";

            return context;
        }
        protected virtual SqlResult CompileInsertQuery(Query query)
        {
            SqlResult sqlResult = IntializeSQL_Result(query, " insert");

            string table = MakeTable(sqlResult);

            var inserts = sqlResult.Query.GetComponents<AbstractInsertClause>("insert", EngineCode);

            if (inserts[0] is InsertClause insertClause)
            {
                sqlResult = IfInsertCase(sqlResult, table, insertClause);
            }
            else
            {
                sqlResult = ElseInsertCase(sqlResult, table, inserts);
            }

            if (inserts.Count > 1)
            {
                foreach (var insert in inserts.GetRange(1, inserts.Count - 1))
                {
                    var clause = insert as InsertClause;

                    sqlResult.RawSql += ", (" + string.Join(", ", Parameterize(sqlResult, clause.Values)) + ")";

                }
            }


            return sqlResult;
        }

        protected virtual SqlResult CompileCteQuery(SqlResult ctx, Query query)
        {
            var cteFinder = new CteFinder(query, EngineCode);
            var cteSearchResult = cteFinder.Find();

            var rawSql = new StringBuilder("WITH ");
            var cteBindings = new List<object>();

            foreach (var cte in cteSearchResult)
            {
                var cteCtx = CompileCte(cte);

                cteBindings.AddRange(cteCtx.Bindings);
                rawSql.Append(cteCtx.RawSql.Trim());
                rawSql.Append(",\n");
            }

            rawSql.Length -= 2; // remove last comma
            rawSql.Append('\n');
            rawSql.Append(ctx.RawSql);

            ctx.Bindings.InsertRange(0, cteBindings);
            ctx.RawSql = rawSql.ToString();

            return ctx;
        }

        /// <summary>
        /// Compile a single column clause
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public virtual string CompileColumn(SqlResult ctx, AbstractColumn column)
        {
            if (column is RawColumn raw)
            {
                ctx.Bindings.AddRange(raw.Bindings);
                return WrapIdentifiers(raw.Expression);
            }

            if (column is QueryColumn queryColumn)
            {
                var alias = "";

                if (!string.IsNullOrWhiteSpace(queryColumn.Query.QueryAlias))
                {
                    alias = $" {ColumnAsKeyword}{WrapValue(queryColumn.Query.QueryAlias)}";
                }

                var subCtx = CompileSelectQuery(queryColumn.Query);

                ctx.Bindings.AddRange(subCtx.Bindings);

                return "(" + subCtx.RawSql + $"){alias}";
            }

            return Wrap((column as Column).Name);

        }


        public virtual SqlResult CompileCte(AbstractFrom cte)
        {
            var ctx = new SqlResult();

            if (null == cte)
            {
                return ctx;
            }

            if (cte is RawFromClause raw)
            {
                ctx.Bindings.AddRange(raw.Bindings);
                ctx.RawSql = $"{WrapValue(raw.Alias)} AS ({WrapIdentifiers(raw.Expression)})";
            }
            else if (cte is QueryFromClause queryFromClause)
            {
                var subCtx = CompileSelectQuery(queryFromClause.Query);
                ctx.Bindings.AddRange(subCtx.Bindings);

                ctx.RawSql = $"{WrapValue(queryFromClause.Alias)} AS ({subCtx.RawSql})";
            }

            return ctx;
        }

        protected virtual SqlResult OnBeforeSelect(SqlResult ctx)
        {
            return ctx;
        }

        protected virtual string CompileColumns(SqlResult context)
        {
            if (context.Query.HasComponent("aggregate", EngineCode))
            {
                AggregateClause aggregate = context.Query.GetOneComponent<AggregateClause>("aggregate", EngineCode);

                List<string> aggregateColumns = aggregate.Columns
                    .Select(x => CompileColumn(context, new Column { Name = x }))
                    .ToList();

                string sql = string.Empty;

                if (aggregateColumns.Count == 1)
                {
                    sql = string.Join(", ", aggregateColumns);

                    if (context.Query.IsDistinct)
                    {
                        sql = "DISTINCT " + sql;
                    }

                    return "SELECT " + aggregate.Type.ToUpperInvariant() + "(" + sql + $") {ColumnAsKeyword}" + WrapValue(aggregate.Type);
                }

                return "SELECT 1";
            }

            List<string> columns = context.Query
                .GetComponents<AbstractColumn>("select", EngineCode)
                .Select(x => CompileColumn(context, x))
                .ToList();

            string distinct = context.Query.IsDistinct ? "DISTINCT " : "";

            string select = columns.Any() ? string.Join(", ", columns) : "*";

            return $"SELECT {distinct}{select}";

        }

        public virtual string CompileUnion(SqlResult context)
        {

            // Handle UNION, EXCEPT and INTERSECT
            if (!context.Query.GetComponents("combine", EngineCode).Any())
            {
                return null;
            }

            List<string> combinedQueries = new List<string>();

            List<AbstractCombine> clauses = context.Query.GetComponents<AbstractCombine>("combine", EngineCode);

            foreach (AbstractCombine clause in clauses)
            {
                if (clause is Combine combineClause)
                {
                    string combineOperator = combineClause.Operation.ToUpperInvariant() + " " + (combineClause.All ? "ALL " : "");

                    SqlResult subCtx = CompileSelectQuery(combineClause.Query);

                    context.Bindings.AddRange(subCtx.Bindings);

                    combinedQueries.Add($"{combineOperator}{subCtx.RawSql}");
                }
                else
                {
                    RawCombine combineRawClause = clause as RawCombine;

                    context.Bindings.AddRange(combineRawClause.Bindings);

                    combinedQueries.Add(WrapIdentifiers(combineRawClause.Expression));

                }
            }

            return string.Join(" ", combinedQueries);

        }

        public virtual string CompileTableExpression(SqlResult context, AbstractFrom from)
        {
            if (from is RawFromClause raw)
            {
                context.Bindings.AddRange(raw.Bindings);
                return WrapIdentifiers(raw.Expression);
            }

            if (from is QueryFromClause queryFromClause)
            {
                Query fromQuery = queryFromClause.Query;

                string alias = string.IsNullOrEmpty(fromQuery.QueryAlias) ? "" : $" {TableAsKeyword}" + WrapValue(fromQuery.QueryAlias);

                SqlResult subCtx = CompileSelectQuery(fromQuery);

                context.Bindings.AddRange(subCtx.Bindings);

                return "(" + subCtx.RawSql + ")" + alias;
            }

            if (from is FromClause fromClause)
            {
                return Wrap(fromClause.Table);
            }

            throw InvalidClauseException("TableExpression", from);
        }

        public virtual string CompileFrom(SqlResult context)
        {
            if (!context.Query.HasComponent("from", EngineCode))
            {
                throw new InvalidOperationException("No table is set");
            }

            AbstractFrom from = context.Query.GetOneComponent<AbstractFrom>("from", EngineCode);

            return "FROM " + CompileTableExpression(context, from);
        }

        public virtual string CompileJoins(SqlResult context)
        {
            if (!context.Query.HasComponent("join", EngineCode))
            {
                return null;
            }

            IEnumerable<string> joins = context.Query
                .GetComponents<BaseJoin>("join", EngineCode)
                .Select(x => CompileJoin(context, x.Join));

            return "\n" + string.Join("\n", joins);
        }

        public virtual string CompileJoin(SqlResult context, Join join, bool isNested = false)
        {

            var from = join.GetOneComponent<AbstractFrom>("from", EngineCode);
            var conditions = join.GetComponents<AbstractCondition>("where", EngineCode);

            var joinTable = CompileTableExpression(context, from);
            var constraints = CompileConditions(context, conditions);

            var onClause = conditions.Any() ? $" ON {constraints}" : "";

            return $"{join.Type} {joinTable}{onClause}";
        }

        public virtual string CompileWheres(SqlResult context)
        {
            if (!context.Query.HasComponent("from", EngineCode) || !context.Query.HasComponent("where", EngineCode))
            {
                return null;
            }

            List<AbstractCondition> conditions = context.Query.GetComponents<AbstractCondition>("where", EngineCode);
            string sql = CompileConditions(context, conditions).Trim();

            return string.IsNullOrEmpty(sql) ? null : $"WHERE {sql}";
        }

        public virtual string CompileGroups(SqlResult context)
        {
            if (!context.Query.HasComponent("group", EngineCode))
            {
                return null;
            }

            IEnumerable<string> columns = context.Query
                .GetComponents<AbstractColumn>("group", EngineCode)
                .Select(x => CompileColumn(context, x));

            return "GROUP BY " + string.Join(", ", columns);
        }

        public virtual string CompileOrders(SqlResult context)
        {
            if (!context.Query.HasComponent("order", EngineCode))
            {
                return null;
            }

            var columns = context.Query
                .GetComponents<AbstractOrderBy>("order", EngineCode)
                .Select(x =>
            {

                if (x is RawOrderBy raw)
                {
                    context.Bindings.AddRange(raw.Bindings);
                    return WrapIdentifiers(raw.Expression);
                }

                var direction = (x as OrderBy).Ascending ? "" : " DESC";

                return Wrap((x as OrderBy).Column) + direction;
            });

            return "ORDER BY " + string.Join(", ", columns);
        }

        public virtual string CompileHaving(SqlResult ctx)
        {
            if (!ctx.Query.HasComponent("having", EngineCode))
            {
                return null;
            }

            var sql = new List<string>();
            string boolOperator;

            var having = ctx.Query.GetComponents("having", EngineCode)
                .Cast<AbstractCondition>()
                .ToList();

            for (var i = 0; i < having.Count; i++)
            {
                var compiled = CompileCondition(ctx, having[i]);

                if (!string.IsNullOrEmpty(compiled))
                {
                    boolOperator = i > 0 ? having[i].IsOr ? "OR " : "AND " : "";

                    sql.Add(boolOperator + compiled);
                }
            }

            return $"HAVING {string.Join(" ", sql)}";
        }

        public virtual string CompileLimit(SqlResult ctx)
        {
            var limit = ctx.Query.GetLimit(EngineCode);
            var offset = ctx.Query.GetOffset(EngineCode);

            if (limit == 0 && offset == 0)
            {
                return null;
            }

            if (offset == 0)
            {
                ctx.Bindings.Add(limit);
                return "LIMIT ?";
            }

            if (limit == 0)
            {
                ctx.Bindings.Add(offset);
                return "OFFSET ?";
            }

            ctx.Bindings.Add(limit);
            ctx.Bindings.Add(offset);

            return "LIMIT ? OFFSET ?";
        }

        /// <summary>
        /// Compile the random statement into SQL.
        /// </summary>
        /// <param name="seed"></param>
        /// <returns></returns>
        public virtual string CompileRandom(string seed)
        {
            return "RANDOM()";
        }

        public virtual string CompileLower(string value)
        {
            return $"LOWER({value})";
        }

        public virtual string CompileUpper(string value)
        {
            return $"UPPER({value})";
        }

        public virtual string CompileTrue()
        {
            return "true";
        }

        public virtual string CompileFalse()
        {
            return "false";
        }

        private InvalidCastException InvalidClauseException(string section, AbstractClause clause)
        {
            return new InvalidCastException($"Invalid type \"{clause.GetType().Name}\" provided for the \"{section}\" clause.");
        }

        protected string checkOperator(string op)
        {
            op = op.ToLowerInvariant();

            var valid = operators.Contains(op) || userOperators.Contains(op);

            if (!valid)
            {
                throw new InvalidOperationException($"The operator '{op}' cannot be used. Please consider white listing it before using it.");
            }

            return op;
        }

        /// <summary>
        /// Wrap a single string in a column identifier.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual string Wrap(string value)
        {

            if (value.ToLowerInvariant().Contains(" as "))
            {
                var index = value.ToLowerInvariant().IndexOf(" as ");
                var before = value.Substring(0, index);
                var after = value.Substring(index + 4);

                return Wrap(before) + $" {ColumnAsKeyword}" + WrapValue(after);
            }

            if (value.Contains("."))
            {
                return string.Join(".", value.Split('.').Select((x, index) =>
                {
                    return WrapValue(x);
                }));
            }

            // If we reach here then the value does not contain an "AS" alias
            // nor dot "." expression, so wrap it as regular value.
            return WrapValue(value);
        }

        /// <summary>
        /// Wrap a single string in keyword identifiers.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual string WrapValue(string value)
        {
            if (value == "*") return value;

            var opening = this.OpeningIdentifier;
            var closing = this.ClosingIdentifier;

            return opening + value.Replace(closing, closing + closing) + closing;
        }

        /// <summary>
        /// Resolve a parameter
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="parameter"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual object Resolve(SqlResult ctx, object parameter)
        {
            // if we face a literal value we have to return it directly
            if (parameter is UnsafeLiteral literal)
            {
                return literal.Value;
            }

            // if we face a variable we have to lookup the variable from the predefined variables
            if (parameter is Variable variable)
            {
                var value = ctx.Query.FindVariable(variable.Name);
                return value;
            }

            return parameter;

        }

        /// <summary>
        /// Resolve a parameter and add it to the binding list
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="parameter"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual string Parameter(SqlResult ctx, object parameter)
        {
            // if we face a literal value we have to return it directly
            if (parameter is UnsafeLiteral literal)
            {
                return literal.Value;
            }

            // if we face a variable we have to lookup the variable from the predefined variables
            if (parameter is Variable variable)
            {
                var value = ctx.Query.FindVariable(variable.Name);
                ctx.Bindings.Add(value);
                return "?";
            }

            ctx.Bindings.Add(parameter);
            return "?";
        }

        /// <summary>
        /// Create query parameter place-holders for an array.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public virtual string Parameterize<T>(SqlResult ctx, IEnumerable<T> values)
        {
            return string.Join(", ", values.Select(x => Parameter(ctx, x)));
        }

        /// <summary>
        /// Wrap an array of values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public virtual List<string> WrapArray(List<string> values)
        {
            return values.Select(x => Wrap(x)).ToList();
        }
        /// <summary>
        /// Replaces escape charactors( // ) with {}[]
        /// 
        /// this method expects 4 esc charactors
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public virtual string WrapIdentifiers(string input)
        {
            return input

                // deprecated
                .ReplaceIdentifierUnlessEscaped(this.EscapeCharacter, "{", this.OpeningIdentifier)
                .ReplaceIdentifierUnlessEscaped(this.EscapeCharacter, "}", this.ClosingIdentifier)

                .ReplaceIdentifierUnlessEscaped(this.EscapeCharacter, "[", this.OpeningIdentifier)
                .ReplaceIdentifierUnlessEscaped(this.EscapeCharacter, "]", this.ClosingIdentifier);
        }
    }
}
