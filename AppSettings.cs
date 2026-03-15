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