using System;

namespace OpenRdpGuard.Models
{
    public class NavigationItem
    {
        public string Title { get; set; } = string.Empty;
        public string IconGlyph { get; set; } = string.Empty;
        public Type PageType { get; set; } = typeof(object);
    }
}
