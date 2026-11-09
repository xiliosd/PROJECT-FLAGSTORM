using System;
using System.Collections.Generic;

namespace Flagstorm.Core.Rules
{
    public static class MatchReplayer
    {
        public static Match Replay(Func<Match> createFresh, IReadOnlyList<RecordedCommand> history)
        {
            if (createFresh == null)
            {
                throw new ArgumentNullException(nameof(createFresh));
            }

            if (history == null)
            {
                throw new ArgumentNullException(nameof(history));
            }

            var match = createFresh();
            for (var i = 0; i < history.Count; i++)
            {
                var result = match.Submit(history[i].Side, history[i].Command);
                if (!result.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"El comando {i} ({history[i].Command}) fue rechazado al repetir: {result.Reason}.");
                }
            }

            return match;
        }
    }
}
