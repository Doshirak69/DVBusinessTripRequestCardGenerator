using DocsVision.BackOffice.ObjectModel;
using DocsVision.BackOffice.ObjectModel.Services;
using DocsVision.Platform.ObjectModel;
namespace BusinessTripRequestCardGenerator
{
    class CardStateNavigator
    {
        private readonly ObjectContext _context;

        public CardStateNavigator(ObjectContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public string ChangeStateTo(Document card, string targetStateName)
        {
            var stateSvc = _context.GetService<IStateService>();
            var current = card.SystemInfo?.State;
            if (current == null) return "Состояние: текущее состояние карточки не определено.";

            if (StateNameEquals(current, targetStateName))
                return $"Состояние: уже находится в '{StateDisplayName(current)}'.";

            var path = FindShortestPathBfs(card, stateSvc, current, targetStateName);
            if (path == null)
                return $"Путь до '{targetStateName}' не найден из '{StateDisplayName(current)}'.";

            foreach (var br in path)
            {
                if (!IsBranchAllowed(stateSvc, br, card))
                    return $"Нет прав на переход к '{StateDisplayName(br.EndState)}'.";

                stateSvc.ChangeState(card, br);
                _context.SaveObject(card);
            }

            return $"Состояние: изменено на '{StateDisplayName(card.SystemInfo?.State)}'.";
        }
       
        private static List<StatesStateMachineBranch>? FindShortestPathBfs( 
            Document card, IStateService stateSvc, StatesState start, string targetStateSystemName)
        {
            var visited = new HashSet<StatesState>();
            var queue = new Queue<StatesState>();
            var parent = new Dictionary<StatesState, StatesState>();
            var parentBranch = new Dictionary<StatesState, StatesStateMachineBranch>();

            visited.Add(start);
            queue.Enqueue(start);

            StatesState? finish = null;

            while (queue.Count > 0)
            {
                var st = queue.Dequeue();
                var branches = stateSvc.FindLineBranchesByStartState(st) ?? new List<StatesStateMachineBranch>();

                foreach (var b in branches)
                {
                    var end = b.EndState;
                    if (end == null || visited.Contains(end))
                        continue;

                    visited.Add(end);
                    parent[end] = st;
                    parentBranch[end] = b;

                    if (StateNameEquals(end, targetStateSystemName))
                    {
                        finish = end;
                        queue.Clear();
                        break;
                    }

                    queue.Enqueue(end);
                }
            }

            if (finish == null)
                return null;

            var path = new List<StatesStateMachineBranch>();
            for (var cur = finish; !ReferenceEquals(cur, start); cur = parent[cur])
                path.Add(parentBranch[cur]);

            path.Reverse();
            return path;
        }

        private static bool StateNameEquals(StatesState? state, string target)
        {
            if (state == null || string.IsNullOrWhiteSpace(target)) return false;
            return string.Equals(state.DefaultName, target, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBranchAllowed(IStateService stateSvc, StatesStateMachineBranch branch, BaseCard card)
        {
            var op = branch.Operation;
            if (op != null)
                return stateSvc.IsOperationAllowedFull(op, card);

            return false;
        }

        private static string StateDisplayName(StatesState? state)
        {
            return state?.DefaultName ?? "?";
        }
    }
}
