namespace HomeBuddy_API.Extensions
{
    public static class QueryableExtensions
    {
        private const int MaxLimit = 20;

        /// <summary>
        /// Limits the query to a maximum number of items.
        /// </summary>
        public static IQueryable<T> Limit<T>(this IQueryable<T> query, int limit = MaxLimit)
        {
            return query.Take(Math.Min(limit, MaxLimit));
        }

        /// <summary>
        /// Optionally supports Skip + Take for pagination.
        /// </summary>
        public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int page = 1, int pageSize = MaxLimit)
        {
            pageSize = Math.Min(pageSize, MaxLimit);
            page = Math.Max(page, 1);

            return query.Skip((page - 1) * pageSize)
                        .Take(pageSize);
        }
    }
}
