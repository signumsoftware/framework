
namespace Signum.Utilities.DataStructures;

public class PriorityQueue<T>
{
    List<T> list = new List<T>();
    Comparison<T> comparer;

    public PriorityQueue()
        : this(Comparer<T>.Default.Compare)
    {
    }

    public PriorityQueue(IComparer<T> comparer)
    {
        this.comparer = comparer.Compare;
    }

    public PriorityQueue(Comparison<T> comparer)
    {
         this.comparer = comparer;
    }


    public int Count
    {
        get
        {
            return list.Count;
        }
    }

    public bool Empty
    {
        get { return list.Count == 0; }
    }


    public int Push(T element)
    {
        int p = list.Count;
        list.Add(element);
        do
        {
            if (p == 0)
                break;
            int p2 = (p - 1) / 2;
            if (Compare(p, p2) < 0)
            {
                SwitchElements(p, p2);
                p = p2;
            }
            else
                break;
        } while (true);
        return p;
    }

    public void PushAll(IEnumerable<T> elements)
    {
        foreach (var item in elements)
            Push(item);
    }

    public T Pop()
    {
        if (Empty)
            throw new InvalidOperationException("Empty PriorityQueue");

        int p = 0;
        T result = list[0];
        list[0] = list[list.Count - 1];
        list.RemoveAt(list.Count - 1);
        do
        {
            int pn = p;
            int p1 = 2 * p + 1;
            int p2 = 2 * p + 2;
            if (list.Count > p1 && Compare(p, p1) > 0)
                p = p1;
            if (list.Count > p2 && Compare(p, p2) > 0)
                p = p2;

            if (p == pn)
                break;

            SwitchElements(p, pn);
        } while (true);

        return result;
    }

    public T Peek()
    {
        if (Empty)
            throw new InvalidOperationException("Empty PriorityQueue");

        return list[0];
    }



    public void Clear()
    {
        list.Clear();
    }

    public void Update(T element)
    {
        Update(list.IndexOf(element));
    }

    public bool Contains(T element)
    {
        return list.Contains(element);
    }


    int Compare(int i, int j)
    {
        return comparer(list[i], list[j]);
    }

    void SwitchElements(int i, int j)
    {
        T h = list[i];
        list[i] = list[j];
        list[j] = h;
    }

    void Update(int i)
    {
        int p = i, pn;
        int p1, p2;
        do
        {
            if (p == 0)
                break;
            p2 = (p - 1) / 2;
            if (Compare(p, p2) < 0)
            {
                SwitchElements(p, p2);
                p = p2;
            }
            else
                break;
        } while (true);
        if (p < i)
            return;
        do
        {
            pn = p;
            p1 = 2 * p + 1;
            p2 = 2 * p + 2;
            if (list.Count > p1 && Compare(p, p1) > 0)
                p = p1;
            if (list.Count > p2 && Compare(p, p2) > 0)
                p = p2;

            if (p == pn)
                break;
            SwitchElements(p, pn);
        } while (true);
    }

    public List<T> GetOrderedList()
    {
        var result = new List<T>(list);

        result.Sort(comparer);

        return result;
    }
}
