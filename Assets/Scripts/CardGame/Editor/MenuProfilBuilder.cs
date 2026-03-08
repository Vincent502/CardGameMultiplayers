using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace CardGame.Editor
{
    /// <summary>
    /// Ajoute le bouton Profil et le panel Profil au Menu.
    /// Menu : CardGame > Ajouter Profil au Menu
    /// </summary>
    public static class MenuProfilBuilder
    {
        [MenuItem("CardGame/Ajouter Profil au Menu")]
        public static void AddProfilToMenu()
        {
            AddProfilToScene("Assets/Scenes/WindowsScene/Menu.unity", android: false);
        }

        private static void AddProfilToScene(string scenePath, bool android)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError("[MenuProfilBuilder] Scène introuvable : " + scenePath);
                return;
            }
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var menuController = Object.FindFirstObjectByType<CardGame.Unity.MenuController>();
            if (menuController == null)
            {
                Debug.LogError("[MenuProfilBuilder] MenuController introuvable.");
                return;
            }

            if (GameObject.Find("PanelProfil") != null)
            {
                Debug.Log("[MenuProfilBuilder] PanelProfil existe déjà. Rien à faire.");
                return;
            }

            var menuUi = GameObject.Find("MenuUi");
            if (menuUi == null)
            {
                Debug.LogError("[MenuProfilBuilder] MenuUi introuvable.");
                return;
            }

            var panelMenu = GameObject.Find("PanelMenu");
            if (panelMenu == null)
            {
                Debug.LogError("[MenuProfilBuilder] PanelMenu introuvable.");
                return;
            }

            // Créer le prefab ItemSuccesPrefab s'il n'existe pas
            var succesPrefabPath = "Assets/Prefab/ItemSuccesPrefab.prefab";
            GameObject succesPrefab = null;
            if (System.IO.File.Exists(succesPrefabPath))
            {
                succesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(succesPrefabPath);
            }
            else
            {
                succesPrefab = CreateItemSuccesPrefab(succesPrefabPath);
            }

            // Créer le prefab ItemStatsPrefab s'il n'existe pas
            var statsPrefabPath = "Assets/Prefab/ItemStatsPrefab.prefab";
            GameObject statsPrefab = null;
            if (System.IO.File.Exists(statsPrefabPath))
            {
                statsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(statsPrefabPath);
            }
            else
            {
                statsPrefab = CreateItemStatsPrefab(statsPrefabPath);
            }

            // Créer le bouton Profil (copier Historique)
            var historiqueBtn = panelMenu.transform.Find("Historique");
            GameObject buttonProfilGO = null;
            if (historiqueBtn != null)
            {
                buttonProfilGO = Object.Instantiate(historiqueBtn.gameObject, panelMenu.transform);
                buttonProfilGO.name = "Profil";
                buttonProfilGO.transform.SetSiblingIndex(historiqueBtn.GetSiblingIndex() + 1);
                var rt = buttonProfilGO.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(0, -100);
                var text = buttonProfilGO.GetComponentInChildren<TMP_Text>();
                if (text != null) text.text = "Profil";
            }
            else
            {
                buttonProfilGO = CreateButton(panelMenu.transform, "Profil", new Vector2(0, -100));
            }

            var buttonProfil = buttonProfilGO.GetComponent<Button>();

            // Créer PanelProfil (similaire à PanelHistorique)
            var panelHistorique = GameObject.Find("PanelHistorique");
            RectTransform panelHistoriqueRT = panelHistorique != null ? panelHistorique.GetComponent<RectTransform>() : null;

            var panelProfilGO = new GameObject("PanelProfil");
            panelProfilGO.transform.SetParent(menuUi.transform, false);
            var panelProfilRT = panelProfilGO.AddComponent<RectTransform>();
            panelProfilRT.anchorMin = Vector2.zero;
            panelProfilRT.anchorMax = Vector2.one;
            panelProfilRT.offsetMin = Vector2.zero;
            panelProfilRT.offsetMax = Vector2.zero;
            var panelProfilImg = panelProfilGO.AddComponent<Image>();
            panelProfilImg.color = new Color(0, 0, 0, 0.97f);
            panelProfilGO.SetActive(false);

            // Bouton Retour
            var btnBackGO = new GameObject("ButtonReturnProfil");
            btnBackGO.transform.SetParent(panelProfilGO.transform, false);
            var btnBackRT = btnBackGO.AddComponent<RectTransform>();
            btnBackRT.anchorMin = new Vector2(0, 1);
            btnBackRT.anchorMax = new Vector2(0, 1);
            btnBackRT.pivot = new Vector2(0, 1);
            btnBackRT.anchoredPosition = new Vector2(20, -20);
            btnBackRT.sizeDelta = new Vector2(160, 50);
            var btnBackImg = btnBackGO.AddComponent<Image>();
            btnBackImg.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            var btnBack = btnBackGO.AddComponent<Button>();
            var btnBackTextGO = new GameObject("Text");
            btnBackTextGO.transform.SetParent(btnBackGO.transform, false);
            var btnBackTextRT = btnBackTextGO.AddComponent<RectTransform>();
            btnBackTextRT.anchorMin = Vector2.zero;
            btnBackTextRT.anchorMax = Vector2.one;
            btnBackTextRT.offsetMin = Vector2.zero;
            btnBackTextRT.offsetMax = Vector2.zero;
            var btnBackText = btnBackTextGO.AddComponent<TextMeshProUGUI>();
            btnBackText.text = "Retour";
            btnBackText.fontSize = 22;
            btnBackText.alignment = TextAlignmentOptions.Center;

            // ScrollView pour le contenu
            var scrollGO = new GameObject("ScrollViewProfil");
            scrollGO.transform.SetParent(panelProfilGO.transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0.05f, 0.1f);
            scrollRT.anchorMax = new Vector2(0.95f, 0.9f);
            scrollRT.offsetMin = new Vector2(20, 80);
            scrollRT.offsetMax = new Vector2(-20, -20);

            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            var scrollImg = scrollGO.AddComponent<Image>();
            scrollImg.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewportGO.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            var viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.color = Color.clear;
            var mask = viewportGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            var contentGO = new GameObject("ContentProfil");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0, 800);

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(15, 15, 15, 15);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            var csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.content = contentRT;
            scrollRect.viewport = viewportRT;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // ContentStats (conteneur pour les items de stats, préfab instanciés)
            var contentStatsGO = new GameObject("ContentStats");
            contentStatsGO.transform.SetParent(contentGO.transform, false);
            var contentStatsRT = contentStatsGO.AddComponent<RectTransform>();
            var contentStatsLE = contentStatsGO.AddComponent<LayoutElement>();
            contentStatsLE.flexibleWidth = 1;
            contentStatsLE.minHeight = 100;
            var contentStatsVlg = contentStatsGO.AddComponent<VerticalLayoutGroup>();
            contentStatsVlg.spacing = 12;
            contentStatsVlg.padding = new RectOffset(0, 0, 0, 10);
            contentStatsVlg.childAlignment = TextAnchor.UpperLeft;
            contentStatsVlg.childControlHeight = true;
            contentStatsVlg.childControlWidth = true;
            contentStatsVlg.childForceExpandHeight = false;
            contentStatsVlg.childForceExpandWidth = true;

            // Succès Content (sous les stats)
            var succesContentGO = new GameObject("ContentSucces");
            succesContentGO.transform.SetParent(contentGO.transform, false);
            var succesContentRT = succesContentGO.AddComponent<RectTransform>();
            var succesContentLE = succesContentGO.AddComponent<LayoutElement>();
            succesContentLE.minHeight = 300;
            succesContentLE.flexibleWidth = 1;
            var succesContentVlg = succesContentGO.AddComponent<VerticalLayoutGroup>();
            succesContentVlg.spacing = 8;
            succesContentVlg.childForceExpandHeight = false;
            succesContentVlg.childControlHeight = true;
            succesContentVlg.childControlWidth = true;

            // ProfileController sur le même objet que MenuController
            var menuControllerGO = menuController.gameObject;
            var profileController = menuControllerGO.GetComponent<CardGame.Unity.ProfileController>();
            if (profileController == null)
                profileController = menuControllerGO.AddComponent<CardGame.Unity.ProfileController>();

            var so = new SerializedObject(profileController);
            so.FindProperty("_panelProfil").objectReferenceValue = panelProfilGO;
            so.FindProperty("_statsScrollContent").objectReferenceValue = contentStatsGO.transform;
            so.FindProperty("_statsItemPrefab").objectReferenceValue = statsPrefab;
            so.FindProperty("_succesListContent").objectReferenceValue = succesContentGO.transform;
            so.FindProperty("_succesItemPrefab").objectReferenceValue = succesPrefab;
            so.FindProperty("_buttonBackFromProfil").objectReferenceValue = btnBack;
            so.ApplyModifiedPropertiesWithoutUndo();

            // MenuController : ajouter _buttonProfil et _profileController
            var menuSo = new SerializedObject(menuController);
            menuSo.FindProperty("_buttonProfil").objectReferenceValue = buttonProfil;
            menuSo.FindProperty("_profileController").objectReferenceValue = profileController;
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            if (android)
            {
                menuSo.FindProperty("_android").boolValue = true;
                menuSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();
            Debug.Log($"[MenuProfilBuilder] Profil ajouté : {scenePath}");
        }

        [MenuItem("CardGame/Ajouter Profil à Androide_Menu")]
        public static void AddProfilToAndroideMenu()
        {
            var androidPath = "Assets/Scenes/AndroideScene/Androide_Menu.unity";
            if (!System.IO.File.Exists(androidPath))
            {
                Debug.LogError("[MenuProfilBuilder] Androide_Menu introuvable.");
                return;
            }
            AddProfilToScene(androidPath, android: true);
        }

        private static GameObject CreateButton(Transform parent, string label, Vector2 pos)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(220, 90);
            go.AddComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f, 1f);
            var btn = go.AddComponent<Button>();
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            return go;
        }

        private static GameObject CreateItemSuccesPrefab(string path)
        {
            var go = new GameObject("ItemSuccesPrefab");
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 80);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 60;
            le.preferredHeight = 80;
            go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.22f, 1f);
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10, 5);
            textRT.offsetMax = new Vector2(-10, -5);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Succès";
            text.fontSize = 18;
            text.richText = true;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;

            if (!System.IO.Directory.Exists("Assets/Prefab"))
                System.IO.Directory.CreateDirectory("Assets/Prefab");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateItemStatsPrefab(string path)
        {
            var go = new GameObject("ItemStatsPrefab");
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 60);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 40;
            le.flexibleWidth = 1;
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            go.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.2f, 0.95f);
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(12, 8);
            textRT.offsetMax = new Vector2(-12, -8);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Stats";
            text.fontSize = 16;
            text.richText = true;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;

            if (!System.IO.Directory.Exists("Assets/Prefab"))
                System.IO.Directory.CreateDirectory("Assets/Prefab");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
