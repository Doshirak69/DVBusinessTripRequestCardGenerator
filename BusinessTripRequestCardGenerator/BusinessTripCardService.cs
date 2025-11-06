using DocsVision.BackOffice.CardLib.CardDefs;
using DocsVision.BackOffice.ObjectModel.Services;
using DocsVision.BackOffice.ObjectModel;
using DocsVision.Platform.ObjectModel.Search;
using DocsVision.Platform.ObjectModel;
using System.Text.Json;

namespace BusinessTripRequestCardGenerator
{
    class BusinessTripCardService
    {
        private readonly ObjectContext _context;

        public BusinessTripCardService(ObjectContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void ProcessBusinessTripRequest(BusinessTripRequestDto template)
        {
            Console.WriteLine("Старт обработки заявки на командировку...");

            var dto = JsonSerializer.Deserialize<BusinessTripRequestDto>(
                JsonSerializer.Serialize(template))!;

            Document newCard = CreateBusinessTripCard(dto.View);

            FillMainInfo(newCard.MainInfo, dto);

            Console.WriteLine("Поля карточки в MainInfo заполнены.");

            var optionalActionsLog = new List<string>();

            if (dto.ApproverAccountNames != null &&
                dto.ApproverAccountNames.Any(a => !string.IsNullOrWhiteSpace(a)))
            {
                _context.AcceptChanges();
                string approversMsg = AddApproversToCard(newCard, dto.ApproverAccountNames);
                optionalActionsLog.Add(approversMsg);
            }

            if (!string.IsNullOrWhiteSpace(dto.AttachmentFilePath))
            {
                _context.AcceptChanges();
                string fileMsg = AttachMainFileToCard(newCard, dto.AttachmentFilePath);
                optionalActionsLog.Add(fileMsg);
            }

            if (!string.IsNullOrWhiteSpace(dto.WorkflowState))
            {
                _context.AcceptChanges();
                var targetEn = MappingService.MapStateName(dto.WorkflowState);
                if (!string.IsNullOrWhiteSpace(targetEn))
                {
                    var navigator = new CardStateNavigator(_context);
                    var msg = navigator.ChangeStateTo(newCard, targetEn!);
                    optionalActionsLog.Add(msg);
                }
            }

            _context.AcceptChanges();
            _context.SaveObject(newCard);

            Console.WriteLine("Обработка заявки на командировку успешно завершена.");
            Console.WriteLine($"Подготовлена карточка '{newCard.MainInfo.Name}' (ID: {newCard.GetObjectId()}).");
        }

        private Document CreateBusinessTripCard(string cardKindName)
        {
            IDocumentService docSvc = _context.GetService<IDocumentService>();
            KindsCardKind cardKind = GetCardKindByName(cardKindName);

            Document newCard = docSvc.CreateDocument(null, cardKind) 
                ?? throw new InvalidOperationException($"Не удалось создать карточку вида '{cardKindName}'.");
            newCard.MainInfo.Name = $"Заявка на командировку от {DateTime.Now.ToShortDateString()}";
            return newCard;
        }

        private void FillMainInfo(DocumentMainInfo mainInfo, BusinessTripRequestDto dto)
        {
            var unfilledFields = new List<string>();

            StaffEmployee? travelingEmployee = FindEmployeeByAccount(dto.TravelingEmployeeAccount);
            if (travelingEmployee != null)
            {
                mainInfo["TravelingEmployee"] = travelingEmployee.GetObjectId();
                StaffEmployee? manager = travelingEmployee.GetManager(_context);
                if (manager != null)
                {
                    mainInfo["Manager"] = manager.GetObjectId();
                    mainInfo["PhoneNumber"] = manager.Phone;
                }
                PartnersCompany? organization = travelingEmployee.GetPartnersOrganization(_context);
                if (organization != null)
                {
                    mainInfo["OrganizationName"] = organization.GetObjectId();
                }
            }

            StaffEmployee? processingEmployee = FindEmployeeByAccount(dto.ProcessingEmployee);
            if (processingEmployee != null)
            {
                mainInfo["ResponsibleEmployee"] = processingEmployee.GetObjectId();
            }

            mainInfo["DateFrom"] = dto.DateFrom;
            mainInfo["DateTo"] = dto.DateTo;
            mainInfo["ReasonForTravel"] = dto.ReasonForTravelItem;

            int numberOfDays = Math.Abs((int)(dto.DateTo.Date - dto.DateFrom.Date).TotalDays) + 1;
            mainInfo["NumberOfDaysOnBusinessTrip"] = numberOfDays;

            BaseUniversalItem? cityItem = GetUniversalItemByName("Города", dto.City);
            if (cityItem != null)
            {
                mainInfo["City"] = cityItem.GetObjectId();
                var dsa = (decimal)cityItem.ItemCard.MainInfo["DailySubsistenceAllowance"];
                mainInfo["TripAllowanceAmount"] = dsa * numberOfDays;
            }

            if (!string.IsNullOrWhiteSpace(dto.Tickets))
            {
                var tik = MappingService.MapTickets(dto.Tickets); 
                if (tik.HasValue)
                    mainInfo["Tickets"] = (int)tik.Value;
            }
        }

        private string AddApproversToCard(Document card, IEnumerable<string> approverAccountNames)
        {
            var cleanAccounts = approverAccountNames
                 .Where(a => !string.IsNullOrWhiteSpace(a))
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 .ToList();

            var approversSection = (IList<BaseCardSectionRow>)card.GetSection(CardDocument.Approvers.ID);
            int added = 0;
            var notFound = new List<string>();

            foreach (string accountName in cleanAccounts)
            {
                StaffEmployee? approverEmployee = FindEmployeeByAccount(accountName);
                if (approverEmployee != null)
                {
                    var approverRow = new BaseCardSectionRow();
                    approverRow[CardDocument.Approvers.Approver] = approverEmployee.GetObjectId();
                    approversSection.Add(approverRow);
                    added++;
                }
                else
                {
                    notFound.Add(accountName);
                }
            }

            if (notFound.Count == 0)
                return $"Утверждающие: добавлено {added}.";
            else
                return $"Утверждающие: добавлено {added}. Не найдены: {string.Join(", ", notFound)}.";
        }

        private string AttachMainFileToCard(Document card, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "Файл: путь не указан — прикрепление пропущено.";

            if (!File.Exists(filePath))
                return $"Файл: не найден по пути '{filePath}' — прикрепление пропущено.";

            IDocumentService documentService = _context.GetService<IDocumentService>();

            documentService.AddMainFile(card, filePath);
            _context.SaveObject(card);

            return $"Файл: прикреплён '{Path.GetFileName(filePath)}'.";
        }

        private KindsCardKind GetCardKindByName(string cardKindName)
        {
            KindsCardKind cardKind = _context.FindObject<KindsCardKind>(
                new QueryObject(KindsCardKind.NameProperty.Name, cardKindName)) 
                ?? throw new Exception($"Вид карточки '{cardKindName}' не найден.");
            
            return cardKind;
        }

        private StaffEmployee? FindEmployeeByAccount(string accountName)
        {
            IStaffService staffService = _context.GetService<IStaffService>();
            return staffService.FindEmpoyeeByAccountName(accountName);
        }

        private BaseUniversalItem? GetUniversalItemByName(string dictionaryName, string itemName)
        {
            IBaseUniversalService universalService = _context.GetService<IBaseUniversalService>();

            BaseUniversalItemType dictionaryType = universalService.FindItemTypeWithSameName(dictionaryName, null);

            if (dictionaryType == null)
            {
                Console.WriteLine($"Внимание: Тип универсального справочника '{dictionaryName}' не найден.");
                return null;
            }
            BaseUniversalItem item = universalService.FindItemWithSameName(itemName, dictionaryType);

            if (item == null)
            {
                Console.WriteLine($"Внимание: Элемент '{itemName}' в справочнике '{dictionaryName}' не найден.");
                return null;
            }

            return item;
        }

    }
}
