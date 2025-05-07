using EmailService_Clarity25.Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EmailService_Clarity25.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // setting up the configuration
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory.ToString())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // setup sevices
            IServiceCollection services = new ServiceCollection();
            ConfigureServices(services, configuration);

            //build the service provider
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // build the logger and email sender from the provider
            ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            IEmailSender emailSender = serviceProvider.GetRequiredService<IEmailSender>();

            // make sure the database is ready
            try
            {
                var dbContext = serviceProvider.GetRequiredService<EmailLogDbContext>();
                {
                    dbContext.Database.EnsureCreated(); // making sure the in-memory database has been created
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error has occured while setting up the database.");
                System.Console.WriteLine($"Falied to setup the database,this message has also been added to the logs: {ex.Message}");
                return; // the database setup has failed, stop the pressess!!!
            }

            // get the recipient email from the command line if provided, otherwise use the default email
            string recipient = args.Length > 0 ? args[0] : configuration.GetValue<string>("DefaultRecipient"); // if we don't get am eamil entered, use the one int he appseetings file
            if (string.IsNullOrEmpty(recipient))
            {
                logger.LogError("The recipeint email is missing. please provide it as a commandline argument or set the defaultRecipient in the appsettings.json file.");
                System.Console.WriteLine("Please provide a recipient email address as a command line argument or set the default in appsettings.json");
                return; // we are missing required data, stop and try again
            }

            // almost all the checks have been cleared, here is where it should actually work. 
            try
            {
                System.Console.WriteLine($"Sending email to {recipient}...");
                logger.LogInformation($"Sending email to {recipient}");
                await emailSender.SendEmailAsync(recipient, "Test email from James' Email Clarity App", $"This is a test of the Clarity email service project sent to {recipient} at {DateTime.UtcNow}");
                System.Console.WriteLine("Email sent successfully");
                logger.LogInformation("Email sent successfully");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to send message: {ex.Message}");
                logger.LogInformation(ex, "Failed to send message");
            }

            System.Console.WriteLine("Press any key to Exit");
            System.Console.ReadKey();
        }

        // method to configure services for dependency injection
        static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // add logging
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddDebug(); // add debug logger, this is good enough for my purposes. 
            });

            // add configuration
            services.AddSingleton(configuration);

            // add emailSettings
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

            // add emailSender
            services.AddScoped<IEmailSender, EmailSender>();

            // add DbContext
            //services.AddDbContext<EmailLogDbContext>(options => options.UseInMemoryDatabase("EmailLogDatabase"));  // pssst, here is the name of the in-memory db
            services.AddDbContext<EmailLogDbContext>(options => options.UseInMemoryDatabase("JamesInMemoryDatabase_console"));
        }
    }
}