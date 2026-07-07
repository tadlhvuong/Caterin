namespace Website.Areas.Admin.Models
{
    public class ExchangeModel
    {
        public string date;
        public Dictionary<string, object> vnd;
    }
    public class CommonModel
    {
        public string Key;
        public string Value;
    }
    public class ModalFormRemove
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsDeleted { get { return false; } set { } }
    }
    public class ModalFormResult
    {
        public int Code { get; set; }
        public string? Message { get; set; }
        public string? data { get; set; }
        public string? SubData { get; set; }
        public string? ReturnUrl { get; set; }
    }
    public class ModalFormPageResult
    {
        public int Code { get; set; }
        public string Message { get; set; }
        //public PagedData<MediaFile> data { get; set; }
        public string[]? previewData { get; set; }
        public object[]? previewConfig { get; set; }
    }
    public class FileUploadResult
    {
        public string[] initialPreview { get; set; }
        public object[] initialPreviewConfig { get; set; }
    }
    public class AvatarUploadResult
    {
        public string defaultPreviewContent { get; set; }
    }
    public class FileUploadModel
    {
        public int? Id { get; set; }

        public string FileName { get; set; }

        public string FileData { get; set; }
    }
    public class FileUploadAttribute
    {

        public string FileName { get; set; }

        public string FileData { get; set; }
    }

    public class SelectItemModel
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public bool Selected { get; set; }
        public List<SelectItemModel> ItemsChild { get; set; }
    }
    public class SelectItemStringModel
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public bool Selected { get; set; }
        public List<SelectItemModel> ItemsChild { get; set; }
    }

    public class SelectIconItemModel
    {
        public int Value { get; set; }
        public string Text { get; set; }
        public string Avatar { get; set; }
        public bool Selected { get; set; }
    }
}
