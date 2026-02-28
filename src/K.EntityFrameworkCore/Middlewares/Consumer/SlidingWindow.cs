namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// A circular buffer that tracks the last <see cref="Size"/> outcomes (true = failure).
/// Thread-safe via lock.
/// </summary>
internal sealed class SlidingWindow
{
    private readonly bool[] _buffer;
    private readonly object _lock = new();
    private int _position;
    private int _failureCount;
    private int _recorded;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlidingWindow"/> class with the specified size.
    /// </summary>
    /// <param name="size">The number of outcomes to track.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> is less than 1.</exception>
    public SlidingWindow(int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        _buffer = new bool[size];
        Size = size;
    }

    /// <summary>
    /// Gets the number of failures currently recorded in the window.
    /// </summary>
    public int FailureCount
    {
        get
        {
            lock (_lock)
            {
                return _failureCount;
            }
        }
    }

    /// <summary>
    /// Gets the size of the sliding window.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Records an outcome in the sliding window.
    /// </summary>
    /// <param name="isFailure">True if the outcome is a failure; false otherwise.</param>
    public void Record(bool isFailure)
    {
        lock (_lock)
        {
            // If we've filled the buffer, subtract the value being overwritten
            if (_recorded >= Size && _buffer[_position])
            {
                _failureCount--;
            }

            _buffer[_position] = isFailure;

            if (isFailure)
            {
                _failureCount++;
            }

            _position = (_position + 1) % Size;

            if (_recorded < Size)
            {
                _recorded++;
            }
        }
    }

    /// <summary>
    /// Resets the sliding window, clearing all recorded outcomes.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            Array.Clear(_buffer);
            _position = 0;
            _failureCount = 0;
            _recorded = 0;
        }
    }
}
