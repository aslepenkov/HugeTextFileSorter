namespace SorterCore;

public static class LineSorter
{
    ///Merge Sort
    public static string[] Sort(string[]? lines, int left, int right)
    {
        if (left < right)
        {
            int middle = left + (right - left) / 2;

            Sort(lines, left, middle);
            Sort(lines, middle + 1, right);

            MergeLines(lines, left, middle, right);
        }
        return lines;
    }

    private static void MergeLines(string[] array, int left, int middle, int right)
    {
        var cmp = new LineComparer();
        var leftArrayLength = middle - left + 1;
        var rightArrayLength = right - middle;
        var leftTempArray = new string[leftArrayLength];
        var rightTempArray = new string[rightArrayLength];
        int i, j;

        for (i = 0; i < leftArrayLength; ++i)
            leftTempArray[i] = array[left + i];
        for (j = 0; j < rightArrayLength; ++j)
            rightTempArray[j] = array[middle + 1 + j];
        i = j = 0;

        int k = left;
        while (i < leftArrayLength && j < rightArrayLength)
        {
            if (cmp.Compare(leftTempArray[i], rightTempArray[j]) <= 0)
            {
                array[k++] = leftTempArray[i++];
            }
            else
            {
                array[k++] = rightTempArray[j++];
            }
        }
        while (i < leftArrayLength)
        {
            array[k++] = leftTempArray[i++];
        }
        while (j < rightArrayLength)
        {
            array[k++] = rightTempArray[j++];
        }
    }
}