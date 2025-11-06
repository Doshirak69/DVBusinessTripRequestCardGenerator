using DocsVision.Platform.ObjectManager;
using DocsVision.Platform.ObjectModel;
using DocsVision.Platform.StorageServer;
using Microsoft.Extensions.Configuration;

namespace BusinessTripRequestCardGenerator 
{
    internal class Programm
    {
        private const string JsonFileName = "businessTripData.json";
        static void Main(string[] args)
        {
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;

            var serverUrl = appSettings["DVUrl"];
            var username = appSettings["Username"];
            var password = appSettings["Password"];

            var sessionManager = SessionManager.CreateInstance();
            sessionManager.Connect(serverUrl, string.Empty, username, password);
            UserSession? session = null;
            try
            {
                var mode = AskMode();
                var cardsToMake = mode == WorkMode.Random ? AskCardsCount()
                                                : 1;

                session = sessionManager.CreateSession();
                var context = CreateContext(session);
                var dvSrv = new BusinessTripCardService(context);

                switch (mode)
                {
                    case WorkMode.Random:
                        RunRandomGeneration(dvSrv, context, cardsToMake);
                        break;

                    case WorkMode.FromJson:
                        RunFromJson(dvSrv);
                        break;
                }

                Console.WriteLine("\n== Готово. Нажмите любую клавишу ==");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nКритическая ошибка: {ex}");
            }
            finally
            {
                session?.Close();
            }
           
                
            Console.ReadKey();
        }

        private static WorkMode AskMode()
        {
            Console.WriteLine("Выберите режим работы:\n 1 – случайные заявки\n 2 – из JSON");
            Console.Write("Ваш выбор (1/2): ");
            string? answer = Console.ReadLine();
            return answer == "1" ? WorkMode.Random : WorkMode.FromJson;
        }

        private static int AskCardsCount()
        {
            Console.Write("Сколько карточек создать? ");
            int.TryParse(Console.ReadLine(), out int n);

            return n > 0 ? n : 1;
        }

        private static void RunRandomGeneration(BusinessTripCardService dvSrv, ObjectContext context, int count)
        {
            var generator = new RandomCardGenerator(context);
            for (int i = 0; i < count; i++)
            {
                var dto = generator.CreateRandom();
                dvSrv.ProcessBusinessTripRequest(dto);
            }
        }

        private static void RunFromJson(BusinessTripCardService dvSrv)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(JsonFileName, optional: false, reloadOnChange: true)
                .Build();

            var dto = config.GetSection("BusinessTripRequest").Get<BusinessTripRequestDto>();
            if (dto == null)
                throw new Exception($"Не удалось загрузить данные заявки из {JsonFileName}");

            dvSrv.ProcessBusinessTripRequest(dto);
        }

        private static ObjectContext CreateContext(UserSession session)
            => DocsVision.BackOffice.ObjectModel.ContextFactory.CreateContext(session);

        private enum WorkMode { Random, FromJson }
    }
}
