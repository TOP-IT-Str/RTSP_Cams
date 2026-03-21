namespace RTSP_Cams.Settings
{
    public sealed class AppSettings
    {
        public string IpAddress { get; set; } = "192.168.1.108";
        public string Username { get; set; } = "admin";
        public string Password { get; set; } = "";
        public ushort CameraCount { get; set; } = 4;
        public ushort RtspPort { get; set; } = 554;

        public bool IsFullScreen { get; set; } = false;
        public byte Volume { get; set; } = 100;

        public VlcSettings VlcSettings { get; set; } = new();

        public List<string> CameraNames { get; set; } = new();

        public string GetCameraName(int channel)
        {
            if (
                channel - 1 >= 0 &&
                channel - 1 < CameraNames.Count)
            {
                string customName = CameraNames[channel - 1];

                if (!string.IsNullOrWhiteSpace(customName))
                    return customName;
            }

            return $"Камера {channel}";
        }
    }
}