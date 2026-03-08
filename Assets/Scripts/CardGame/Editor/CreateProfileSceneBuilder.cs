using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace CardGame.Editor
{
    /// <summary>
    /// Crée la scène CreateProfile avec Canvas, formulaire nom, bouton Valider.
    /// Menu : CardGame > Créer scène CreateProfile
    /// </summary>
    public static class CreateProfileSceneBuilder
    {
        [MenuItem("CardGame/Créer scène CreateProfile")]
        public static void BuildCreateProfileScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem (déjà créé par DefaultGameObjects, vérifier)
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Panel formulaire
            var panelGO = new GameObject("PanelFormulaire");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRT = panelGO.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(400, 250);
            panelRT.anchoredPosition = Vector2.zero;
            var panelImg = panelGO.AddComponent<Image>();
            panelImg.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);

            // Titre
            var titreGO = new GameObject("Titre");
            titreGO.transform.SetParent(panelGO.transform, false);
            var titreRT = titreGO.AddComponent<RectTransform>();
            titreRT.anchorMin = new Vector2(0, 1);
            titreRT.anchorMax = new Vector2(1, 1);
            titreRT.pivot = new Vector2(0.5f, 1);
            titreRT.anchoredPosition = new Vector2(0, -20);
            titreRT.sizeDelta = new Vector2(0, 40);
            var titreTxt = titreGO.AddComponent<TextMeshProUGUI>();
            titreTxt.text = "Créer votre profil";
            titreTxt.fontSize = 28;
            titreTxt.alignment = TextAlignmentOptions.Center;

            // InputField
            var inputGO = new GameObject("InputNom");
            inputGO.transform.SetParent(panelGO.transform, false);
            var inputRT = inputGO.AddComponent<RectTransform>();
            inputRT.anchorMin = new Vector2(0.1f, 0.6f);
            inputRT.anchorMax = new Vector2(0.9f, 0.75f);
            inputRT.anchoredPosition = Vector2.zero;
            inputRT.sizeDelta = Vector2.zero;
            var inputField = inputGO.AddComponent<TMP_InputField>();
            var inputBg = inputGO.AddComponent<Image>();
            inputBg.color = new Color(0.2f, 0.2f, 0.22f, 1f);
            var inputTextGO = new GameObject("Text");
            inputTextGO.transform.SetParent(inputGO.transform, false);
            var inputTextRT = inputTextGO.AddComponent<RectTransform>();
            inputTextRT.anchorMin = Vector2.zero;
            inputTextRT.anchorMax = Vector2.one;
            inputTextRT.offsetMin = new Vector2(10, 5);
            inputTextRT.offsetMax = new Vector2(-10, -5);
            var inputText = inputTextGO.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 18;
            inputText.color = Color.white;
            inputField.textViewport = inputRT;
            inputField.textComponent = inputText;
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputGO.transform, false);
            var placeholderRT = placeholderGO.AddComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = new Vector2(10, 5);
            placeholderRT.offsetMax = new Vector2(-10, -5);
            var placeholderTxt = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderTxt.text = "Entrez votre pseudonyme...";
            placeholderTxt.fontSize = 18;
            placeholderTxt.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            inputField.placeholder = placeholderTxt;

            // Text Erreur
            var errGO = new GameObject("TextErreur");
            errGO.transform.SetParent(panelGO.transform, false);
            var errRT = errGO.AddComponent<RectTransform>();
            errRT.anchorMin = new Vector2(0.1f, 0.45f);
            errRT.anchorMax = new Vector2(0.9f, 0.55f);
            errRT.anchoredPosition = Vector2.zero;
            errRT.sizeDelta = Vector2.zero;
            var errTxt = errGO.AddComponent<TextMeshProUGUI>();
            errTxt.text = "";
            errTxt.fontSize = 14;
            errTxt.color = new Color(1f, 0.4f, 0.4f);
            errTxt.alignment = TextAlignmentOptions.Center;
            errGO.SetActive(false);

            // Bouton Valider
            var btnGO = new GameObject("ButtonValider");
            btnGO.transform.SetParent(panelGO.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.25f, 0.15f);
            btnRT.anchorMax = new Vector2(0.75f, 0.35f);
            btnRT.anchoredPosition = Vector2.zero;
            btnRT.sizeDelta = Vector2.zero;
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.5f, 0.8f, 1f);
            var btn = btnGO.AddComponent<Button>();
            var btnTextGO = new GameObject("Text");
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var btnTextRT = btnTextGO.AddComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero;
            btnTextRT.anchorMax = Vector2.one;
            btnTextRT.offsetMin = Vector2.zero;
            btnTextRT.offsetMax = Vector2.zero;
            var btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
            btnText.text = "Valider";
            btnText.fontSize = 22;
            btnText.alignment = TextAlignmentOptions.Center;

            // CreateProfileController
            var ctrlGO = new GameObject("CreateProfileController");
            var ctrl = ctrlGO.AddComponent<CardGame.Unity.CreateProfileController>();

            // Assigner via SerializedObject (les champs sont privés)
            var so = new SerializedObject(ctrl);
            so.FindProperty("_panelFormulaire").objectReferenceValue = panelGO;
            so.FindProperty("_inputNom").objectReferenceValue = inputField;
            so.FindProperty("_buttonValider").objectReferenceValue = btn;
            so.FindProperty("_textErreur").objectReferenceValue = errTxt;
            so.ApplyModifiedPropertiesWithoutUndo();

            var path = "Assets/Scenes/WindowsScene/CreateProfile.unity";
            if (!System.IO.Directory.Exists("Assets/Scenes/WindowsScene"))
                System.IO.Directory.CreateDirectory("Assets/Scenes/WindowsScene");
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            Debug.Log($"[CreateProfileSceneBuilder] Scène créée : {path}");
        }

        [MenuItem("CardGame/Créer scène Androide_CreateProfile")]
        public static void BuildAndroideCreateProfileScene()
        {
            var windowsPath = "Assets/Scenes/WindowsScene/CreateProfile.unity";
            var androidPath = "Assets/Scenes/AndroideScene/Androide_CreateProfile.unity";
            if (!System.IO.File.Exists(windowsPath))
            {
                Debug.LogError("[CreateProfileSceneBuilder] Créez d'abord CreateProfile (CardGame > Créer scène CreateProfile)");
                return;
            }
            if (!System.IO.Directory.Exists("Assets/Scenes/AndroideScene"))
                System.IO.Directory.CreateDirectory("Assets/Scenes/AndroideScene");
            AssetDatabase.CopyAsset(windowsPath, androidPath);
            var scene = EditorSceneManager.OpenScene(androidPath, OpenSceneMode.Single);
            var ctrl = Object.FindObjectOfType<CardGame.Unity.CreateProfileController>();
            if (ctrl != null)
            {
                var so = new SerializedObject(ctrl);
                so.FindProperty("_android").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.Refresh();
            Debug.Log($"[CreateProfileSceneBuilder] Scène Android créée : {androidPath}");
        }

        [MenuItem("CardGame/Configurer Build Settings (CreateProfile en premier)")]
        public static void ConfigureBuildSettings()
        {
            var createProfilePath = "Assets/Scenes/WindowsScene/CreateProfile.unity";
            var androidCreateProfilePath = "Assets/Scenes/AndroideScene/Androide_CreateProfile.unity";
            var existing = EditorBuildSettings.scenes;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();

            // Ajouter CreateProfile en premier (Windows) s'il existe
            if (System.IO.File.Exists(createProfilePath))
            {
                list.Add(new EditorBuildSettingsScene(createProfilePath, true));
            }
            // Ajouter Androide_CreateProfile (Android) s'il existe
            if (System.IO.File.Exists(androidCreateProfilePath))
            {
                list.Add(new EditorBuildSettingsScene(androidCreateProfilePath, true));
            }

            foreach (var s in existing)
            {
                if (s.path.EndsWith("CreateProfile.unity") || s.path.EndsWith("Androide_CreateProfile.unity"))
                    continue;
                list.Add(s);
            }
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[CreateProfileSceneBuilder] Build Settings : CreateProfile ajouté en premier.");
        }
    }
}
