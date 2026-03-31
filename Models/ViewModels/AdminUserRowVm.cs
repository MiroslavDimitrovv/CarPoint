namespace CarPoint.Models.ViewModels
{
    public class AdminUserRowVm
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";

        public int? ClientId { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? PhoneNumber { get; set; }

        public IList<string> Roles { get; set; } = new List<string>();
        public int RentalsCount { get; set; }

        public bool IsAdmin => Roles.Any(r => r == "Admin");
        public string DisplayName
        {
            get
            {
                var name = $"{FirstName} {LastName}".Trim();
                return string.IsNullOrWhiteSpace(name) ? "(без име)" : name;
            }
        }
    }
}
