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
        private readonly List<HexCoord> _pendingTurrets = new List<HexCoord>();
        private int _aiCommandsThisTurn;

        public MatchSession(Match match, IReadOnlyList<WeaponDefinition> arsenal, BasicAiPlayer ai)
        {
            Match = match;
            Arsenal = arsenal;
            _ai = ai;
            Match.Submit(AiSide, _ai.ChooseSetup(Match.ViewFor(AiSide)));
        }

        public Match Match { get; }

        public IReadOnlyList<WeaponDefinition> Arsenal { get; }

        public PlayerView View => Match.ViewFor(HumanSide);

        public IReadOnlyList<HexCoord> PendingFlags => _pendingFlags;

        public IReadOnlyList<HexCoord> PendingTurrets => _pendingTurrets;

        public string SelectedWeaponId { get; private set; }

        public int? SelectedRobotId { get; private set; }

        public bool IsAiTurn => Match.Phase == MatchPhase.Playing && Match.CurrentSide == AiSide;

        public bool IsHumanTurn => Match.Phase == MatchPhase.Playing && Match.CurrentSide == HumanSide;

        public static MatchSession CreateDefault(uint seed)
        {
            var match = new Match(
                PrototypeIslands.CreateDefault(),
                PrototypeIslands.CreateDefault(),
                RulesConfig.Default,
                PrototypeArsenal.All);
            return new MatchSession(match, PrototypeArsenal.All, new BasicAiPlayer(new SeededRandom(seed)));
        }

        public WeaponDefinition WeaponById(string weaponId) => Arsenal.FirstOrDefault(w => w.Id == weaponId);

        // Devuelve null si el toque solo cambió la selección; un resultado si se intentó un comando.
        public CommandResult TapOwnCell(HexCoord cell)
        {
            if (Match.Phase == MatchPhase.Setup)
            {
                return ToggleSetupCell(cell);
            }

            var weapon = WeaponById(SelectedWeaponId);
            if (!IsHumanTurn || weapon == null)
            {
                return null;
            }

            return weapon.Category == WeaponCategory.Defend
                ? Match.Submit(HumanSide, new BuildDefenseCommand(weapon.Id, cell))
                : CommandResult.Rejected(RejectionReason.OutOfIsland);
        }

        public CommandResult ConfirmSetup()
        {
            var result = Match.Submit(HumanSide, new SetupCommand(_pendingFlags.ToList(), _pendingTurrets.ToList()));
            if (result.IsAccepted)
            {
                _pendingFlags.Clear();
                _pendingTurrets.Clear();
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
                    return CommandResult.Rejected(RejectionReason.OutOfIsland);
                default:
                    return null;
            }
        }

        public CommandResult EndTurn()
        {
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

            _aiCommandsThisTurn++;
            var command = _aiCommandsThisTurn > MaxAiCommandsPerTurn
                ? new EndTurnCommand()
                : _ai.NextCommand(Match.ViewFor(AiSide), Arsenal);
            var result = Match.Submit(AiSide, command);
            return result.IsAccepted ? result : Match.Submit(AiSide, new EndTurnCommand());
        }

        private CommandResult ToggleSetupCell(HexCoord cell)
        {
            if (_pendingFlags.Remove(cell) || _pendingTurrets.Remove(cell))
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

            if (_pendingTurrets.Count < Match.Rules.InitialFreeTurrets)
            {
                _pendingTurrets.Add(cell);
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
            var path = PathFinder.FindPath(view.EnemyIsland, definition, robot.Position, destination);
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
