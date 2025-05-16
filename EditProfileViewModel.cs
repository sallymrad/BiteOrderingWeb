namespace BiteOrderWeb.ViewModels
{
    public class EditProfileViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public IFormFile ImageFile { get; set; }
        public string? ProfilePicture { get; set; }
    }

}
