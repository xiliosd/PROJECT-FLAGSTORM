using System.Collections.Generic;
using System.Linq;
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
            var lateSetup = new SetupCommand(MatchBuilder.DefaultFlags, new HexCoord[0]);
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
            Assert.That(view.OwnTurrets.Keys, Is.EquivalentTo(MatchBuilder.DefaultTurrets));
            Assert.That(view.OwnTurrets.Values, Is.All.EqualTo(4));
        }

        private static SetupCommand InvalidSetup(string caseName)
        {
            var flags = MatchBuilder.DefaultFlags.ToList();
            var turrets = MatchBuilder.DefaultTurrets.ToList();
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
                    turrets.Add(new HexCoord(1, 3));
                    break;
                case "torreta-sobre-bandera":
                    turrets[0] = flags[0];
                    break;
            }

            return new SetupCommand(flags, turrets);
        }
    }
}
