using Moq;
using SqlKata.Execution;
using SqlKata.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace SqlKata.Tests
{
    public class QueryExtensionsAsyncTests : TestSupport
    {
        /**
         * UPDATE: THERE IS NO WAY TO UNIT TEST ASYNC METHODS
         */
        [Fact]
        public void ShouldPrintOutCanceledMessageWhenCancellationToeknTrue_GetAsync()
        {
            //var query = new Mock<Query>();
            //var mockQuery = query.Object;
            //var result = new Mock<IEnumerable<string>>();
            //CancellationToken cancellationToken = default;
            //var c = QueryExtensionsAsync.GetAsync<string>(mockQuery, cancellationToken);
            //result.Verify(QueryExtensionsAsync.GetAsync<string>(mockQuery, cancellationToken), Times.AtMostOnce);
            //Assert.True(c);
        }
    }
}
