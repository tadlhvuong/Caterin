namespace Website.Areas.Admin.Models
{
    public class SettingViewModel
    {
        public string CurrentTab { get;set; }
        public ICollection<SettingResult> Settings { get; set; } = new List<SettingResult>();
    }
    public class SettingResult
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
