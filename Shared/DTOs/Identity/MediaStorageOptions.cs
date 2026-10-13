namespace Shared.DTOs.Identity
{
    public class MediaStorageOptions
    {
        public string RootPath { get; set; } = "wwwroot/uploads";

        public string ProductFolder { get; set; } = "products";

        /// <summary>
        /// Thư mục lưu file đính kèm chat.
        /// </summary>
        public string ChatFolder { get; set; } = "chats";
    }
}
