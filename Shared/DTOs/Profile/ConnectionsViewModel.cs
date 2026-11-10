
namespace Shared.DTOs.Profile
{
    public class ConnectionsViewModel
    {
        public List<ExternalConnectionDto> Connections { get; set; } = [];
    }

    public class ExternalConnectionDto
    {
        public string Provider { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
    }
}
