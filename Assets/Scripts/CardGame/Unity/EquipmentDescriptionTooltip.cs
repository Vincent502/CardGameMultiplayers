using UnityEngine;
using UnityEngine.EventSystems;
using CardGame.Core;

namespace CardGame.Unity
{
    /// <summary>
    /// Affiche la description d'un équipement : PC = survol souris, Mobile = clic pour afficher/masquer.
    /// À placer sur chaque label d'équipement. Nécessite un Graphic (Image ou TMP_Text) pour recevoir les événements.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Graphic))]
    public class EquipmentDescriptionTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private string _cardName;
        private string _cardDescription;

        private static EquipmentTooltipPanel _panel;

        /// <summary>Configure le tooltip avec les données de la carte.</summary>
        public void SetCardData(CardData data)
        {
            _cardName = data.Name;
            _cardDescription = data.Description ?? "";
        }

        /// <summary>Référence au panneau tooltip (assigné par GameUI).</summary>
        public static void SetTooltipPanel(EquipmentTooltipPanel panel)
        {
            _panel = panel;
        }

        /// <summary>Masque le tooltip (ex. quand les labels sont recréés).</summary>
        public static void HideTooltip()
        {
            if (_panel != null) _panel.Hide();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_panel != null && !Application.isMobilePlatform)
                _panel.Show(_cardName, _cardDescription, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_panel != null && !Application.isMobilePlatform)
                _panel.ScheduleHide(3f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_panel == null) return;
            if (Application.isMobilePlatform)
                _panel.Toggle(_cardName, _cardDescription, eventData.position);
            else
                _panel.Hide();
        }
    }
}
