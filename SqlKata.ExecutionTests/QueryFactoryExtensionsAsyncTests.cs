using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SqlKata.Execution.Tests
{
    [TestClass()]
    public class QueryFactoryExtensionsAsyncTests
    {
        [TestMethod()]
        public void GetAsyncTest()
        {
            Query q = new Query();
            q.Select("*").From("table");
            QueryFactory factory = new QueryFactory();
            factory.Compiler = new Compilers.PostgresCompiler();
            Task<IEnumerable<dynamic>> result = QueryFactoryExtensionsAsync.GetAsync(factory, q);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsCompleted);
        }
    }
}