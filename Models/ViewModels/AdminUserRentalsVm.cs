namespace CarPoint.Models.ViewModels
{
    public class AdminUserRentalsVm
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }

        public string DisplayName
        {
            get
            {
                var name = $"{FirstName} {LastName}".Trim();
                return string.IsNullOrWhiteSpace(name) ? "(без име)" : name;
            }
        }

        public List<AdminUserRentRowVm> Rentals { get; set; } = new();
    }
}
