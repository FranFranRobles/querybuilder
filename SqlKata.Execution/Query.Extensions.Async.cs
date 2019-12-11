using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SqlKata.Execution
{
    public static class QueryExtensionsAsync
    {
        public static async Task<IEnumerable<T>> GetAsync<T>(this Query query, CancellationToken cancellationToken = default)
        {
            return await QueryHelper.CreateQueryFactory(query).GetAsync<T>(query, cancellationToken);
        }

        public static async Task<IEnumerable<dynamic>> GetAsync(this Query query, CancellationToken cancellationToken = default)
        {
            return await GetAsync<dynamic>(query, cancellationToken);
        }

        public static async Task<T> FirstOrDefaultAsync<T>(this Query query, CancellationToken cancellationToken = default)
        {
            return await QueryHelper.CreateQueryFactory(query).FirstOrDefaultAsync<T>(query, cancellationToken);
        }

        public static async Task<dynamic> FirstOrDefaultAsync(this Query query, CancellationToken cancellationToken = default)
        {
            return await FirstOrDefaultAsync<dynamic>(query, cancellationToken);
        }

        public static async Task<T> FirstAsync<T>(this Query query, CancellationToken cancellationToken = default)
        {
            return await QueryHelper.CreateQueryFactory(query).FirstAsync<T>(query, cancellationToken);
        }

        public static async Task<dynamic> FirstAsync(this Query query, CancellationToken cancellationToken = default)
        {
            return await FirstAsync<dynamic>(query, cancellationToken);
        }

        public static async Task<PaginationResult<T>> PaginateAsync<T>(this Query query, int page, int perPage = 25, CancellationToken cancellationToken = default)
        {
            QueryFactory db = QueryHelper.CreateQueryFactory(query);

            return await db.PaginateAsync<T>(query, page, cancellationToken, perPage);
        }

        public static async Task<PaginationResult<dynamic>> PaginateAsync(this Query query, int page, int perPage = 25, CancellationToken cancellationToken = default)
        {
            return await PaginateAsync<dynamic>(query, page, perPage, cancellationToken);
        }

        public static async Task ChunkAsync<T>(this Query query, int chunkSize, Func<IEnumerable<T>, int, bool> func, CancellationToken cancellationToken = default)
        {
            await QueryHelper.CreateQueryFactory(query).ChunkAsync<T>(query, chunkSize, func, cancellationToken);
        }

        public static async Task ChunkAsync<T>(this Query query, int chunkSize, Action<IEnumerable<T>, int> action, CancellationToken cancellationToken = default)
        {
            await QueryHelper.CreateQueryFactory(query).ChunkAsync<T>(query, chunkSize, action, cancellationToken);
        }

        public static async Task ChunkAsync(this Query query, int chunkSize, Func<IEnumerable<dynamic>, int, bool> func, CancellationToken cancellationToken = default)
        {
            await ChunkAsync<dynamic>(query, chunkSize, func, cancellationToken);
        }


        public static async Task ChunkAsync(this Query query, int chunkSize, Action<IEnumerable<dynamic>, int> action, CancellationToken cancellationToken = default)
        {
            await ChunkAsync<dynamic>(query, chunkSize, action, cancellationToken);
        }

        public static async Task<int> InsertAsync(
            this Query query,
            IReadOnlyDictionary<string, object> values,
            CancellationToken cancellationToken = default
        )
        {
            return await QueryHelper.CreateQueryFactory(query)
                .ExecuteAsync(query.AsInsert(values), cancellationToken: cancellationToken);
        }

        public static async Task<int> InsertAsync(this Query query, object data, CancellationToken cancellationToken = default)
        {
            return await QueryHelper.CreateQueryFactory(query)
                .ExecuteAsync(query.AsInsert(data), cancellationToken: cancellationToken);
        }

        public static async Task<T> InsertGetIdAsync<T>(this Query query, object data, CancellationToken cancellationToken = default)
        {
            InsertGetIdRow<T> row = await QueryHelper.CreateQueryFactory(query)
                .FirstAsync<InsertGetIdRow<T>>(query.AsInsert(data, true), cancellationToken);

            return row.Id;
        }

        public static async Task<int> InsertAsync(
            this Query query,
            IEnumerable<string> columns,
            Query fromQuery,
            CancellationToken cancellationToken = default
        )
        {
            return await QueryHelper.CreateQueryFactory(query)
                .ExecuteAsync(query.AsInsert(columns, fromQuery), cancellationToken: cancellationToken);
        }

        public static async Task<int> UpdateAsync(this Query query, IReadOnlyDictionary<string, object> values, CancellationToken cancellationToken = default)
        {
            return await QueryHelper.CreateQueryFactory(query)
                .ExecuteAsync(query.AsUpdate(values), cancellationToken: cancellationToken);
        }

        public static async Task<int> UpdateAsync(this Query query, object data, CancellationToken cancellationToken = default)
        {
            return await QueryHelper.CreateQueryFactory(query)
                .ExecuteAsync(query.AsUpdate(data), cancellationToken: cancellationToken);
        }

        public static async Task<int> DeleteAsync(this Query query)
        {
            return await QueryHelper.CreateQueryFactory(query)
                .ExecuteAsync(query.AsDelete(), default);
        }

    }
}