using CarPoint.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarPoint.Data
{
    public static class AppSeed
    {
        private const string AdminRole = "Admin";
        private const string AdminEmail = "admin@carpoint.bg";
        private const string AdminPassword = "Admin123!";
        private const string UserEmail = "client@carpoint.bg";
        private const string UserPassword = "Client123!";
        private const string GuestSupportEmail = "guest-support@carpoint.local";
        private const string GuestSupportPassword = "GuestSupport!123";

        // Test@test1.com - Test@test1.com

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await EnsureRolesAsync(roleManager);

            var adminUser = await EnsureUserAsync(userManager, AdminEmail, AdminPassword, "0888000001");
            if (!await userManager.IsInRoleAsync(adminUser, AdminRole))
            {
                await userManager.AddToRoleAsync(adminUser, AdminRole);
            }

            var clientUser = await EnsureUserAsync(userManager, UserEmail, UserPassword, "0888000002");
            var guestSupportUser = await EnsureUserAsync(userManager, GuestSupportEmail, GuestSupportPassword, "0000000000");

            await EnsureClientAsync(
                db,
                adminUser.Id,
                "Админ",
                "CarPoint",
                AdminEmail,
                "0888000001",
                "гр. София, бул. България 12");

            var client = await EnsureClientAsync(
                db,
                clientUser.Id,
                "Иван",
                "Петров",
                UserEmail,
                "0888000002",
                "гр. Варна, ул. Приморска 8");

            await EnsureCarsAsync(db);
            await db.SaveChangesAsync();

            await EnsureSupportAsync(db, clientUser.Id, client.Email, guestSupportUser.Id);
            await db.SaveChangesAsync();
        }

        private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync(AdminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(AdminRole));
            }
        }

        private static async Task<ApplicationUser> EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string phoneNumber)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                return user;
            }

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PhoneNumber = phoneNumber,
                PhoneNumberConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Failed to create seeded user {email}: {errors}");
            }

            return user;
        }

        private static async Task<Client> EnsureClientAsync(
            ApplicationDbContext db,
            string userId,
            string firstName,
            string lastName,
            string email,
            string phone,
            string address)
        {
            var existing = await db.Clients.FirstOrDefaultAsync(x => x.UserId == userId);
            if (existing != null)
            {
                return existing;
            }

            var client = new Client
            {
                UserId = userId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phone,
                Address = address,
                Egn = "9001011234",
                CreatedAt = DateTime.UtcNow
            };

            db.Clients.Add(client);
            await db.SaveChangesAsync();
            return client;
        }

        private static async Task EnsureCarsAsync(ApplicationDbContext db)
        {
            if (await db.Cars.AnyAsync())
            {
                return;
            }

            var cars = new[]
            {
                new Car
                {
                    Brand = "BMW",
                    Model = "320d xDrive",
                    ImageFileName = "placeholder.jpg",
                    Year = 2021,
                    Description = "Поддържан седан с пълна сервизна история, автоматична скоростна кутия и богато оборудване.",
                    Mileage = 68400,
                    Engine = "2.0 дизел",
                    HorsePower = 190,
                    FuelType = "Дизел",
                    Transmission = "Автоматична",
                    Type = Car.ListingType.ForSale,
                    CurrentOffice = OfficeLocation.Sofia,
                    SalePrice = 54800m,
                    Status = Car.StatusType.Available
                },
                new Car
                {
                    Brand = "Audi",
                    Model = "A6 45 TFSI",
                    ImageFileName = "placeholder.jpg",
                    Year = 2020,
                    Description = "Комфортен семеен автомобил с навигация, LED светлини и адаптивен круиз контрол.",
                    Mileage = 79200,
                    Engine = "2.0 бензин",
                    HorsePower = 245,
                    FuelType = "Бензин",
                    Transmission = "Автоматична",
                    Type = Car.ListingType.ForSale,
                    CurrentOffice = OfficeLocation.Varna,
                    SalePrice = 59700m,
                    Status = Car.StatusType.Available
                },
                new Car
                {
                    Brand = "Toyota",
                    Model = "Corolla Touring Sports",
                    ImageFileName = "placeholder.jpg",
                    Year = 2022,
                    Description = "Икономичен хибрид с просторен салон, подходящ за градско и междуградско шофиране.",
                    Mileage = 38400,
                    Engine = "1.8 хибрид",
                    HorsePower = 140,
                    FuelType = "Хибрид",
                    Transmission = "Автоматична",
                    Type = Car.ListingType.ForSale,
                    CurrentOffice = OfficeLocation.Burgas,
                    SalePrice = 43900m,
                    Status = Car.StatusType.Available
                },
                new Car
                {
                    Brand = "Volkswagen",
                    Model = "Passat Variant",
                    ImageFileName = "placeholder.jpg",
                    Year = 2021,
                    Description = "Практично комби за дълги пътувания с много място за багаж и отличен разход.",
                    Mileage = 55800,
                    Engine = "2.0 TDI",
                    HorsePower = 150,
                    FuelType = "Дизел",
                    Transmission = "Автоматична",
                    Type = Car.ListingType.ForRent,
                    CurrentOffice = OfficeLocation.Ruse,
                    RentPricePerDay = 89m,
                    Status = Car.StatusType.Available
                },
                new Car
                {
                    Brand = "Skoda",
                    Model = "Octavia",
                    ImageFileName = "placeholder.jpg",
                    Year = 2023,
                    Description = "Надежден автомобил под наем с нисък разход, удобен за пътуване и бизнес срещи.",
                    Mileage = 22100,
                    Engine = "1.5 TSI",
                    HorsePower = 150,
                    FuelType = "Бензин",
                    Transmission = "Автоматична",
                    Type = Car.ListingType.ForRent,
                    CurrentOffice = OfficeLocation.Sofia,
                    RentPricePerDay = 79m,
                    Status = Car.StatusType.Available
                },
                new Car
                {
                    Brand = "Renault",
                    Model = "Clio",
                    ImageFileName = "placeholder.jpg",
                    Year = 2022,
                    Description = "Компактен градски автомобил, удобен за ежедневни маршрути и кратки уикенд пътувания.",
                    Mileage = 30800,
                    Engine = "1.0 TCe",
                    HorsePower = 100,
                    FuelType = "Бензин",
                    Transmission = "Ръчна",
                    Type = Car.ListingType.ForRent,
                    CurrentOffice = OfficeLocation.Varna,
                    RentPricePerDay = 55m,
                    Status = Car.StatusType.Available
                }
            };

            db.Cars.AddRange(cars);
        }

        private static async Task EnsureSupportAsync(ApplicationDbContext db, string userId, string userEmail, string guestSupportUserId)
        {
            if (await db.SupportTickets.AnyAsync())
            {
                return;
            }

            var openTicket = new SupportTicket
            {
                UserId = userId,
                Subject = "Въпрос за наличен автомобил",
                Description = "Интересувам се от BMW 320d xDrive и искам да знам дали може да се види на място в София тази седмица.",
                Category = TicketCategory.Sales,
                Priority = TicketPriority.Normal,
                Status = TicketStatus.InProgress,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var closedTicket = new SupportTicket
            {
                UserId = userId,
                Subject = "Нужна е помощ за регистрация",
                Description = "Регистрацията вече е успешна, но пазя заявката като пример за история на комуникацията.",
                Category = TicketCategory.Account,
                Priority = TicketPriority.Low,
                Status = TicketStatus.Closed,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                UpdatedAt = DateTime.UtcNow.AddDays(-7)
            };

            var guestTicket = new SupportTicket
            {
                UserId = guestSupportUserId,
                Subject = "Запитване за наем от летище Варна",
                Description =
                    $"Име за контакт: Иван Петров{Environment.NewLine}" +
                    $"Имейл за контакт: {userEmail}{Environment.NewLine}{Environment.NewLine}" +
                    "Търся автомобил под наем за 5 дни с автоматична скоростна кутия и вземане от офис Варна.",
                Category = TicketCategory.Rentals,
                Priority = TicketPriority.High,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            db.SupportTickets.AddRange(openTicket, closedTicket, guestTicket);
            await db.SaveChangesAsync();

            db.SupportTicketMessages.AddRange(
                new SupportTicketMessage
                {
                    TicketId = openTicket.Id,
                    AuthorUserId = userId,
                    IsAdmin = false,
                    Message = openTicket.Description,
                    CreatedAt = openTicket.CreatedAt
                },
                new SupportTicketMessage
                {
                    TicketId = openTicket.Id,
                    AuthorUserId = userId,
                    IsAdmin = true,
                    Message = "Здравейте, автомобилът е наличен и можем да организираме оглед в офиса ни в София.",
                    CreatedAt = openTicket.CreatedAt.AddHours(3)
                },
                new SupportTicketMessage
                {
                    TicketId = closedTicket.Id,
                    AuthorUserId = userId,
                    IsAdmin = false,
                    Message = closedTicket.Description,
                    CreatedAt = closedTicket.CreatedAt
                },
                new SupportTicketMessage
                {
                    TicketId = closedTicket.Id,
                    AuthorUserId = userId,
                    IsAdmin = true,
                    Message = "Проблемът е отстранен. Вече можете да влизате в профила си нормално.",
                    CreatedAt = closedTicket.CreatedAt.AddHours(1)
                },
                new SupportTicketMessage
                {
                    TicketId = guestTicket.Id,
                    AuthorUserId = guestSupportUserId,
                    IsAdmin = false,
                    Message = guestTicket.Description,
                    CreatedAt = guestTicket.CreatedAt
                });
        }
    }
}
