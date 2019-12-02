using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlKata.Execution.Tests
{
    [TestClass()]
    public class QueryFactoryHelperTests
    {
        [TestMethod()]
        public void GetLocalIDsTest()
        {
            Include inc = new Include();
            inc.LocalKey = "key";
            string obj = "im the object";
            List<Dictionary<string, object>> dynamicResult = new List<Dictionary<string, object>>();
            dynamicResult.Add(new Dictionary<string, object>());
            dynamicResult[0].Add(inc.LocalKey, obj);
            List<object> result = QueryFactoryHelper.GetLocalIDs(dynamicResult, inc);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            string resultVal = result[0] as string;
            Assert.AreEqual(obj, resultVal);
        }
    }
}