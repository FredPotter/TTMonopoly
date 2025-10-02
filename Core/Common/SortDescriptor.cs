namespace Core.Common;

using System.Linq.Expressions;

public enum SortDirection
{
    Ascending,
    Descending
}

public class SortDescriptor<T>
{
    public Expression<Func<T, object>> KeySelector { get; }
    public SortDirection Direction { get; }

    public SortDescriptor(Expression<Func<T, object>> keySelector, SortDirection direction)
    {
        KeySelector = keySelector;
        Direction = direction;
    }
}
