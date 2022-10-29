namespace Sorter;
public class Sorter
{
    private static Sorter instance;

    private Sorter()
    { }

    public static Sorter getInstance()
    {
        if (instance == null)
            instance = new Sorter();
        return instance;
    }
}

