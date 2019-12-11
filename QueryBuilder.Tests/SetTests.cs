using System;
using System.Collections.Generic;
using System.Linq;
using SqlKata.Compilers;
using SqlKata.Tests.Infrastructure;
using Xunit;

namespace SqlKata.Tests
{
    public class SetTests : TestSupport
    {
        [Fact]
        public void UpdateObject()
        {
            IEnumerable<string> cols = new[] { "src", "dest" };
            IEnumerable<string> values = new[] { "Date", "Date" };

            Query query = new Query("Table").AsSet(cols,values);

            IReadOnlyDictionary<string, string> c = Compile(query);

            Assert.Equal("UPDATE [Table] " +
                "SET books.[Date]=Movie.[Date] " +
                "From [sourceBulkTable] src  " +
                "INNER JOIN [destinationTable] dest " +
                " ON src.[ID] = dest.[ID]",  c[EngineCodes.SqlServer]);


            Assert.Equal("UPDATE \"TABLE\" SET \"NAME\" = 'The User', \"AGE\" = '2018-01-01'", c[EngineCodes.Firebird]);
        }


    }
}