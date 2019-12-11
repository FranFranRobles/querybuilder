using SqlKata.Compilers;
using SqlKata.Tests.Infrastructure;
using Xunit;

namespace SqlKata.Tests.PostgreSql
{
    public class PostgreSqlArrayCompileTests : TestSupport
    {
        private readonly PostgresCompiler compiler;

        public PostgreSqlArrayCompileTests()
        {
            compiler = Compilers.Get<PostgresCompiler>(EngineCodes.PostgreSql);
        }

        [Fact]
        public void CompileArrayInsert()
        {
            // Using the same insert statement that the  original issue author expected it to be
            string expected = "INSERT INTO 'entities'('id', 'name', 'ids') VALUES{'1', 'some', {'1', '2','3'} }";

            // Build the query using the same AsInsert statement as the original author did
            Query query = new Query("entities").AsInsert(new
            {
                id = "1",
                name = "some",
                ids = new[] { "1", "2", "3" }
            });
            var books = db.Query("Books").WhereTrue("IsPublished").Get();
            // Compile our query into raw sql
            SqlResult context = compiler.Compile(query);

            // get the actual rawSQL from the context
            string actual = context.RawSql;

            // Let's see if they are the same
            Assert.Equal(expected, actual);
        }
     }
}