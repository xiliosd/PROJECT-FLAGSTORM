using System;
using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Rules
{
    // Partida: valida y aplica comandos. Las reglas de cada tipo de acción están en las otras partes parciales.
    public sealed partial class Match
    {
        private readonly Dictionary<Side, SideState> _sides;
        private readonly Dictionary<string, WeaponDefinition> _arsenal;
        private readonly List<RecordedCommand> _history = new List<RecordedCommand>();

        // Sin loadouts explícitos, ambos bandos disponen del catálogo completo.
        public Match(
            IslandMap blueIsland, IslandMap redIsland, RulesConfig rules, IReadOnlyList<WeaponDefinition> catalog)
            : this(blueIsland, redIsland, rules, catalog, AllIds(catalog), AllIds(catalog))
        {
        }

        public Match(
            IslandMap blueIsland,
            IslandMap redIsland,
            RulesConfig rules,
            IReadOnlyList<WeaponDefinition> catalog,
            IReadOnlyList<string> blueLoadout,
            IReadOnlyList<string> redLoadout)
        {
            if (blueIsland == null || redIsland == null)
            {
                throw new ArgumentNullException(blueIsland == null ? nameof(blueIsland) : nameof(redIsland));
            }

            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _arsenal = (catalog ?? throw new ArgumentNullException(nameof(catalog))).ToDictionary(w => w.Id);
            _sides = new Dictionary<Side, SideState>
            {
                [Side.Blue] = new SideState(blueIsland, CopyLoadout(blueLoadout, nameof(blueLoadout))),
                [Side.Red] = new SideState(redIsland, CopyLoadout(redLoadout, nameof(redLoadout))),
            };
            Phase = MatchPhase.Setup;
            TurnNumber = 1;
        }

        public RulesConfig Rules { get; }

        public MatchPhase Phase { get; private set; }

        public Side CurrentSide { get; private set; }

        public Side? Winner { get; private set; }

        public int TurnNumber { get; private set; }

        public IReadOnlyList<RecordedCommand> History => _history;

        public int EnergyOf(Side side) => _sides[side].Energy;

        public IslandMap IslandOf(Side side) => _sides[side].Island;

        public IReadOnlyList<string> LoadoutOf(Side side) => _sides[side].Loadout;

        public CommandResult Submit(Side side, IMatchCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var result = Dispatch(side, command);
            if (result.IsAccepted)
            {
                _history.Add(new RecordedCommand(side, command));
            }

            return result;
        }

        public PlayerView ViewFor(Side side)
        {
            var own = _sides[side];
            var enemy = _sides[side.Opponent()];
            return new PlayerView
            {
                Viewer = side,
                Phase = Phase,
                CurrentSide = CurrentSide,
                TurnNumber = TurnNumber,
                Energy = own.Energy,
                Rules = Rules,
                OwnIsland = own.Island,
                EnemyIsland = enemy.Island,
                Loadout = own.Loadout,
                OwnDefenses = DefensesIn(own),
                OwnFlags = Sorted(own.Flags),
                OwnGuards = own.Guards.OrderBy(g => g.RobotId).Select(g => g.ToInfo()).ToList(),
                KnownEnemyGuards = KnownGuardsOf(own, enemy),
                KnownEnemyDefenses = KnownDefensesOf(own, enemy),
                RevealedEnemyCells = Sorted(own.RevealedEnemyCells),
                EnemySignals = Sorted(own.RevealedEnemyCells.Where(c => IsSignal(enemy, c))),
                OwnRobots = own.Robots.OrderBy(r => r.RobotId).Select(r => r.ToInfo()).ToList(),
                EnemyRobotsOnOwnIsland = enemy.Robots.OrderBy(r => r.RobotId).Select(r => r.ToInfo()).ToList(),
                FlagsCapturedByViewer = own.FlagsCaptured,
                FlagsLostByViewer = enemy.FlagsCaptured,
            };
        }

        private static IReadOnlyList<string> AllIds(IReadOnlyList<WeaponDefinition> catalog) =>
            (catalog ?? throw new ArgumentNullException(nameof(catalog))).Select(w => w.Id).ToList();

        private static IReadOnlyList<string> CopyLoadout(IReadOnlyList<string> loadout, string name) =>
            (loadout ?? throw new ArgumentNullException(name)).ToList();

        private static IReadOnlyList<HexCoord> Sorted(IEnumerable<HexCoord> cells) =>
            cells.OrderBy(c => c.R).ThenBy(c => c.Q).ToList();

        private static bool IsSignal(SideState owner, HexCoord cell) =>
            owner.Flags.Contains(cell)
            || (owner.Defenses.TryGetValue(cell, out var defense) && defense.WeaponId == PrototypeArsenal.Decoy);

        private static IReadOnlyDictionary<HexCoord, DefenseInfo> DefensesIn(SideState owner)
        {
            var result = new SortedDictionary<HexCoord, DefenseInfo>(CellOrder.Instance);
            foreach (var defense in owner.Defenses)
            {
                result[defense.Key] = new DefenseInfo(defense.Value.WeaponId, defense.Value.HitPoints);
            }

            return result;
        }

        // Un señuelo se hace pasar por bandera: nunca se expone como defensa, ni siquiera en una celda revelada.
        // Una defensa fija se ve si su celda está revelada o si un robot propio está a una casilla (§3.10).
        private static IReadOnlyDictionary<HexCoord, DefenseInfo> KnownDefensesOf(SideState viewer, SideState owner)
        {
            var result = new SortedDictionary<HexCoord, DefenseInfo>(CellOrder.Instance);
            foreach (var entry in owner.Defenses)
            {
                var known = viewer.RevealedEnemyCells.Contains(entry.Key)
                    || viewer.Robots.Any(r => r.Position.DistanceTo(entry.Key) <= 1);
                if (known && entry.Value.WeaponId != PrototypeArsenal.Decoy)
                {
                    result[entry.Key] = new DefenseInfo(entry.Value.WeaponId, entry.Value.HitPoints);
                }
            }

            return result;
        }

        private CommandResult Dispatch(Side side, IMatchCommand command)
        {
            if (Phase == MatchPhase.Finished)
            {
                return CommandResult.Rejected(RejectionReason.MatchFinished);
            }

            if (command is SetupCommand setup)
            {
                return Phase == MatchPhase.Setup
                    ? ApplySetup(side, setup)
                    : CommandResult.Rejected(RejectionReason.WrongPhase);
            }

            if (Phase != MatchPhase.Playing)
            {
                return CommandResult.Rejected(RejectionReason.WrongPhase);
            }

            if (side != CurrentSide)
            {
                return CommandResult.Rejected(RejectionReason.NotYourTurn);
            }

            switch (command)
            {
                case EndTurnCommand _:
                    return ApplyEndTurn(side);
                case LaunchDroneCommand drone:
                    return ApplyDrone(side, drone);
                case FireMissileCommand missile:
                    return ApplyMissile(side, missile);
                case DeployRobotCommand deploy:
                    return ApplyDeploy(side, deploy);
                case MoveRobotCommand move:
                    return ApplyMove(side, move);
                case BuildDefenseCommand build:
                    return IsGuardWeapon(build.WeaponId)
                        ? ApplyPlaceGuard(side, build)
                        : ApplyBuildDefense(side, build);
                default:
                    throw new ArgumentException($"Comando no soportado: {command.GetType().Name}", nameof(command));
            }
        }

        // Valida arma, categoría y energía; no gasta nada.
        private RejectionReason CheckWeapon(
            Side side, string weaponId, WeaponCategory category, out WeaponDefinition weapon)
        {
            if (weaponId == null || !_arsenal.TryGetValue(weaponId, out weapon))
            {
                weapon = null;
                return RejectionReason.UnknownWeapon;
            }

            if (!_sides[side].Loadout.Contains(weaponId))
            {
                return RejectionReason.NotInLoadout;
            }

            if (weapon.Category != category)
            {
                return RejectionReason.WrongCategory;
            }

            return weapon.EnergyCost > _sides[side].Energy ? RejectionReason.NotEnoughEnergy : RejectionReason.None;
        }

        private CommandResult ApplySetup(Side side, SetupCommand setup)
        {
            var state = _sides[side];
            if (state.SetupDone || !IsValidSetup(state, setup))
            {
                return CommandResult.Rejected(RejectionReason.InvalidSetup);
            }

            foreach (var flag in setup.Flags)
            {
                state.Flags.Add(flag);
            }

            foreach (var placement in setup.FreeDefenses)
            {
                var defense = _arsenal[placement.WeaponId];
                if (defense.Category == WeaponCategory.Guard)
                {
                    state.Guards.Add(new GuardState(_nextRobotId++, defense.Id, placement.Cell, defense.HitPoints));
                    continue;
                }

                state.Defenses[placement.Cell] = new DefenseState(defense.Id, defense.HitPoints);
            }

            state.SetupDone = true;
            var events = new List<IMatchEvent> { new SetupCompleted(side) };
            if (_sides.Values.All(s => s.SetupDone))
            {
                Phase = MatchPhase.Playing;
                CurrentSide = Side.Blue;
                events.Add(new MatchStarted(Side.Blue));
                events.Add(StartTurnOf(Side.Blue));
            }

            return CommandResult.Accepted(events);
        }

        private bool IsValidSetup(SideState state, SetupCommand setup)
        {
            if (setup.Flags == null || setup.FreeDefenses == null || setup.FreeDefenses.Any(p => p == null))
            {
                return false;
            }

            var allowed = setup.FreeDefenses.All(
                p => state.Loadout.Contains(p.WeaponId)
                    && _arsenal.TryGetValue(p.WeaponId, out var weapon)
                    && (weapon.Category == WeaponCategory.Defend || weapon.Category == WeaponCategory.Guard));
            var island = state.Island;
            var all = setup.Flags.Concat(setup.FreeDefenses.Select(p => p.Cell)).ToList();
            return allowed
                && setup.Flags.Count == Rules.FlagsPerSide
                && setup.FreeDefenses.Count <= Rules.InitialFreeDefenses
                && all.All(island.Contains)
                && all.Distinct().Count() == all.Count;
        }

        private CommandResult ApplyEndTurn(Side side)
        {
            var events = new List<IMatchEvent>();
            OnTurnEnding(side, events);
            var next = side.Opponent();
            CurrentSide = next;
            TurnNumber++;
            events.Add(new TurnEnded(next, TurnNumber));
            events.Add(StartTurnOf(next));
            return CommandResult.Accepted(events);
        }

        private EnergyChanged StartTurnOf(Side side)
        {
            var state = _sides[side];
            state.Energy = Math.Min(Rules.MaxEnergy, state.Energy + Rules.EnergyPerTurn);
            OnTurnStarting(side);
            return new EnergyChanged(side, state.Energy);
        }

        // Puntos de extensión para las reglas de robots y defensas.
        partial void OnTurnEnding(Side side, List<IMatchEvent> events);

        partial void OnTurnStarting(Side side);
    }
}
