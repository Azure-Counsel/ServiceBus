namespace HeartbeatLockRenewalDemo;

public class HeartbeatTracker
{
    private DateTime _lastBeatUtc;

    public HeartbeatTracker()
    {
        Beat();
    }

    public void Beat()
    {
        _lastBeatUtc = DateTime.UtcNow;
    }

    public DateTime LastBeatUtc
    {
        get
        {
            return _lastBeatUtc;
        }
    }

    public TimeSpan Age =>
        DateTime.UtcNow - _lastBeatUtc;
}