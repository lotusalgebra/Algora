namespace Algora.Application.DTOs.Common
{
    /// <summary>
    /// Standard request for DataTables server-side processing.
    /// </summary>
    public class DataTableRequest
    {
        /// <summary>
        /// Draw counter for DataTables (used for request/response matching).
        /// </summary>
        public int Draw { get; set; }

        /// <summary>
        /// Number of records to skip (offset).
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Number of records to return (page size).
        /// </summary>
        public int Length { get; set; } = 10;

        /// <summary>
        /// Search value for filtering.
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Column index for sorting.
        /// </summary>
        public int SortColumn { get; set; }

        /// <summary>
        /// Sort direction: "asc" or "desc".
        /// </summary>
        public string SortDirection { get; set; } = "asc";

        /// <summary>
        /// Current page number (1-based, calculated from Start/Length).
        /// </summary>
        public int Page => (Start / Math.Max(Length, 1)) + 1;
    }

    /// <summary>
    /// Standard response for DataTables server-side processing.
    /// </summary>
    /// <typeparam name="T">Type of data items.</typeparam>
    public class DataTableResponse<T>
    {
        /// <summary>
        /// Draw counter echoed back for request/response matching.
        /// </summary>
        public int Draw { get; set; }

        /// <summary>
        /// Total records in the dataset (before filtering).
        /// </summary>
        public int RecordsTotal { get; set; }

        /// <summary>
        /// Total records after filtering.
        /// </summary>
        public int RecordsFiltered { get; set; }

        /// <summary>
        /// The data for the current page.
        /// </summary>
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();

        /// <summary>
        /// Optional error message.
        /// </summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// Simple paginated list response.
    /// </summary>
    /// <typeparam name="T">Type of items.</typeparam>
    public class PaginatedList<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
