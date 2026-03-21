namespace RTSP_Cams.Settings;

public sealed class VlcSettings
{
    public ConcretVlcSettings SubGrid { get; set; } = new ConcretVlcSettings(
        rtspTcp: true,
        networkCaching: 50,
        liveCaching: 50,
        dropLateFrames: true,
        skipFrames: true,
        audio: false,
        reconnectTimeout: 5
    );
    public ConcretVlcSettings SubFullscreen { get; set; } = new ConcretVlcSettings(
        rtspTcp: true,
        networkCaching: 50,
        liveCaching: 50,
        dropLateFrames: true,
        skipFrames: true,
        audio: true,
        reconnectTimeout: 1
    );
    public ConcretVlcSettings MainFullscreen { get; set; } = new ConcretVlcSettings(
        rtspTcp: true,
        networkCaching: 300,
        liveCaching: 300,
        dropLateFrames: true,
        skipFrames: true,
        audio: true,
        reconnectTimeout: 1
    );
}