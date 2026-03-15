namespace RTSP_Cams2
{
    public sealed class AppSettings
    {
        public string IpAddress { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int CameraCount { get; set; } = 4;
        public int RtspPort { get; set; } = 554;

        public bool IsFullScreen { get; set; } = false;
    }
}