namespace SorterCore;

public interface ILineComparer : IComparer<string> {}

public class LineComparer : ILineComparer
{
    public int Compare(string? left, string? right)
    {
        if (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right))
            return 0;

        if (string.IsNullOrEmpty(right))
            return -1;

        if (string.IsNullOrEmpty(left))
            return 1;

        var pL = left?.Split(".");//parts left
        var pR = right?.Split(".");//parts right

        //Rule #1 Compare String part
        var compareStrs = string.Compare(pL[1], pR[1], OrdinalIgnoreCase);
        if (compareStrs == 0)
        {
            //Rule #2 Compare digits part, when strings are equal
            var compareDigits = string.Compare(pL[0], pR[0], OrdinalIgnoreCase);
            return compareDigits > 0 ? 1 : compareDigits == 0 ? 0 : -1;
        }
        else
        {
            return compareStrs > 0 ? 1 : -1;
        }
    }
}