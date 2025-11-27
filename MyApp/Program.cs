using MyApp.Components;
using MyApp.Data;
using MyApp.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Entity Framework Core with SQL Server and enable DbContext pooling
builder.Services.AddDbContextPool<PatientDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    "Server=(LocalDB)\\mssqllocaldb;Database=PatientManagementDb;Trusted_Connection=true;"), poolSize: 128);

// Register PatientService and TransactionService
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<TransactionService>();

// Add API Controllers
builder.Services.AddControllers();

// Add API documentation/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PatientDbContext>();
    try
    {
        // Drop and recreate database to ensure clean state
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
        SeedPatientData(dbContext);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while seeding the database: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Enable Swagger in development
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// Map API controllers
app.MapControllers();

// Map Razor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static void SeedPatientData(PatientDbContext context)
{
    if (context.Patients.Any())
    {
        return; // Database has been seeded
    }

    var patients = new[]
    {
        new MyApp.Models.Patient
        {
            Name = "John Doe",
            DateOfBirth = new DateTime(1985, 6, 15),
            ContactInfo = "555-1234",
            MedicalRecordNumber = "MRN-001"
        },
        new MyApp.Models.Patient
        {
            Name = "Jane Smith",
            DateOfBirth = new DateTime(1990, 9, 20),
            ContactInfo = "555-5678",
            MedicalRecordNumber = "MRN-002"
        }
    };

    context.Patients.AddRange(patients);
    context.SaveChanges();

    // Seed transactions
    var transactions = new[]
    {
        new MyApp.Models.Transaction
        {
            PatientId = 1, // John Doe
            ServiceType = "Consultation",
            Amount = 100m,
            TransactionDate = new DateTime(2025, 11, 1),
            Status = "Paid"
        },
        new MyApp.Models.Transaction
        {
            PatientId = 2, // Jane Smith
            ServiceType = "X-Ray",
            Amount = 250m,
            TransactionDate = new DateTime(2025, 11, 5),
            Status = "Unpaid"
        }
    };

    context.Transactions.AddRange(transactions);
    context.SaveChanges();
}

