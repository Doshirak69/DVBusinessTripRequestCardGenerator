using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessTripRequestCardGenerator
{
    internal class MappingService
    {
        public static readonly IReadOnlyList<string> TicketDisplayNames = new[] { "Авиа", "Поезд" };
        public static readonly IReadOnlyList<string> CityNames = new[]
        {
            "Москва",
            "Мурманск",
            "Оренбург",
            "Саратов"
        };
        private static readonly Dictionary<string, string> StateAliases = 
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Проект"] = "Project",
                ["На согласовании"] = "Under Review",
                ["На оформлении"] = "On Registration",
                ["Закрыто"] = "Closed",
            };
        public static readonly IReadOnlyList<string> StateRuDisplayNames = StateAliases.Keys.ToList();

        public static Tickets? MapTickets(string? name) => name?.Trim() switch
        {
            "Авиа" => Tickets.Avia,
            "Поезд" => Tickets.Rail,
            _ => null
        };
        public enum Tickets
        {
            Avia,
            Rail
        }

        public static string? MapStateName(string? ruName) 
            => ruName != null && StateAliases.TryGetValue(ruName, out var canonical)
            ? canonical : null;

    }
}
