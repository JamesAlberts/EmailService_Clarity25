//using EmailService_Clarity25.Library;
using EmailService_Clarity25.API.Data;
using EmailService_Clarity25.API.Models;
using EmailService_Clarity25.API.Services;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

//Add service to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddDebug();
});

//configuration
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

//dbcontext
builder.Services.AddDbContext<EmailLogDbContext>(options => options.UseInMemoryDatabase("JamesInMemoryDatabase_API")); // in memory database

//emailsender
builder.Services.AddScoped<IEmailSender, EmailSender>();

var app = builder.Build();

// configure the HTTP Request pipline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// create the database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EmailLogDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();

