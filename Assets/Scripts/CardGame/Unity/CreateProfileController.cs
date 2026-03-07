using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace CardGame.Unity
{
    /// <summary>
    /// Contrôleur de la scène CreateProfile. Si le profil existe, redirige vers le Menu.
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

        private void Awake()
        {
            MenuController.SceneNames.SetAndroidMode(_android);
        }

        private void Start()
        {
            if (ProfileManager.ProfilExiste())
            {
                SceneManager.LoadScene(MenuController.SceneNames.Menu);
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
