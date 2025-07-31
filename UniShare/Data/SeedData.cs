using Microsoft.AspNetCore.Identity;
using UniShare.Models;

namespace UniShare.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created
            context.Database.EnsureCreated();

            // Seed Roles
            await SeedRoles(roleManager);

            // Seed Courses
            await SeedCourses(context);

            // Seed Admin User
            await SeedAdminUser(userManager, context);

            await context.SaveChangesAsync();
        }

        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Administrador", "Professor", "Aluno" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedCourses(ApplicationDbContext context)
        {
            if (!context.Courses.Any())
            {
                var courses = new List<Course>
                {
                    new Course
                    {
                        Name = "Engenharia Informática",
                        Code = "EI",
                        Description = "Curso de Engenharia Informática",
                        TotalECTS = 180
                    },
                    new Course
                    {
                        Name = "Gestão",
                        Code = "GEST",
                        Description = "Curso de Gestão",
                        TotalECTS = 180
                    },
                    new Course
                    {
                        Name = "Engenharia Civil",
                        Code = "EC",
                        Description = "Curso de Engenharia Civil",
                        TotalECTS = 180
                    },
                    new Course
                    {
                        Name = "Psicologia",
                        Code = "PSI",
                        Description = "Curso de Psicologia",
                        TotalECTS = 180
                    }
                };

                context.Courses.AddRange(courses);
                await context.SaveChangesAsync();

                // Seed some subjects for Engenharia Informática
                var eiCourse = context.Courses.First(c => c.Code == "EI");
                var subjects = new List<Subject>
                {
                    new Subject
                    {
                        Name = "Programação I",
                        Code = "PROG1",
                        Description = "Introdução à programação",
                        ECTS = 6,
                        Semester = 1,
                        Year = 1,
                        CourseId = eiCourse.Id
                    },
                    new Subject
                    {
                        Name = "Matemática Discreta",
                        Code = "MD",
                        Description = "Fundamentos de matemática discreta",
                        ECTS = 6,
                        Semester = 1,
                        Year = 1,
                        CourseId = eiCourse.Id
                    },
                    new Subject
                    {
                        Name = "Estruturas de Dados",
                        Code = "ED",
                        Description = "Estruturas de dados e algoritmos",
                        ECTS = 6,
                        Semester = 2,
                        Year = 1,
                        CourseId = eiCourse.Id
                    }
                };

                context.Subjects.AddRange(subjects);
            }
        }

        private static async Task SeedAdminUser(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            const string adminEmail = "admin@unishare.pt";
            const string adminPassword = "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Administrador Sistema",
                    StudentNumber = "ADMIN001",
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrador");
                }
            }

            // Create a sample professor
            const string profEmail = "professor@unishare.pt";
            var profUser = await userManager.FindByEmailAsync(profEmail);
            if (profUser == null)
            {
                var eiCourse = context.Courses.FirstOrDefault(c => c.Code == "EI");
                profUser = new ApplicationUser
                {
                    UserName = profEmail,
                    Email = profEmail,
                    EmailConfirmed = true,
                    FullName = "Professor Exemplo",
                    StudentNumber = "PROF001",
                    CourseId = eiCourse?.Id,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(profUser, "Professor123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(profUser, "Professor");
                }
            }

            // Create a sample student
            const string studentEmail = "aluno@unishare.pt";
            var studentUser = await userManager.FindByEmailAsync(studentEmail);
            if (studentUser == null)
            {
                var eiCourse = context.Courses.FirstOrDefault(c => c.Code == "EI");
                studentUser = new ApplicationUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    EmailConfirmed = true,
                    FullName = "Aluno Exemplo",
                    StudentNumber = "20240001",
                    CourseId = eiCourse?.Id,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(studentUser, "Aluno123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(studentUser, "Aluno");
                }
            }
        }
    }
}