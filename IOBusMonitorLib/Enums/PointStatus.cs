namespace IOBusMonitorLib
{
    /// <summary>
    /// Communication state for a live point/device path.
    /// </summary>
    public enum PointStatus
    {
        Unknown = 0,
        Connecting = 1,
        Online = 2,
        Timeout = 3,
        ReadError = 4,
        Disabled = 5,
        Offline = 6
    }
}
