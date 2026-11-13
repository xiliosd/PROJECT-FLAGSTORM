using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Ai;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;

namespace Flagstorm.Presentation
{
    // Traduce toques del jugador en comandos del núcleo. Sin dependencias de Unity para poder probarla.
    public sealed class MatchSession
    {
        public const Side HumanSide = Side.Blue;
        public const Side AiSide = Side.Red;

        // Tope de seguridad: si la IA no termina su turno por sí sola, se le termina.
        private const int MaxAiCommandsPerTurn = 40;

        private readonly BasicAiPlayer _ai;
        private readonly List<HexCoord> _pendingFlags = new List<HexCoord>();
        private readonly List<DefensePlacement> _pendingDefenses = new List<DefensePlacement>();
        private readonly HashSet<int> _knownGuardIdsBefore = new HashSet<int>();
        private readonly HashSet<HexCoord> _knownDefenseCellsBefore = new HashSet<HexCoord>();
        private int _aiCommandsThisTurn;

        public MatchSession(Match match, IReadOnlyList<WeaponDefinition> arsenal, BasicAiPlayer ai)
        {
            Match = match;
            Arsenal = arsenal;
            _ai = ai;
            SelectedSetupDefenseId = SetupDefenseOptions.FirstOrDefault()?.Id;
            Match.Submit(AiSide, _ai.ChooseSetup(Match.ViewFor(AiSide)));
        }

        public Match Match { get; }

        // Catálogo completo de la partida; el arsenal que el jugador lleva es PlayerWeapons.
        public IReadOnlyList<WeaponDefinition> Arsenal { get; }

        // Armas del loadout del jugador, en el orden del catálogo.
        public IReadOnlyList<WeaponDefinition> PlayerWeapons =>
            Arsenal.Where(w => Match.LoadoutOf(HumanSide).Contains(w.Id)).ToList();

        // Defensas del loadout que se pueden poner gratis en la preparación.
        public IReadOnlyList<WeaponDefinition> SetupDefenseOptions =>
            PlayerWeapons.Where(IsDefenseWeapon).ToList();

        public PlayerView View => Match.ViewFor(HumanSide);

        public IReadOnlyList<HexCoord> PendingFlags => _pendingFlags;

        public IReadOnlyList<DefensePlacement> PendingDefenses => _pendingDefenses;

        public string SelectedSetupDefenseId { get; private set; }

        public string SelectedWeaponId { get; private set; }

        public int? SelectedRobotId { get; private set; }

        public bool IsAiTurn => Match.Phase == MatchPhase.Playing && Match.CurrentSide == AiSide;

        public bool IsHumanTurn => Match.Phase == MatchPhase.Playing && Match.CurrentSide == HumanSide;

        public static MatchSession CreateDefault(uint seed) => Create(
            seed,
            ArsenalProgression.UnlockedAfter(0).Select(w => w.Id).ToList(),
            ArsenalProgression.UnlockedAfter(0));

        // La IA arma su loadout entre las mismas armas desbloqueadas y con el mismo tamaño que el del jugador.
        public static MatchSession Create(
            uint seed, IReadOnlyList<string> playerLoadout, IReadOnlyList<WeaponDefinition> unlocked)
        {
            var error = LoadoutRules.Validate(playerLoadout, unlocked);
            if (error != LoadoutError.None)
            {
                throw new System.ArgumentException($"Arsenal inválido: {error}", nameof(playerLoadout));
            }

            var humanIsland = PrototypeIslands.CreateDefault();
            var ai = new BasicAiPlayer(new SeededRandom(seed));
            var aiLoadout = ai.ChooseLoadout(humanIsland, unlocked, playerLoadout.Count);
            var match = new Match(
                humanIsland,
                PrototypeIslands.CreateDefault(),
                RulesConfig.Default,
                PrototypeArsenal.All,
                playerLoadout,
                aiLoadout);
            return new MatchSession(match, PrototypeArsenal.All, ai);
        }

        public WeaponDefinition WeaponById(string weaponId) => Arsenal.FirstOrDefault(w => w.Id == weaponId);

        // Solo se puede elegir una defensa (escudo, antimisiles, torreta, señuelo o defensor) del loadout.
        public bool SelectSetupDefense(string weaponId)
        {
            if (SetupDefenseOptions.All(w => w.Id != weaponId))
            {
                return false;
            }

            SelectedSetupDefenseId = weaponId;
            return true;
        }

        // Devuelve null si el toque solo cambió la selección; un resultado si se intentó un comando.
        public CommandResult TapOwnCell(HexCoord cell)
        {
            RememberKnownPieces();
            if (Match.Phase == MatchPhase.Setup)
            {
                return ToggleSetupCell(cell);
            }

            var weapon = WeaponById(SelectedWeaponId);
            if (!IsHumanTurn || weapon == null)
            {
                return null;
            }

            return IsDefenseWeapon(weapon)
                ? Match.Submit(HumanSide, new BuildDefenseCommand(weapon.Id, cell))
                : CommandResult.Rejected(RejectionReason.OutOfIsland);
        }

        public CommandResult ConfirmSetup()
        {
            RememberKnownPieces();
            var result = Match.Submit(HumanSide, new SetupCommand(_pendingFlags.ToList(), _pendingDefenses.ToList()));
            if (result.IsAccepted)
            {
                _pendingFlags.Clear();
                _pendingDefenses.Clear();
            }

            return result;
        }

        public void SelectWeapon(string weaponId)
        {
            SelectedRobotId = null;
            SelectedWeaponId = SelectedWeaponId == weaponId ? null : weaponId;
        }

        public CommandResult TapEnemyCell(HexCoord cell)
        {
            RememberKnownPieces();
            if (!IsHumanTurn)
            {
                return null;
            }

            var view = View;
            var robotHere = view.OwnRobots.FirstOrDefault(r => r.Position == cell);
            if (robotHere != null)
            {
                SelectedWeaponId = null;
                SelectedRobotId = robotHere.RobotId;
                return null;
            }

            if (SelectedRobotId.HasValue)
            {
                return MoveSelectedRobot(view, cell);
            }

            var weapon = WeaponById(SelectedWeaponId);
            switch (weapon?.Category)
            {
                case WeaponCategory.Reveal:
                    return Match.Submit(HumanSide, new LaunchDroneCommand(weapon.Id, cell));
                case WeaponCategory.Destroy:
                    return Match.Submit(HumanSide, new FireMissileCommand(weapon.Id, cell));
                case WeaponCategory.Capture:
                    return Match.Submit(HumanSide, new DeployRobotCommand(weapon.Id, cell));
                case WeaponCategory.Defend:
                case WeaponCategory.Guard:
                    return CommandResult.Rejected(RejectionReason.OutOfIsland);
                default:
                    return null;
            }
        }

        public CommandResult EndTurn()
        {
            RememberKnownPieces();
            SelectedRobotId = null;
            _aiCommandsThisTurn = 0;
            return Match.Submit(HumanSide, new EndTurnCommand());
        }

        // Aplica un comando de la IA; la escena lo llama con pausas para que el jugador pueda seguirlo.
        public CommandResult StepAi()
        {
            if (!IsAiTurn)
            {
                return null;
            }

            RememberKnownPieces();
            _aiCommandsThisTurn++;
            var command = _aiCommandsThisTurn > MaxAiCommandsPerTurn
                ? new EndTurnCommand()
                : _ai.NextCommand(Match.ViewFor(AiSide), Arsenal);
            var result = Match.Submit(AiSide, command);
            return result.IsAccepted ? result : Match.Submit(AiSide, new EndTurnCommand());
        }

        // Eventos que el jugador puede ver tras un comando: una pieza rival cuenta si estaba a la vista antes
        // del comando o lo está después (p. ej. un defensor que un misil acaba de destruir).
        public IReadOnlyList<IMatchEvent> VisibleEventsOf(CommandResult result)
        {
            var view = View;
            return EventVisibility.Filter(
                result.Events,
                HumanSide,
                _knownGuardIdsBefore.Union(view.KnownEnemyGuards.Select(g => g.RobotId)),
                _knownDefenseCellsBefore.Union(view.KnownEnemyDefenses.Keys));
        }

        private static bool IsDefenseWeapon(WeaponDefinition weapon) =>
            weapon.Category == WeaponCategory.Defend || weapon.Category == WeaponCategory.Guard;

        private void RememberKnownPieces()
        {
            var view = View;
            _knownGuardIdsBefore.Clear();
            _knownGuardIdsBefore.UnionWith(view.KnownEnemyGuards.Select(g => g.RobotId));
            _knownDefenseCellsBefore.Clear();
            _knownDefenseCellsBefore.UnionWith(view.KnownEnemyDefenses.Keys);
        }

        private CommandResult ToggleSetupCell(HexCoord cell)
        {
            if (_pendingFlags.Remove(cell) || _pendingDefenses.RemoveAll(p => p.Cell == cell) > 0)
            {
                return null;
            }

            if (!View.OwnIsland.Contains(cell))
            {
                return CommandResult.Rejected(RejectionReason.OutOfIsland);
            }

            if (_pendingFlags.Count < Match.Rules.FlagsPerSide)
            {
                _pendingFlags.Add(cell);
                return null;
            }

            if (SelectedSetupDefenseId != null && _pendingDefenses.Count < Match.Rules.InitialFreeDefenses)
            {
                _pendingDefenses.Add(new DefensePlacement(SelectedSetupDefenseId, cell));
                return null;
            }

            return CommandResult.Rejected(RejectionReason.InvalidSetup);
        }

        private CommandResult MoveSelectedRobot(PlayerView view, HexCoord destination)
        {
            var robot = view.OwnRobots.FirstOrDefault(r => r.RobotId == SelectedRobotId);
            if (robot == null)
            {
                SelectedRobotId = null;
                return CommandResult.Rejected(RejectionReason.UnknownRobot);
            }

            var definition = WeaponById(robot.WeaponId);
            // Los defensores conocidos bloquean el paso; los que no se han visto no se pueden esquivar.
            var knownGuards = new HashSet<HexCoord>(view.KnownEnemyGuards.Select(g => g.Position));
            var path = PathFinder.FindPath(view.EnemyIsland, definition, robot.Position, destination, knownGuards);
            if (path == null || path.Count == 0)
            {
                return CommandResult.Rejected(RejectionReason.InvalidPath);
            }

            // Si no alcanza este turno, avanza lo que pueda hacia el destino.
            var reachable = PathFinder.TruncateToBudget(view.EnemyIsland, definition, path, robot.MovementPointsLeft);
            if (reachable.Count == 0)
            {
                return CommandResult.Rejected(RejectionReason.InvalidPath);
            }

            var result = Match.Submit(HumanSide, new MoveRobotCommand(robot.RobotId, reachable.ToList()));
            if (View.OwnRobots.All(r => r.RobotId != robot.RobotId))
            {
                SelectedRobotId = null;
            }

            return result;
        }
    }
}
