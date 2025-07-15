namespace GenAI.Bridge.Utils;

internal readonly struct ValueStopwatch
{
    private readonly long _start;
    private ValueStopwatch(long s) => _start = s;

    public static ValueStopwatch StartNew()
        => new(System.Diagnostics.Stopwatch.GetTimestamp());

    public TimeSpan GetElapsedTime()
        => TimeSpan.FromSeconds(
            (System.Diagnostics.Stopwatch.GetTimestamp() - _start) / (double)System.Diagnostics.Stopwatch.Frequency);
}