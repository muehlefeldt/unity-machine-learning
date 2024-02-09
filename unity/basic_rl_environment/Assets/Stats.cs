
[System.Serializable]
public class Stats
{
    public ulong sameRoom;
    public ulong episodeCount;
    public int sensorCount;
    public int runId;

    public Stats()
    {
        sameRoom = 0;
        episodeCount = 0;
        sensorCount = 0;
    }
}

