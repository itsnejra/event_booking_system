namespace EventBooking.Application.Catalog;

public enum EventSortOrder
{
    StartDate = 0,
    Title = 1,
    CheapestFirst = 2,

    /// <summary>Most tickets sold first.</summary>
    Popularity = 3,
}
