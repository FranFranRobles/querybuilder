using SqlKata.Compilers;
using SqlKata.Tests.Infrastructure;
using Xunit;

namespace SqlKata.Tests.SqlServer
{
    public class SqlServerLimitTests : TestSupport
    {
        private readonly SqlServerCompiler compiler;

        public SqlServerLimitTests()
        {
            compiler = Compilers.Get<SqlServerCompiler>(EngineCodes.SqlServer);
            compiler.UseLegacyPagination = false;
        }

        [Fact]
        public void NoLimitNorOffset()
        {
            Query query = new Query("Table");
            SqlResult context = new SqlResult {Query = query};

            Assert.Null(compiler.CompileLimit(context));
        }

        [Fact]
        public void LimitOnly()
        {
            Query query = new Query("Table").Limit(10);
            SqlResult context = new SqlResult {Query = query};

            Assert.EndsWith("OFFSET ? ROWS FETCH NEXT ? ROWS ONLY", compiler.CompileLimit(context));
            Assert.Equal(2, context.Bindings.Count);
            Assert.Equal(0, context.Bindings[0]);
            Assert.Equal(10, context.Bindings[1]);
        }
        [Fact]
        public void LongLimitOnly()
        {
            long limit = 10;
            Query query = new Query("Table").Limit(limit);
            SqlResult context = new SqlResult { Query = query };

            Assert.EndsWith("OFFSET ? ROWS FETCH NEXT ? ROWS ONLY", compiler.CompileLimit(context));
            Assert.Equal(2, context.Bindings.Count);
            Assert.Equal(0, context.Bindings[0]);
            Assert.Equal(limit, context.Bindings[1]);
        }

        [Fact]
        public void OffsetOnly()
        {
            Query query = new Query("Table").Offset(20);
            SqlResult context = new SqlResult {Query = query};

            Assert.EndsWith("OFFSET ? ROWS", compiler.CompileLimit(context));

            Assert.Single(context.Bindings);
            Assert.Equal(20, context.Bindings[0]);
        }

        [Fact]
        public void LimitAndOffset()
        {
            Query query = new Query("Table").Limit(5).Offset(20);
            SqlResult context = new SqlResult {Query = query};

            Assert.EndsWith("OFFSET ? ROWS FETCH NEXT ? ROWS ONLY", compiler.CompileLimit(context));

            Assert.Equal(2, context.Bindings.Count);
            Assert.Equal(20, context.Bindings[0]);
            Assert.Equal(5, context.Bindings[1]);
        }
        [Fact]
        public void LongLimitAndOffset()
        {
            long limit = 5;
            Query query = new Query("Table").Limit(limit).Offset(20);
            SqlResult context = new SqlResult { Query = query };

            Assert.EndsWith("OFFSET ? ROWS FETCH NEXT ? ROWS ONLY", compiler.CompileLimit(context));

            Assert.Equal(2, context.Bindings.Count);
            Assert.Equal(20, context.Bindings[0]);
            Assert.Equal(limit, context.Bindings[1]);
        }

        [Fact]
        public void ShouldEmulateOrderByIfNoOrderByProvided()
        {
            Query query = new Query("Table").Limit(5).Offset(20);

            Assert.Contains("ORDER BY (SELECT 0)", compiler.Compile(query).ToString());
        }

        [Fact]
        public void ShouldKeepTheOrdersAsIsIfNoPaginationProvided()
        {
            Query query = new Query("Table").OrderBy("Id");

            Assert.Contains("ORDER BY [Id]", compiler.Compile(query).ToString());
        }

        [Fact]
        public void ShouldKeepTheOrdersAsIsIfPaginationProvided()
        {
            Query query = new Query("Table").Offset(10).Limit(20).OrderBy("Id");

            Assert.Contains("ORDER BY [Id]", compiler.Compile(query).ToString());
            Assert.DoesNotContain("(SELECT 0)", compiler.Compile(query).ToString());
        }
    }
}