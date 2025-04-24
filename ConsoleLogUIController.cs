using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Steelbox.Console
{
    public class ConsoleLogUIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _dateText;
        [SerializeField] private TMP_Text _contentText;
        [SerializeField] private Image _badgeImage;

        public void Initialize(string date, string content, ConsoleLogType logType)
        {
            _dateText.SetText(date);
            string colorPrefix = logType switch
            {
                ConsoleLogType.System => "<color=#b7b7b7>",
                ConsoleLogType.Error => "<color=red>",
                ConsoleLogType.Warning => "<color=yellow>",
                _ => string.Empty
            };
            string colorSuffix = colorPrefix.Length > 0 ? "</color>" : string.Empty;
            
            _contentText.SetText(colorPrefix + content + colorSuffix);
            _badgeImage.gameObject.SetActive(logType == ConsoleLogType.System);
        }
    }
}