namespace OpenRdpGuard.Models
{
    public class ShareEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public bool IsAdminShare { get; set; }
        public bool IsSystemShare { get; set; }
        public bool IsCustomShare { get; set; }
    }
}
