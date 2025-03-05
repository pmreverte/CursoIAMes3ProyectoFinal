using System.Collections.Generic;

namespace Sprint2.Models
{
    /// <summary>
    /// ViewModel for representing a list of tasks with filtering and pagination.
    /// </summary>
    public class TaskListViewModel
    {
        /// <summary>
        /// Gets or sets the collection of tasks.
        /// </summary>
        public IEnumerable<TodoTask> Tasks { get; set; } = new List<TodoTask>();

        /// <summary>
        /// Gets or sets the filter criteria for tasks.
        /// </summary>
        public TaskFilter Filter { get; set; } = new();

        /// <summary>
        /// Gets or sets the pagination information for the task list.
        /// </summary>
        public PaginationInfo Pagination { get; set; } = new();
    }

    /// <summary>
    /// Represents the filter criteria for tasks.
    /// </summary>
    public class TaskFilter
    {
        /// <summary>
        /// Gets or sets the search term for filtering tasks.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Gets or sets the status filter for tasks.
        /// </summary>
        public TodoTaskStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the priority filter for tasks.
        /// </summary>
        public Priority? Priority { get; set; }

        /// <summary>
        /// Gets or sets the category filter for tasks.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to filter overdue tasks.
        /// </summary>
        public bool? IsOverdue { get; set; }
        
        /// <summary>
        /// Returns a string representation of the filter for logging purposes.
        /// </summary>
        /// <returns>A string representation of the filter.</returns>
        public override string ToString()
        {
            return $"TaskFilter[SearchTerm='{SearchTerm ?? "null"}', Status={Status?.ToString() ?? "null"}, " +
                   $"Priority={Priority?.ToString() ?? "null"}, Category='{Category ?? "null"}', " +
                   $"IsOverdue={IsOverdue?.ToString() ?? "null"}]";
        }
    }

    /// <summary>
    /// Represents pagination information for a list of items.
    /// </summary>
    public class PaginationInfo
    {
        /// <summary>
        /// Gets or sets the current page number.
        /// </summary>
        public int CurrentPage { get; set; } = 1;

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the total number of items.
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int TotalPages => (TotalItems + PageSize - 1) / PageSize;

        /// <summary>
        /// Gets a value indicating whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// Gets a value indicating whether there is a next page.
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
