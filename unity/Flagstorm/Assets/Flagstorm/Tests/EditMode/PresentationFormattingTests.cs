using System;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using NUnit.Framework;

namespace Flagstorm.Presentation.Tests
{
    public class PresentationFormattingTests
    {
        [Test]
        public void Modifier_UsesTypographicMinus() =>
            Assert.That(ModifierLabelFormatter.Format(-50), Is.EqualTo("−50 %"));

        [Test]
        public void Modifier_PositiveAndZero()
        {
            Assert.That(ModifierLabelFormatter.Format(40), Is.EqualTo("+40 %"));
            Assert.That(ModifierLabelFormatter.Format(0), Is.EqualTo(string.Empty));
        }

        [Test]
        public void EveryTerrainHasLabelAndEveryRejectionHasText()
        {
            foreach (TerrainType terrain in Enum.GetValues(typeof(TerrainType)))
            {
                Assert.That(HoloPalette.TerrainLabel(terrain), Is.Not.Empty, terrain.ToString());
            }

            foreach (RejectionReason reason in Enum.GetValues(typeof(RejectionReason)))
            {
                if (reason != RejectionReason.None)
                {
                    Assert.That(RejectionTextFormatter.Format(reason), Is.Not.Empty, reason.ToString());
                }
            }
        }

        [Test]
        public void TerrainLabels_AreDistinct()
        {
            var labels = new System.Collections.Generic.HashSet<string>();
            foreach (TerrainType terrain in Enum.GetValues(typeof(TerrainType)))
            {
                Assert.That(labels.Add(HoloPalette.TerrainLabel(terrain)), Is.True, terrain.ToString());
            }
        }

        [Test]
        public void CaptureEvent_TextDependsOnViewer()
        {
            var captured = new FlagCaptured(Side.Blue, new HexCoord(1, 1), 3);
            Assert.That(EventTextFormatter.Format(captured, Side.Blue, 5), Is.EqualTo("¡Capturaste una bandera! (3/5)"));
            Assert.That(EventTextFormatter.Format(captured, Side.Red, 5), Is.EqualTo("El rival capturó una de tus banderas (3/5)"));
        }

        [Test]
        public void EveryEventType_HasText()
        {
            var cell = new HexCoord(0, 0);
            var events = new IMatchEvent[]
            {
                new SetupCompleted(Side.Red),
                new MatchStarted(Side.Blue),
                new TurnEnded(Side.Red, 2),
                new CellsRevealed(Side.Blue, new[] { cell }),
                new MissileImpact(Side.Blue, "misil-estandar", cell, new[] { cell }),
                new DefenseDamaged(Side.Red, cell, "torreta", 2),
                new DefenseDestroyed(Side.Red, cell, "torreta"),
                new RobotDeployed(Side.Blue, 1, "robot-orugas", cell),
                new RobotMoved(Side.Blue, 1, new[] { cell }),
                new FlagCaptured(Side.Blue, cell, 1),
                new DecoyExposed(Side.Blue, cell),
                new MatchWon(Side.Blue),
                new DefenseBuilt(Side.Blue, cell, "torreta"),
                new TurretFired(Side.Red, cell, 1, 2),
                new RobotDestroyed(Side.Blue, 1, cell),
            };
            foreach (var matchEvent in events)
            {
                Assert.That(EventTextFormatter.Format(matchEvent, Side.Blue, 5), Is.Not.Null, matchEvent.GetType().Name);
            }

            // La energía ya se ve en el HUD; no ensucia el registro.
            Assert.That(EventTextFormatter.Format(new EnergyChanged(Side.Blue, 4), Side.Blue, 5), Is.Null);
        }
    }
}
