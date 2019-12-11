using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlKata
{
    public partial class Query
    {

        public Query Set(params string[] columns)
        {
            Method = "set";

            columns = columns
                .Select(x => Helper.ExpandExpression(x))
                .SelectMany(x => x)
                .ToArray();


            foreach (string column in columns)
            {
                AddComponent("set", new Column
                {
                    Name = column
                });
            }

            return this;
        }

        public Query AsSet(IEnumerable<string> columns, IEnumerable<object> values)
        {

            if ((columns?.Count() ?? 0) == 0 || (values?.Count() ?? 0) == 0)
            {
                throw new InvalidOperationException("Columns and Values cannot be null or empty");
            }

            if (columns.Count() != values.Count())
            {
                throw new InvalidOperationException("Columns count should be equal to Values count");
            }

            Method = "set";

            ClearComponent("set").AddComponent("set", new InsertClause
            {
                Columns = columns.ToList(),
                Values = values.ToList()
            });

            return this;
        }

        public Query AsSet(IReadOnlyDictionary<string, object> data)
        {

            if (data == null || data.Count == 0)
            {
                throw new InvalidOperationException("Values dictionary cannot be null or empty");
            }

            Method = "set";

            ClearComponent("set").AddComponent("set", new InsertClause
            {
                Columns = data.Keys.ToList(),
                Values = data.Values.ToList(),
            });

            return this;
        }

    }