#if UNITY_EDITOR
using Drakensland.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LocalizedTextBinder
{
    private const string SCENE_PATH = "Assets/Scenes/MainMenu.unity";

    private static readonly (string path, string key)[] Bindings =
    {
        ("Canvas/CanvasChild/OptionMenu/Cadre/FondCadre/VerticalOrganization/MasterVolume/TextContener/Text (TMP)",   LocalizationKeys.OPTIONS_SOUND),
        ("Canvas/CanvasChild/OptionMenu/Cadre/FondCadre/VerticalOrganization/Volume musical/TextContener/Text (TMP)", LocalizationKeys.OPTIONS_MUSIC),
        ("Canvas/CanvasChild/OptionMenu/Cadre/FondCadre/VerticalOrganization/Volume ambiant/TextContener/Text (TMP)", LocalizationKeys.OPTIONS_SOUND),
        ("Canvas/CanvasChild/OptionMenu/Cadre/FondCadre/RestoreButton/RestorateAchat",                                LocalizationKeys.SHOP_RESTORE_PURCHASES),
        ("Canvas/CanvasChild/PermanentUI/VerticalBotomAligment/BottomSize/FondBottom/BottomButtons/Boutique/ButtonOrganisation1/IconeAnimated1/Text (TMP)1",   LocalizationKeys.MENU_SHOP),
        ("Canvas/CanvasChild/PermanentUI/VerticalBotomAligment/BottomSize/FondBottom/BottomButtons/Button/ButtonOrganisation2/IconeAnimated2/Text (TMP)2",      LocalizationKeys.MENU_PLAY),
        ("Canvas/CanvasChild/PermanentUI/VerticalBotomAligment/BottomSize/FondBottom/BottomButtons/Equipment/ButtonOrganisation3/IconeAnimated3/Text (TMP)3",   LocalizationKeys.MENU_INVENTORY),
        ("Canvas/CanvasChild/ShopMenu/PanelShop/Viewport/SkinEquipFocus/FocusRoot/Pose/NomObjet/DescriptionObjet/ContenerButton/DejaEquiped",              LocalizationKeys.SHOP_EQUIPPED),
        ("Canvas/CanvasChild/ShopMenu/PanelShop/Viewport/SkinEquipFocus/FocusRoot/Pose/NomObjet/DescriptionObjet/ContenerButton/BoutonEquiper/Text (TMP)", LocalizationKeys.SHOP_EQUIP),
        ("Canvas/CanvasChild/ShopMenu/PanelShop/Viewport/PanelConfirm/PanelConfirm (1)/ContenerButton/Confirm/Text (TMP)", LocalizationKeys.COMMON_YES),
        ("Canvas/CanvasChild/ShopMenu/PanelShop/Viewport/PanelConfirm/PanelConfirm (1)/ContenerButton/Cancel/Text (TMP)",  LocalizationKeys.COMMON_NO),
        ("Canvas/CanvasChild/ShopMenu/PanelShop/Viewport/PanelConfirmSkin/PanelConfirm (1)/ContenerButton/Confirm/Text (TMP)", LocalizationKeys.COMMON_YES),
        ("Canvas/CanvasChild/ShopMenu/PanelShop/Viewport/PanelConfirmSkin/PanelConfirm (1)/ContenerButton/Cancel/Text (TMP)",  LocalizationKeys.COMMON_NO),
    };

    [MenuItem("Drakensland/Bind Localized Texts (MainMenu)")]
    public static void BindAll()
    {
        Scene scene = SceneManager.GetSceneByPath(SCENE_PATH);
        if (!scene.isLoaded)
        {
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            scene = SceneManager.GetSceneByPath(SCENE_PATH);
        }

        int bound = 0, missing = 0;

        foreach ((string goPath, string key) in Bindings)
        {
            GameObject go = FindInScene(scene, goPath);
            if (go == null) { Debug.LogWarning($"[Binder] Not found: '{goPath}'"); missing++; continue; }
            if (go.GetComponent<TMP_Text>() == null) { missing++; continue; }

            LocalizedText lt = go.GetComponent<LocalizedText>();
            if (lt == null) lt = Undo.AddComponent<LocalizedText>(go);

            SerializedObject so = new SerializedObject(lt);
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(go);
            bound++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Binder", $"Bound: {bound}  |  Missing: {missing}", "OK");
    }

    private static GameObject FindInScene(Scene scene, string path)
    {
        string[] parts = path.Split('/');
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != parts[0]) continue;
            if (parts.Length == 1) return root;
            Transform t = root.transform;
            for (int i = 1; i < parts.Length; i++)
            {
                t = FindChild(t, parts[i]);
                if (t == null) return null;
            }
            return t.gameObject;
        }
        return null;
    }

    private static Transform FindChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        return null;
    }
}
#endif
