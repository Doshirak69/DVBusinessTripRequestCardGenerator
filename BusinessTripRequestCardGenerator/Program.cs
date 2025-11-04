using DocsVision.Platform.ObjectManager;
using DocsVision.Platform.ObjectModel;
using Microsoft.Extensions.Configuration;

namespace BusinessTripRequestCardGenerator 
{
    internal class Program
    {
        private const string JsonFileName = "businessTripData.json";
        static void Main(string[] args)
        {
            var serverURL = System.Configuration.ConfigurationManager.AppSettings["DVUrl"];
            var username = System.Configuration.ConfigurationManager.AppSettings["Username"];
            var password = System.Configuration.ConfigurationManager.AppSettings["Password"];

            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(JsonFileName, optional: false, reloadOnChange: true)
                .Build();

            var businessTripRequestData = config.GetSection("BusinessTripRequest").Get<BusinessTripRequestData>();
            if (businessTripRequestData == null)
            {
                Console.WriteLine($"Ошибка: Не удалось загрузить данные заявки из {JsonFileName}");
                Console.ReadKey();
                return;
            }

            var sessionManager = SessionManager.CreateInstance();
            sessionManager.Connect(serverURL, String.Empty, username, password);

            UserSession? session = null;
            try
            {
                session = sessionManager.CreateSession();
                var context = CreateContext(session);
                DocsVisionService dvService = new DocsVisionService(context);
                dvService.ProcessBusinessTripRequest(businessTripRequestData);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nПроизошла критическая ошибка: {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            finally
            {
                session?.Close();
            }

        }
        public static ObjectContext CreateContext(UserSession session)
        {
            return DocsVision.BackOffice.ObjectModel.ContextFactory.CreateContext(session);
        }

    }
}
