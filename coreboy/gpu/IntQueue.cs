namespace coreboy.gpu;

public class IntQueue(int capacity)
{
	private Queue<int> _inner = new(capacity);

	public int Size()
	{
		return _inner.Count;
	}

	public void Enqueue(int value)
	{
		_inner.Enqueue(value);
	}

	public int Dequeue()
	{
		return _inner.Dequeue();
	}

	public int Get(int index)
	{
		return _inner.ToArray()[index];
	}

	public void Clear()
	{
		_inner.Clear();
	}

	public void Set(int index, int value)
	{
		lock (_inner)
		{
			int[] asArray = [.. _inner];
			asArray[index] = value;
			_inner = new Queue<int>(asArray);
		}
	}
}
