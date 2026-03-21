using LibVLCSharp.Shared;

namespace RTSP_Cams.Settings;

public sealed class ConcretVlcSettings
{
    public bool RtspTcp { get; set; }
    public uint NetworkCaching { get; set; }
    public uint LiveCaching { get; set; }
    public bool Audio { get; set; }
    public bool DropLateFrames { get; set; }
    public bool SkipFrames { get; set; }
    public uint ClockJitter { get; set; } = 0;
    public uint ClockSynchro { get; set; } = 0;
    public int ReconnectTimeout { get; set; }

    public ConcretVlcSettings(
        bool rtspTcp,
        uint networkCaching,
        uint liveCaching,
        bool audio,
        bool dropLateFrames,
        bool skipFrames,
        int reconnectTimeout,
        uint clockJitter = 0,
        uint clockSynchro = 0
        )
    {
        RtspTcp = rtspTcp;
        NetworkCaching = networkCaching;
        LiveCaching = liveCaching;
        Audio = audio;
        DropLateFrames = dropLateFrames;
        SkipFrames = skipFrames;
        ClockJitter = clockJitter;
        ClockSynchro = clockSynchro;
        ReconnectTimeout = reconnectTimeout;
    }

    private string[] GetOptions()
    {
        var options = new List<string>();
        if (RtspTcp)
        {
            options.Add("rtsp-tcp");
        }

        if (RtspTcp)
        {
            options.Add($"network-caching={NetworkCaching}");
        }
        else
        {
            options.Add($"udp-caching={NetworkCaching}");
        }

        options.Add($"live-caching={LiveCaching}");
        if (DropLateFrames)
        {
            options.Add("drop-late-frames");
        }
        if (SkipFrames)
        {
            options.Add("skip-frames");
        }
        if (!Audio)
        {
            options.Add("no-audio");
        }
        options.Add($"clock-jitter={ClockJitter}");
        options.Add($"clock-synchro={ClockSynchro}");
        if (ReconnectTimeout > 0)
        {
            options.Add("rtsp-reconnect");
            options.Add($"rtsp-timeout={ReconnectTimeout}");
        }
        return options.ToArray();
    }

    public void ApplyTo(Media? media)
    {
        foreach (var option in GetOptions())
        {
            media?.AddOption(":" + option);
        }
    }

    public string[] GetVlcOptions()
    {
        return GetOptions().Select(x => "--" + x).ToArray();
    }
}