using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Dapper;

namespace IdentityServer.Dapper.xUnitTest
{
    public class xTestPostgreSQL
    {
        [Fact]
        public void TestGetPageQuerySQL()
        {
            var options = xTestBase.GetDBProviderOptions(xTestBase.PostgreSQL);
            int pageIndex = 1;
            int pageSize = 10;
            int totalCount = 499;
            string orderby = "order by id";
            DynamicParameters dynamicParameters = new DynamicParameters();

            string pagedsql = options.GetPageQuerySQL("select * from ApiResources where 1 = 1", pageIndex, pageSize, totalCount, orderby, dynamicParameters);

            Assert.False(string.IsNullOrEmpty(pagedsql));
            Assert.Equal("select * from ApiResources where 1 = 1 order by id limit @size offset @start", pagedsql);
        }
    }
}
