using System.Collections.Generic;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using Flagstorm.Core.Tests.Support;

namespace Flagstorm.Core.Tests.Rules
{
    [TestFixture]
    public class MatchSetupAndTurnTests
    {
        [Test]
        public void BothSetups_StartMatchWithBlueAndInitialEnergy()
        {
            var match = MatchBuilder.NewPlaying();
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(match.CurrentSide, Is.EqualTo(Side.Blue));
            Assert.That(match.TurnNumber, Is.EqualTo(1));
            Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(6));
            Assert.That(match.EnergyOf(Side.Red), Is.EqualTo(0));
        }

        [Test]
        public void SetupTwiceBySameSide_IsRejected()
        {
            var match = MatchBuilder.NewSetup();
            var setup = MatchBuilder.DefaultSetupFor(match.Rules);
            Assert.That(match.Submit(Side.Blue, setup).IsAccepted, Is.True);
            Assert.That(match.Submit(Side.Blue, setup).Reason, Is.EqualTo(RejectionReason.InvalidSetup));
        }

        [TestCase("duplicada")]
        [TestCase("agua")]
        [TestCase("cuatro")]
        [TestCase("torretas")]
        [TestCase("torreta-sobre-bandera")]
        public void InvalidSetup_IsRejectedWithoutChangingPhase(string caseName)
        {
            var match = MatchBuilder.NewSetup();
            var result = match.Submit(Side.Blue, InvalidSetup(caseName));
            Assert.That(result.IsAccepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.InvalidSetup));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Setup));
        }

        [Test]
        public void CommandsOutsideTurnOrPhase_AreRejected()
        {
            var setup = MatchBuilder.NewSetup();
            Assert.That(setup.Submit(Side.Blue, new EndTurnCommand()).Reason, Is.EqualTo(RejectionReason.WrongPhase));

            var match = MatchBuilder.NewPlaying();
            Assert.That(match.Submit(Side.Red, new EndTurnCommand()).Reason, Is.EqualTo(RejectionReason.NotYourTurn));
            var lateSetup = new SetupCommand(MatchBuilder.DefaultFlags, new DefensePlacement[0]);
            Assert.That(match.Submit(Side.Blue, lateSetup).Reason, Is.EqualTo(RejectionReason.WrongPhase));
        }

        [Test]
        public void UnspentEnergy_CarriesOverUpToMax()
        {
            var match = MatchBuilder.NewPlaying();
            match.Submit(Side.Blue, new EndTurnCommand());
            Assert.That(match.EnergyOf(Side.Red), Is.EqualTo(6));
            match.Submit(Side.Red, new EndTurnCommand());
            Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(10));
            Assert.That(match.TurnNumber, Is.EqualTo(3));
            Assert.That(match.CurrentSide, Is.EqualTo(Side.Blue));
        }

        [Test]
        public void EndTurn_EmitsTurnEndedAndEnergyEvents()
        {
            var match = MatchBuilder.NewPlaying();
            var result = match.Submit(Side.Blue, new EndTurnCommand());
            Assert.That(result.Events.OfType<TurnEnded>().Single(), Is.EqualTo(new TurnEnded(Side.Red, 2)));
            Assert.That(result.Events.OfType<EnergyChanged>().Single(), Is.EqualTo(new EnergyChanged(Side.Red, 6)));
        }

        [Test]
        public void RejectedCommands_AreNotRecordedInHistory()
        {
            var match = MatchBuilder.NewPlaying();
            var before = match.History.Count;
            match.Submit(Side.Red, new EndTurnCommand());
            Assert.That(match.History.Count, Is.EqualTo(before));
            match.Submit(Side.Blue, new EndTurnCommand());
            Assert.That(match.History.Count, Is.EqualTo(before + 1));
        }

        [Test]
        public void NewMatch_ViewShowsOwnSetupOnly()
        {
            var view = MatchBuilder.NewPlaying().ViewFor(Side.Blue);
            Assert.That(view.Viewer, Is.EqualTo(Side.Blue));
            Assert.That(view.Energy, Is.EqualTo(6));
            Assert.That(view.OwnFlags, Is.EquivalentTo(MatchBuilder.DefaultFlags));
            Assert.That(view.OwnDefenses.Keys, Is.EquivalentTo(MatchBuilder.DefaultTurrets));
            Assert.That(view.OwnDefenses.Values.Select(d => d.HitPoints), Is.All.EqualTo(4));
        }

        private static readonly string[] StarterLoadout =
        {
            PrototypeArsenal.Drone, PrototypeArsenal.DroneShield, PrototypeArsenal.BeachRobot,
            PrototypeArsenal.BeachGuard,
        };

        private static Match NewStarterMatch() => new Match(
            TestIslands.Small(), TestIslands.Small(), RulesConfig.Default, PrototypeArsenal.All,
            StarterLoadout, StarterLoadout);

        private static SetupCommand SetupWith(params DefensePlacement[] defenses) => new SetupCommand(
            MatchBuilder.DefaultFlags, defenses);

        [Test]
        public void Setup_PlacesFreeDefensesFromLoadout()
        {
            var match = NewStarterMatch();
            var cell = new HexCoord(3, 1);
            var result = match.Submit(Side.Blue, SetupWith(new DefensePlacement(PrototypeArsenal.DroneShield, cell)));
            Assert.That(result.IsAccepted, Is.True);
            var defense = match.ViewFor(Side.Blue).OwnDefenses[cell];
            Assert.That(defense.WeaponId, Is.EqualTo(PrototypeArsenal.DroneShield));
            Assert.That(defense.HitPoints, Is.EqualTo(3));
            Assert.That(match.LoadoutOf(Side.Blue), Is.EqualTo(StarterLoadout));
            Assert.That(match.ViewFor(Side.Blue).Loadout, Is.EqualTo(StarterLoadout));
        }

        [Test]
        public void Setup_RejectsFreeDefenseNotInLoadout()
        {
            var result = NewStarterMatch().Submit(
                Side.Blue, SetupWith(new DefensePlacement(PrototypeArsenal.Turret, new HexCoord(3, 1))));
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.InvalidSetup));
        }

        [Test]
        public void Setup_RejectsAttackWeaponAsFreeDefense()
        {
            var result = NewStarterMatch().Submit(
                Side.Blue, SetupWith(new DefensePlacement(PrototypeArsenal.BeachRobot, new HexCoord(3, 1))));
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.InvalidSetup));
        }

        [Test]
        public void Setup_PlacesFreeGuard()
        {
            var match = NewStarterMatch();
            match.Submit(Side.Blue, SetupWith(new DefensePlacement(PrototypeArsenal.BeachGuard, new HexCoord(3, 1))));
            var guard = match.ViewFor(Side.Blue).OwnGuards.Single();
            Assert.That(guard.Position, Is.EqualTo(new HexCoord(3, 1)));
            Assert.That(guard.HitPoints, Is.EqualTo(4));
        }

        [Test]
        public void Command_WithWeaponOutsideLoadout_IsRejected()
        {
            var match = NewStarterMatch();
            match.Submit(Side.Blue, SetupWith());
            match.Submit(Side.Red, SetupWith());
            var energy = match.EnergyOf(Side.Blue);
            var result = match.Submit(Side.Blue, new FireMissileCommand(PrototypeArsenal.StandardMissile, new HexCoord(1, 1)));
            Assert.That(result.Reason, Is.EqualTo(RejectionReason.NotInLoadout));
            Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(energy));
        }

        private static SetupCommand InvalidSetup(string caseName)
        {
            var flags = MatchBuilder.DefaultFlags.ToList();
            var turrets = MatchBuilder.DefaultFreeDefenses.ToList();
            switch (caseName)
            {
                case "duplicada":
                    flags[4] = flags[0];
                    break;
                case "agua":
                    flags[4] = new HexCoord(10, 10);
                    break;
                case "cuatro":
                    flags.RemoveAt(4);
                    break;
                case "torretas":
                    turrets.Add(new DefensePlacement(PrototypeArsenal.Turret, new HexCoord(1, 3)));
                    break;
                case "torreta-sobre-bandera":
                    turrets[0] = new DefensePlacement(PrototypeArsenal.Turret, flags[0]);
                    break;
            }

            return new SetupCommand(flags, turrets);
        }
    }
}
