namespace BackupCLI.Collections;

public class FixedQueue<T>(int capacity) : Queue<T>
{
    public int Capacity { get; } = capacity;
    public T? Last { get; private set; }

    public new void Enqueue(T item)
    {
        base.Enqueue(item);
        Last = item;

        while (Count > Capacity)
        {
            T removed = Dequeue();
            if (removed is IDisposable disposable) disposable.Dispose();
        }
    }
}