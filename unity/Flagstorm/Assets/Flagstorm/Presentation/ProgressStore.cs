using UnityEngine;

namespace Flagstorm.Presentation
{
    // Guarda solo el número de victorias (sin datos personales); de él depende el arsenal desbloqueado.
    public sealed class ProgressStore
    {
        public const string WinsKey = "flagstorm.victorias";

        public int Wins => PlayerPrefs.GetInt(WinsKey, 0);

        public void RecordWin()
        {
            PlayerPrefs.SetInt(WinsKey, Wins + 1);
            PlayerPrefs.Save();
        }

        public void Reset()
        {
            PlayerPrefs.DeleteKey(WinsKey);
            PlayerPrefs.Save();
        }
    }
}
