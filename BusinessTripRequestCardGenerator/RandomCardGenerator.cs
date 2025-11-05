using DocsVision.BackOffice.ObjectModel.Services;
using DocsVision.BackOffice.ObjectModel;
using DocsVision.Platform.ObjectModel;
using DocumentFormat.OpenXml.Bibliography;

namespace BusinessTripRequestCardGenerator
{
    class RandomCardGenerator
    {
        private readonly ObjectContext _context;
        private readonly Random _random = new();

        private IReadOnlyList<StaffEmployee>? _employees;
        private IReadOnlyList<BaseUniversalItem>? _cities;

        internal RandomCardGenerator(ObjectContext сontext)
        {
            _context = сontext;
            LoadDictionaries();
        }

        private void LoadDictionaries()
        {
            var staffSvc = _context.GetService<IStaffService>();
            var employee = staffSvc.FindEmpoyeeByAccountName(_context.UserAccount);
            StaffUnit organization = employee.GetOrganization(_context);
            _employees = staffSvc.GetUnitEmployees(organization, true, true).ToList();
        }

        internal BusinessTripRequestDto CreateRandom()
        {
            var dto = new BusinessTripRequestDto();
            dto.View = "Заявка на командировку";
            var traveler = PickRandom(_employees!);
            dto.TravelingEmployeeAccount = traveler.AccountName;
            
            StaffEmployee processor;
            do { processor = PickRandom(_employees!); } while (processor == traveler);
            dto.ProcessingEmployee = processor.AccountName;

            dto.ApproverAccountNames = PickRandomMany(
                _employees!.Where(e => e != traveler && e != processor)
                           .Select(e => e.AccountName)
                           .ToList(),
                1, 2).ToList();

            var start = DateTime.Today.AddDays(_random.Next(1, 30));
            dto.DateFrom = start;
            dto.DateTo = start.AddDays(_random.Next(1, 10));

            dto.Tickets = PickRandom(MappingService.TicketDisplayNames);
            dto.WorkflowState = PickRandom(MappingService.StateRuDisplayNames);
            dto.City = PickRandom(MappingService.CityNames);

            dto.AttachmentFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                  "DefaultAttachment.txt");
            return dto;
        }
        private T PickRandom<T>(IReadOnlyList<T> list) => list[_random.Next(list.Count)];
        private IEnumerable<T> PickRandomMany<T>(IReadOnlyList<T> list, int min, int max)
        {
            int need = _random.Next(min, max + 1);
            return list.OrderBy(_ => _random.Next()).Take(need);
        }

    }
}

    
