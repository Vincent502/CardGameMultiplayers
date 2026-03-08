using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace CardGame.Unity
{
    /// <summary>
    /// Contrôleur de la scène CreateProfile. Si le profil existe, affiche "Bienvenue [nom]" puis charge le Menu.
    /// Sinon affiche le formulaire de création (nom) et charge le Menu après validation.
    /// </summary>
    public class CreateProfileController : MonoBehaviour
    {
        [Header("Plateforme")]
        [SerializeField] [Tooltip("Si true, charge les scènes Android. Cochez pour la build Android.")]
        private bool _android;

        [Header("UI Création profil")]
        [SerializeField] private GameObject _panelFormulaire;
        [SerializeField] private TMP_InputField _inputNom;
        [SerializeField] private Button _buttonValider;
        [SerializeField] private TMP_Text _textErreur;

        [Header("UI Bienvenue (optionnel — créé à la volée si absent)")]
        [SerializeField] private GameObject _panelBienvenue;
        [SerializeField] private TMP_Text _textBienvenue;

        private const float DelaiBienvenue = 3f;

        private void Awake()
        {
            MenuController.SceneNames.SetAndroidMode(_android);
        }

        private void Start()
        {
            if (ProfileManager.ProfilExiste())
            {
                ShowBienvenue();
                return;
            }

            if (_panelFormulaire != null)
                _panelFormulaire.SetActive(true);

            if (_buttonValider != null)
                _buttonValider.onClick.AddListener(OnValider);

            if (_textErreur != null)
                _textErreur.gameObject.SetActive(false);

            if (_inputNom != null)
            {
                _inputNom.characterLimit = 20;
                var ph = _inputNom.placeholder as TMP_Text;
                if (ph != null) ph.text = "Entrez votre pseudonyme...";
            }
        }

        private void ShowBienvenue()
        {
            var profile = ProfileManager.LoadProfile();
            string nom = profile?.nom ?? "Joueur";

            if (_panelFormulaire != null)
                _panelFormulaire.SetActive(false);

            if (_panelBienvenue != null)
            {
                _panelBienvenue.SetActive(true);
                if (_textBienvenue != null)
                    _textBienvenue.text = $"Bienvenue {nom}";
            }
            else
            {
                CreateBienvenuePanel(nom);
            }

            Invoke(nameof(GoToMenu), DelaiBienvenue);
        }

        private void CreateBienvenuePanel(string nom)
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var panelGO = new GameObject("PanelBienvenue");
            panelGO.transform.SetParent(canvas.transform, false);
            var panelRT = panelGO.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(400, 150);
            panelRT.anchoredPosition = Vector2.zero;
            panelGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 0.95f);

            var textGO = new GameObject("TextBienvenue");
            textGO.transform.SetParent(panelGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0.1f, 0.1f);
            textRT.anchorMax = new Vector2(0.9f, 0.9f);
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = $"Bienvenue {nom}";
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void GoToMenu()
        {
            SceneManager.LoadScene(MenuController.SceneNames.Menu);
        }

        private void OnValider()
        {
            string nom = _inputNom != null ? _inputNom.text : "";
            if (string.IsNullOrWhiteSpace(nom))
            {
                if (_textErreur != null)
                {
                    _textErreur.text = "Veuillez entrer un nom.";
                    _textErreur.gameObject.SetActive(true);
                }
                return;
            }

            ProfileManager.CreerProfil(nom);
            SceneManager.LoadScene(MenuController.SceneNames.Menu);
        }
    }
}
