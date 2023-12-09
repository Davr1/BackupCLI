namespace BackupCLI.Collections;

public class FixedQueue<T>(int size) : Queue<T>
{
    public int Size { get; } = size;
    public T? Last { get; private set; }

    public new void Enqueue(T item)
    {
        base.Enqueue(item);
        Last = item;

        while (Count > Size)
        {
            T removed = Dequeue();
            if (removed is IDisposable disposable) disposable.Dispose();
        }
    }
}