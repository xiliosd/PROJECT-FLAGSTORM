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

        public Match(IslandMap blueIsland, IslandMap redIsland, RulesConfig rules, IReadOnlyList<WeaponDefinition> arsenal)
        {
            if (blueIsland == null || redIsland == null)
            {
                throw new ArgumentNullException(blueIsland == null ? nameof(blueIsland) : nameof(redIsland));
            }

            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _arsenal = (arsenal ?? throw new ArgumentNullException(nameof(arsenal))).ToDictionary(w => w.Id);
            _sides = new Dictionary<Side, SideState>
            {
                [Side.Blue] = new SideState(blueIsland),
                [Side.Red] = new SideState(redIsland),
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
                OwnFlags = Sorted(own.Flags),
                OwnDecoys = Sorted(DefenseCells(own, PrototypeArsenal.Decoy)),
                OwnTurrets = TurretsIn(own, own.Defenses.Keys),
                RevealedEnemyCells = Sorted(own.RevealedEnemyCells),
                EnemySignals = Sorted(own.RevealedEnemyCells.Where(c => IsSignal(enemy, c))),
                KnownEnemyTurrets = TurretsIn(enemy, own.RevealedEnemyCells),
                OwnRobots = own.Robots.OrderBy(r => r.RobotId).Select(r => r.ToInfo()).ToList(),
                EnemyRobotsOnOwnIsland = enemy.Robots.OrderBy(r => r.RobotId).Select(r => r.ToInfo()).ToList(),
                FlagsCapturedByViewer = own.FlagsCaptured,
                FlagsLostByViewer = enemy.FlagsCaptured,
            };
        }

        private static IReadOnlyList<HexCoord> Sorted(IEnumerable<HexCoord> cells) =>
            cells.OrderBy(c => c.R).ThenBy(c => c.Q).ToList();

        private static IEnumerable<HexCoord> DefenseCells(SideState side, string weaponId) =>
            side.Defenses.Where(d => d.Value.WeaponId == weaponId).Select(d => d.Key);

        private static bool IsSignal(SideState owner, HexCoord cell) =>
            owner.Flags.Contains(cell)
            || (owner.Defenses.TryGetValue(cell, out var defense) && defense.WeaponId == PrototypeArsenal.Decoy);

        private static IReadOnlyDictionary<HexCoord, int> TurretsIn(SideState owner, IEnumerable<HexCoord> visible)
        {
            var result = new SortedDictionary<HexCoord, int>(CellOrder.Instance);
            foreach (var cell in visible)
            {
                if (owner.Defenses.TryGetValue(cell, out var defense) && defense.WeaponId == PrototypeArsenal.Turret)
                {
                    result[cell] = defense.HitPoints;
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
                default:
                    // Provisional hasta que existan las reglas de este comando (tareas 5 a 7 del plan).
                    return CommandResult.Rejected(RejectionReason.WrongCategory);
            }
        }

        private CommandResult ApplySetup(Side side, SetupCommand setup)
        {
            var state = _sides[side];
            if (state.SetupDone || !IsValidSetup(state.Island, setup))
            {
                return CommandResult.Rejected(RejectionReason.InvalidSetup);
            }

            var turret = _arsenal[PrototypeArsenal.Turret];
            foreach (var flag in setup.Flags)
            {
                state.Flags.Add(flag);
            }

            foreach (var cell in setup.Turrets)
            {
                state.Defenses[cell] = new DefenseState(turret.Id, turret.HitPoints);
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

        private bool IsValidSetup(IslandMap island, SetupCommand setup)
        {
            if (setup.Flags == null || setup.Turrets == null)
            {
                return false;
            }

            var all = setup.Flags.Concat(setup.Turrets).ToList();
            return setup.Flags.Count == Rules.FlagsPerSide
                && setup.Turrets.Count <= Rules.InitialFreeTurrets
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
