using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Flagstorm.Core.Map;

namespace Flagstorm.Core.Rules
{
    // Huella del estado: permite comprobar que una repetición (o un rival remoto) llegó al mismo estado.
    public sealed partial class Match
    {
        public string ComputeStateHash()
        {
            var text = new StringBuilder();
            Append(text, "phase", Phase.ToString());
            Append(text, "turn", TurnNumber.ToString(CultureInfo.InvariantCulture));
            Append(text, "current", CurrentSide.ToString());
            Append(text, "winner", Winner?.ToString() ?? "-");
            Append(text, "nextRobot", _nextRobotId.ToString(CultureInfo.InvariantCulture));
            foreach (var side in new[] { Side.Blue, Side.Red })
            {
                var state = _sides[side];
                Append(text, "side", side.ToString());
                Append(text, "setup", state.SetupDone ? "1" : "0");
                Append(text, "energy", state.Energy.ToString(CultureInfo.InvariantCulture));
                Append(text, "captured", state.FlagsCaptured.ToString(CultureInfo.InvariantCulture));
                Append(text, "flags", Cells(state.Flags));
                Append(text, "revealed", Cells(state.RevealedEnemyCells));
                foreach (var defense in state.Defenses.OrderBy(d => d.Key, CellOrder.Instance))
                {
                    Append(text, "defense", $"{defense.Key}:{defense.Value.WeaponId}:{defense.Value.HitPoints}");
                }

                foreach (var robot in state.Robots.OrderBy(r => r.RobotId))
                {
                    Append(
                        text,
                        "robot",
                        $"{robot.RobotId}:{robot.WeaponId}:{robot.Position}:"
                        + $"{robot.HitPoints}:{robot.MovementPointsLeft}");
                }
            }

            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
                return string.Concat(bytes.Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static void Append(StringBuilder text, string key, string value) =>
            text.Append(key).Append('=').Append(value).Append('\n');

        private static string Cells(IEnumerable<HexCoord> cells) =>
            string.Join(";", cells.OrderBy(c => c, CellOrder.Instance).Select(c => c.ToString()));
    }
}
