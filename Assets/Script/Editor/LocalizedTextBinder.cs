#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Drakensland.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool that automatically binds <see cref="LocalizedText"/> components
/// to every TMP text in every game scene by matching the current text content
/// to the existing localization database (French fallback).
/// 
/// Run via <b>Drakensland > Bind All Localized Texts (All Scenes)</b>.
/// Safe to run multiple times — existing bindings are updated, not duplicated.
/// Disabled GameObjects are fully supported.
/// </summary>
public static class LocalizedTextBinder
{
    // ── Texts that are set dynamically at runtime and must NOT be bound ──────
    // Add any text that is purely numeric or set by code.
    private static readonly HashSet<string> DynamicPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "0", "1", "100", "1000", "10000",       // counters / prices
        "new text",                              // Unity default placeholder
        "new textjyhnslbkvmcbkclmcbkmbvcbckvbnkv njlvcnjbnbvjl loremipsum dhfdbfvhbvkvbhdvbdchcdbhdvshdcjkhdvsjkdvshjkvncjcvnjkvkjvcnjkvhbjkbhjkxbfjkvbnvjkhvjkvhbkchvbklbhdjhfhdfj", // lorem ipsum
    };

    // ── GameObject names whose TMP text is always set by code ────────────────
    // Any LocalizedText found on these GameObjects will be stripped by CleanupDynamicComponents().
    private static readonly HashSet<string> DynamicGameObjectNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "PriceLabel",          // shop item price — set to cost.ToString()
        "Price",               // Etiquette/Price — set by SkinPurchaseButton / CoinPurchaseButton
        "LabelQuantite",       // inventory quantity — set to quantity.ToString()
        "Number",              // inventory quantity badge
        "LabelEtatEquipe",     // "Équipé" / "Équiper" — set by SkinPurchaseButton
        "NomObjet",            // skin name — set dynamically by SkinEquipFocus
        "DescriptionObjet",    // skin description — set dynamically by SkinEquipFocus
        "LabelNom",            // item name in inventory focus
        "LabelDescription",    // item description in inventory focus
        "ConfirmationNameLabel",         // confirmation panel name — dynamic
        "ConfirmationDescriptionLabel",  // confirmation panel desc — dynamic
        "TexteDescription",    // InventaireMenuUI dynamic description
        "ScoreText",           // runtime score
        "BestScoreText",       // runtime best score
        "RankText",            // leaderboard rank — dynamic
        "PlayerNameText",      // leaderboard player name — dynamic
        "ScoreValueText",      // leaderboard score value — dynamic
        "Count",               // IAP item count inside Price — dynamic
        "CountCoins",          // current coin balance — dynamic
        "CountPremium",        // current gem balance — dynamic
        "Text (TMP)",          // guard dialogue bubble — set by BulleDialogueGardeUI
        "TexteQuantité",       // document quantity on form icon — dynamic
        "Nickname",            // player profile name — set from cloud save
        "Label",               // close button icon (×) in tuto panels — not translatable text
    };

    // ── Scenes to process ────────────────────────────────────────────────────
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Barrage.unity",
        "Assets/Scenes/BarrageTuto.unity",
        "Assets/Scenes/MapRoad.unity",
        "Assets/Scenes/MapTuto.unity",
        "Assets/Scenes/ResultScene.unity",
        "Assets/Scenes/Test.unity",
    };

    // ── Lookup: French text → localization key ───────────────────────────────
    // Built at runtime from all LocalizationData assets whose languageCode == "fr".
    private static Dictionary<string, string> _frenchToKey;

    // ── Entry point ──────────────────────────────────────────────────────────

    // ── Additional entries missing from the initial asset creation ────────────
    // Each tuple: (languageCode, key, value)
    private static readonly (string lang, string key, string value)[] MissingEntries =
    {
        // French — base UI
        ("fr", "menu_home",               "Accueil"),
        ("fr", "shop_section_bonus_items","Objets bonus"),
        ("fr", "shop_section_cosmetics",  "Cosmétiques"),
        ("fr", "shop_section_currency",   "Monnaies"),
        ("fr", "options_sound",           "Volume général"),
        ("fr", "options_music",           "Volume musical"),
        ("fr", "options_ambient",         "Volume ambiant"),
        // English — base UI
        ("en", "menu_home",               "Home"),
        ("en", "shop_section_bonus_items","Bonus Items"),
        ("en", "shop_section_cosmetics",  "Cosmetics"),
        ("en", "shop_section_currency",   "Currency"),
        ("en", "options_sound",           "Master Volume"),
        ("en", "options_music",           "Music Volume"),
        ("en", "options_ambient",         "Ambient Volume"),

        // ── Pause menu (MapRoad) ──────────────────────────────────────────────
        ("fr", "pause_title",             "Le jeu est en pause."),
        ("fr", "pause_confirm_quit",      "Êtes vous sûr de revenir au menu ? (Vous perdrez la progression en cours)"),
        ("en", "pause_title",             "Game paused."),
        ("en", "pause_confirm_quit",      "Are you sure you want to return to the menu? (Your current progress will be lost)"),

        // ── Item names — French ───────────────────────────────────────────────
        ("fr", "item_liasse_nom",         "Liasse de billets"),
        ("fr", "item_passe_nom",          "Formulaire passe-partout"),
        ("fr", "item_badge_nom",          "Badge du gouvernement"),
        ("fr", "item_tirelire_nom",       "Tirelire cochon"),
        ("fr", "item_gateau_nom",         "Gâteau chinois"),
        ("fr", "item_radar_nom",          "Radar obstacles"),
        ("fr", "item_aspirateur_nom",     "Aspirateur"),
        ("fr", "item_montre_nom",         "Montre à gousset"),

        // ── Item names — English ──────────────────────────────────────────────
        ("en", "item_liasse_nom",         "Bundle of Bills"),
        ("en", "item_passe_nom",          "Master Form"),
        ("en", "item_badge_nom",          "Government Badge"),
        ("en", "item_tirelire_nom",       "Piggy Bank"),
        ("en", "item_gateau_nom",         "Fortune Cookie"),
        ("en", "item_radar_nom",          "Obstacle Radar"),
        ("en", "item_aspirateur_nom",     "Vacuum"),
        ("en", "item_montre_nom",         "Pocket Watch"),

        // ── Item descriptions — French ────────────────────────────────────────
        ("fr", "item_liasse_desc",        "Soudoie immédiatement le garde pour passer."),
        ("fr", "item_passe_desc",         "Fait office de n'importe quel document manquant au barrage."),
        ("fr", "item_badge_desc",         "Impressionne le garde — aucune question posée."),
        ("fr", "item_tirelire_desc",      "Fait pleuvoir des pièces autour de vous pendant quelques secondes."),
        ("fr", "item_gateau_desc",        "Booste temporairement votre multiplicateur de pièces."),
        ("fr", "item_radar_desc",         "Révèle les obstacles à venir pour esquiver plus tôt."),
        ("fr", "item_aspirateur_desc",    "Aspire automatiquement toutes les pièces proches."),
        ("fr", "item_montre_desc",        "Ralentit le temps brièvement pour vous laisser réagir."),

        // ── Item descriptions — English ───────────────────────────────────────
        ("en", "item_liasse_desc",        "Instantly bribes the guard to let you through."),
        ("en", "item_passe_desc",         "Acts as any missing document at the checkpoint."),
        ("en", "item_badge_desc",         "Intimidates the guard — no questions asked."),
        ("en", "item_tirelire_desc",      "Drops a shower of coins around you for a short time."),
        ("en", "item_gateau_desc",        "Temporarily boosts your coin multiplier."),
        ("en", "item_radar_desc",         "Reveals obstacles ahead so you can dodge them early."),
        ("en", "item_aspirateur_desc",    "Sucks in all nearby coins automatically."),
        ("en", "item_montre_desc",        "Slows down time briefly, giving you room to react."),

        // ── Cosmetic names — French ───────────────────────────────────────────
        ("fr", "skin_0_nom",  "Voiture simple"),
        ("fr", "skin_1_nom",  "Kitsch"),
        ("fr", "skin_2_nom",  "Voiture bleue"),
        ("fr", "skin_3_nom",  "Brasier"),
        ("fr", "skin_4_nom",  "Chabriolet"),
        ("fr", "skin_5_nom",  "Divauto"),
        ("fr", "skin_6_nom",  "Herbature"),
        ("fr", "skin_7_nom",  "Lovauto"),
        ("fr", "skin_8_nom",  "Alphacar"),
        ("fr", "skin_9_nom",  "Voiture du gouvernement"),
        ("fr", "skin_10_nom", "Bolide de course"),
        ("fr", "skin_11_nom", "Voiture rouge"),
        ("fr", "skin_12_nom", "Carcasse roulante"),
        ("fr", "skin_13_nom", "Rouge ondulant"),
        ("fr", "skin_14_nom", "Voiture violette"),
        ("fr", "skin_15_nom", "L.I.F.E"),

        // ── Cosmetic names — English ──────────────────────────────────────────
        ("en", "skin_0_nom",  "Basic Car"),
        ("en", "skin_1_nom",  "Kitsch"),
        ("en", "skin_2_nom",  "Blue Car"),
        ("en", "skin_3_nom",  "Blaze"),
        ("en", "skin_4_nom",  "Catmobile"),
        ("en", "skin_5_nom",  "Divacar"),
        ("en", "skin_6_nom",  "Naturemobile"),
        ("en", "skin_7_nom",  "Lovecar"),
        ("en", "skin_8_nom",  "Alphacar"),
        ("en", "skin_9_nom",  "Government Car"),
        ("en", "skin_10_nom", "Racecar"),
        ("en", "skin_11_nom", "Red Car"),
        ("en", "skin_12_nom", "Rustbucket"),
        ("en", "skin_13_nom", "Wavemobile"),
        ("en", "skin_14_nom", "Purple Car"),
        ("en", "skin_15_nom", "L.I.F.E"),

        // ── Cosmetic descriptions — French ────────────────────────────────────
        ("fr", "skin_0_desc",  "C'est la voiture de base, son bleu azur est un camouflage parfait pour se cacher dans le ciel !"),
        ("fr", "skin_1_desc",  "Cette voiture est un peu démodée mais rappelle des souvenirs d'une époque révolue..."),
        ("fr", "skin_2_desc",  "Malgré son bleu semblable aux fonds marins, cette voiture ne peut pas nager."),
        ("fr", "skin_3_desc",  "Cette voiture brûle comme la braise ! Tous les gens cools rêvent d'en avoir une !"),
        ("fr", "skin_4_desc",  "Tout le monde aime les petits chats ! Mais attention à ne pas les faire conduire sans autorisations."),
        ("fr", "skin_5_desc",  "C'est une voiture pour les gens de goût ! Même si elle ne convient pas à tous les goûts."),
        ("fr", "skin_6_desc",  "Face au problème grandissant des gens qui restent cloitrés chez eux, les plus grands inventeurs ont créé cette voiture qui amène l'extérieur avec vous !"),
        ("fr", "skin_7_desc",  "Qu'existe-t-il de plus beau que l'amour ? Cette voiture assurément."),
        ("fr", "skin_8_desc",  "Toutes les voitures le suivent et l'adorent, en tout cas c'est ce qu'il pense."),
        ("fr", "skin_9_desc",  "C'est la voiture officielle du gouvernement, c'est à se demander comment elle a atterri ici..."),
        ("fr", "skin_10_desc", "On dit de cette voiture qu'elle a appartenu à un grand pilote, mais depuis les grandes restrictions de vitesse, il est au chômage."),
        ("fr", "skin_11_desc", "C'est une voiture rouge, tout ce qu'il y a de plus banal."),
        ("fr", "skin_12_desc", "On se demande comment cet engin roule encore, et comment il passe les contrôles techniques."),
        ("fr", "skin_13_desc", "Son motif peut rappeler les vagues."),
        ("fr", "skin_14_desc", "Dans beaucoup de cultures, sa couleur symbolise le mal, le gouvernement hésita même à l'interdire."),
        ("fr", "skin_15_desc", "On dit que son conducteur triait des papiers, un boulot passionnant."),

        // ── Cosmetic descriptions — English ───────────────────────────────────
        ("en", "skin_0_desc",  "The basic car — its azure blue is the perfect camouflage to hide in the sky!"),
        ("en", "skin_1_desc",  "A bit dated, but it brings back memories of a bygone era..."),
        ("en", "skin_2_desc",  "Despite its deep-sea blue, this car definitely cannot swim."),
        ("en", "skin_3_desc",  "This car burns like an ember! Every cool person dreams of having one!"),
        ("en", "skin_4_desc",  "Everyone loves little cats! Just don't let them drive without proper authorisation."),
        ("en", "skin_5_desc",  "A car for people of taste! Even if it doesn't suit everyone's taste."),
        ("en", "skin_6_desc",  "Faced with the growing problem of people staying cooped up indoors, the greatest inventors created this car that brings the outdoors with you!"),
        ("en", "skin_7_desc",  "What is more beautiful than love? This car, without a doubt."),
        ("en", "skin_8_desc",  "All cars follow and adore it — at least that's what it thinks."),
        ("en", "skin_9_desc",  "The official government car. Makes you wonder how it ended up here..."),
        ("en", "skin_10_desc", "They say it once belonged to a great driver, but since the major speed restrictions came in, he's been out of work."),
        ("en", "skin_11_desc", "A red car. Nothing special."),
        ("en", "skin_12_desc", "Nobody knows how this thing is still running — or how it keeps passing its MOT."),
        ("en", "skin_13_desc", "Its pattern is reminiscent of waves."),
        ("en", "skin_14_desc", "In many cultures its colour symbolises evil. The government even considered banning it."),
        ("en", "skin_15_desc", "They say its driver sorted papers for a living. A thrilling career."),

        // ── Leaderboard tiers ─────────────────────────────────────────────────
        ("fr", "tier_bronze",  "Bronze"),
        ("fr", "tier_silver",  "Argent"),
        ("fr", "tier_gold",    "Or"),
        ("fr", "tier_diamond", "Diamant"),
        ("fr", "tier_emerald", "Émeraude"),
        ("fr", "tier_ruby",    "Rubis"),
        ("en", "tier_bronze",  "Bronze"),
        ("en", "tier_silver",  "Silver"),
        ("en", "tier_gold",    "Gold"),
        ("en", "tier_diamond", "Diamond"),
        ("en", "tier_emerald", "Emerald"),
        ("en", "tier_ruby",    "Ruby"),

        // ── Tutorial (BarrageTuto) ────────────────────────────────────────────
        ("fr", "tuto_barrage_corps",       "À chaque inspection, remettez au garde les documents demandés avant que la barre de patience en haut de l'écran ne se vide. Ici donnez lui un document rouge.\n\nUne fois tous les documents remis, le garde vous indiquera les documents à préparer pour la prochaine inspection."),
        ("fr", "tuto_barrage_bouton",      "Retour au menu"),
        ("fr", "tuto_felicitations_titre", "Félicitations !"),
        ("fr", "tuto_felicitations_corps", "Vous êtes prêt à passer les inspections !\n\nBonne chance pour la suite."),
        ("fr", "tuto_felicitations_bouton","Retour au menu"),
        ("en", "tuto_barrage_corps",       "At each checkpoint, hand the guard the requested documents before the patience bar at the top of the screen runs out. Here, give him a red document.\n\nOnce all documents are handed over, the guard will tell you which ones to prepare for the next inspection."),
        ("en", "tuto_barrage_bouton",      "Back to menu"),
        ("en", "tuto_felicitations_titre", "Congratulations!"),
        ("en", "tuto_felicitations_corps", "You are ready to go through the checkpoints!\n\nGood luck!"),
        ("en", "tuto_felicitations_bouton","Back to menu"),

        // ── Tutorial (MapTuto) ────────────────────────────────────────────────
        ("fr", "tuto_road_goal",      "Roulez pour échapper à la bureaucratie ! Traversez la ville et collectez les bons documents.\n\nEvitez les obstacles pour rester sur la route.\nLes inspections nécessitent des documents précis, gardez un oeil sur ce qu'il vous faut !"),
        ("fr", "tuto_road_swipe",     "Glissez vers la gauche ou la droite pour changer de voie !"),
        ("fr", "tuto_road_pieces",    "Sur la route, récupérez des pièces en roulant dessus.\n\nCes pièces vous permettront d'acheter des objets utiles dans la boutique !"),
        ("fr", "tuto_road_document",  "Voici le document dont vous aurez besoin pour la prochaine inspection.\n\nChaque inspection vous indiquera quels documents collecter. Roulez à côté des voitures de la bonne couleur et appuyez sur l'écran au bon moment pour attraper leur document ! Une voiture rouge vous donnera un document rouge, etc."),
        ("fr", "tuto_road_slider",    "Cette barre indique votre progression jusqu'à la prochaine inspection.\n\nQuand elle est pleine, l'inspection commence !\n\nPour ce tutoriel, vous allez être transporté directement à l'inspection."),
        ("fr", "tuto_road_prix",      "Prix"),
        ("fr", "tuto_road_continuer", "Continuez"),
        ("en", "tuto_road_goal",      "Drive to escape the bureaucracy! Cross the city and collect the right documents.\n\nAvoid obstacles to stay on the road.\nCheckpoints require specific documents — keep an eye on what you need!"),
        ("en", "tuto_road_swipe",     "Swipe left or right to change lanes!"),
        ("en", "tuto_road_pieces",    "On the road, collect coins by driving over them.\n\nYou can use these coins to buy useful items in the shop!"),
        ("en", "tuto_road_document",  "Here is the document you will need for the next checkpoint.\n\nEach checkpoint will tell you which documents to collect. Drive alongside cars of the right colour and tap the screen at the right moment to grab their document! A red car gives a red document, etc."),
        ("en", "tuto_road_slider",    "This bar shows your progress until the next checkpoint.\n\nWhen it is full, the inspection begins!\n\nFor this tutorial, you will be taken directly to the checkpoint."),
        ("en", "tuto_road_prix",      "Price"),
        ("en", "tuto_road_continuer", "Continue"),
    };

    // ── Item identifier → (cleNom, cleDescription) ────────────────────────────
    private static readonly Dictionary<string, (string cleNom, string cleDesc)> ItemLocalizationKeys
        = new()
    {
        { "LiasseDeBillets",       ("item_liasse_nom",      "item_liasse_desc")      },
        { "FormulairePasePartout", ("item_passe_nom",        "item_passe_desc")        },
        { "BadgeDuGouvernement",   ("item_badge_nom",        "item_badge_desc")        },
        { "TirelireCochon",        ("item_tirelire_nom",     "item_tirelire_desc")     },
        { "GateauChinois",         ("item_gateau_nom",       "item_gateau_desc")       },
        { "RadarObstacles",        ("item_radar_nom",        "item_radar_desc")        },
        { "Aspirateur",            ("item_aspirateur_nom",   "item_aspirateur_desc")   },
        { "MontreAGousset",        ("item_montre_nom",       "item_montre_desc")       },
    };

    // ── Cosmetic index → (cleNom, cleDescription) ─────────────────────────────
    // Keys match SO_Cosmetiques.cosmetiques[i].identifiant (string).
    private static readonly Dictionary<string, (string cleNom, string cleDesc)> CosmeticLocalizationKeys
        = new()
    {
        { "0",  ("skin_0_nom",  "skin_0_desc")  },
        { "1",  ("skin_1_nom",  "skin_1_desc")  },
        { "2",  ("skin_2_nom",  "skin_2_desc")  },
        { "3",  ("skin_3_nom",  "skin_3_desc")  },
        { "4",  ("skin_4_nom",  "skin_4_desc")  },
        { "5",  ("skin_5_nom",  "skin_5_desc")  },
        { "6",  ("skin_6_nom",  "skin_6_desc")  },
        { "7",  ("skin_7_nom",  "skin_7_desc")  },
        { "8",  ("skin_8_nom",  "skin_8_desc")  },
        { "9",  ("skin_9_nom",  "skin_9_desc")  },
        { "10", ("skin_10_nom", "skin_10_desc") },
        { "11", ("skin_11_nom", "skin_11_desc") },
        { "12", ("skin_12_nom", "skin_12_desc") },
        { "13", ("skin_13_nom", "skin_13_desc") },
        { "14", ("skin_14_nom", "skin_14_desc") },
        { "15", ("skin_15_nom", "skin_15_desc") },
    };

    [MenuItem("Drakensland/Bind All Localized Texts (All Scenes)")]
    public static void BindAllScenes()
    {
        PatchMissingEntries();
        PatchItemLocalizationKeys();
        PatchCosmeticLocalizationKeys();
        PatchTierLocalizationKeys();
        BuildFrenchLookup();

        if (_frenchToKey.Count == 0)
        {
            EditorUtility.DisplayDialog("Error",
                "No French LocalizationData found.\n" +
                "Make sure a LocalizationData asset with languageCode='fr' exists.",
                "OK");
            return;
        }

        string activeScenePath = SceneManager.GetActiveScene().path;

        int totalBound    = 0;
        int totalSkipped  = 0;
        int totalUnmapped = 0;
        int totalCleaned  = 0;

        foreach (string scenePath in ScenePaths)
        {
            if (!System.IO.File.Exists(scenePath)) continue;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            int cleaned = CleanupDynamicComponents(scene);
            totalCleaned += cleaned;

            int bound, skipped, unmapped;
            ProcessScene(scene, out bound, out skipped, out unmapped);

            totalBound    += bound;
            totalSkipped  += skipped;
            totalUnmapped += unmapped;

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LocalizedTextBinder] {scenePath} → bound={bound}, skipped={skipped}, unmapped={unmapped}, cleaned={cleaned}");
        }

        // Restore the original scene if possible
        if (!string.IsNullOrEmpty(activeScenePath))
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);

        string summary =
            $"Binding complete across {ScenePaths.Length} scenes.\n\n" +
            $"  Bound   : {totalBound}\n" +
            $"  Cleaned : {totalCleaned}  (LocalizedText stripped from dynamic GOs)\n" +
            $"  Skipped : {totalSkipped}  (dynamic / numeric)\n" +
            $"  Unmapped: {totalUnmapped}  (no matching key — check console)";

        Debug.Log($"[LocalizedTextBinder] {summary}");
        EditorUtility.DisplayDialog("Localization Binder", summary, "OK");
    }

    // ── Patch missing entries ─────────────────────────────────────────────────

    /// <summary>
    /// Injects any missing key/value pairs into the existing LocalizationData assets.
    /// Uses SerializedObject so Unity tracks the changes properly.
    /// </summary>
    private static void PatchMissingEntries()
    {
        // Group missing entries by language code
        Dictionary<string, List<(string key, string value)>> byLang = new();
        foreach ((string lang, string key, string value) in MissingEntries)
        {
            if (!byLang.ContainsKey(lang))
                byLang[lang] = new List<(string, string)>();
            byLang[lang].Add((key, value));
        }

        string[] guids = AssetDatabase.FindAssets("t:LocalizationData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LocalizationData data = AssetDatabase.LoadAssetAtPath<LocalizationData>(path);
            if (data == null) continue;
            if (!byLang.TryGetValue(data.languageCode, out List<(string key, string value)> toAdd)) continue;

            SerializedObject so       = new SerializedObject(data);
            SerializedProperty entries = so.FindProperty("_entries");

            foreach ((string key, string value) in toAdd)
            {
                // Check if key already exists
                bool found = false;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == key)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) continue;

                entries.InsertArrayElementAtIndex(entries.arraySize);
                SerializedProperty newEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                newEntry.FindPropertyRelative("key").stringValue   = key;
                newEntry.FindPropertyRelative("value").stringValue = value;
                Debug.Log($"[LocalizedTextBinder] Patched '{data.languageCode}': {key} = {value}");
            }

            so.ApplyModifiedProperties();
            data.InvalidateCache();
            EditorUtility.SetDirty(data);
        }

        AssetDatabase.SaveAssets();
    }

    // ── Patch TierDefinition localization keys ────────────────────────────────

    /// <summary>
    /// Sets cleNom on each TierDefinition asset so that ObtenirNomLocalise()
    /// returns the correct translation. Uses tierId as the lookup key.
    /// </summary>
    private static void PatchTierLocalizationKeys()
    {
        // tierId → cleNom
        Dictionary<string, string> tierKeys = new()
        {
            { "bronze",  "tier_bronze"  },
            { "silver",  "tier_silver"  },
            { "gold",    "tier_gold"    },
            { "diamond", "tier_diamond" },
            { "emerald", "tier_emerald" },
            { "ruby",    "tier_ruby"    },
        };

        string[] guids = AssetDatabase.FindAssets("t:TierDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TierDefinition tier = AssetDatabase.LoadAssetAtPath<TierDefinition>(path);
            if (tier == null) continue;

            if (!tierKeys.TryGetValue(tier.tierId, out string cleNom)) continue;

            SerializedObject so = new SerializedObject(tier);
            SerializedProperty prop = so.FindProperty("cleNom");
            if (prop.stringValue == cleNom) continue;

            prop.stringValue = cleNom;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(tier);
            Debug.Log($"[LocalizedTextBinder] Patched tier '{tier.tierId}' → cleNom = '{cleNom}'");
        }

        AssetDatabase.SaveAssets();
    }

    // ── Patch SO_Cosmetiques localization keys ────────────────────────────────

    /// <summary>
    /// Sets cleNom and cleDescription on each DefinitionCosmetique in SO_Cosmetiques
    /// so that ObtenirNomLocalise() / ObtenirDescriptionLocalisee() return proper translations.
    /// Uses the integer identifiant field as the lookup key.
    /// </summary>
    private static void PatchCosmeticLocalizationKeys()
    {
        string[] guids = AssetDatabase.FindAssets("t:SO_Cosmetiques");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SO_Cosmetiques catalogue = AssetDatabase.LoadAssetAtPath<SO_Cosmetiques>(path);
            if (catalogue == null) continue;

            SerializedObject so           = new SerializedObject(catalogue);
            SerializedProperty cosmetiques = so.FindProperty("cosmetiques");
            bool modified                  = false;

            for (int i = 0; i < cosmetiques.arraySize; i++)
            {
                SerializedProperty item = cosmetiques.GetArrayElementAtIndex(i);
                string identifiant = item.FindPropertyRelative("identifiant").stringValue;

                if (!CosmeticLocalizationKeys.TryGetValue(identifiant, out (string cleNom, string cleDesc) keys))
                    continue;

                SerializedProperty propNom  = item.FindPropertyRelative("cleNom");
                SerializedProperty propDesc = item.FindPropertyRelative("cleDescription");

                if (propNom.stringValue  != keys.cleNom)  { propNom.stringValue  = keys.cleNom;  modified = true; }
                if (propDesc.stringValue != keys.cleDesc) { propDesc.stringValue = keys.cleDesc; modified = true; }
            }

            if (modified)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(catalogue);
                Debug.Log($"[LocalizedTextBinder] Patched cosmetic localization keys in '{path}'.");
            }
        }

        AssetDatabase.SaveAssets();
    }

    // ── Patch SO_InventaireObjets item localization keys ─────────────────────

    /// <summary>
    /// Sets cleNom and cleDescription on each DefinitionObjetSpecial in SO_InventaireObjets
    /// so that ObtenirNomLocalise() / ObtenirDescriptionLocalisee() return proper translations.
    /// </summary>
    private static void PatchItemLocalizationKeys()
    {
        string[] guids = AssetDatabase.FindAssets("t:SO_InventaireObjets");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SO_InventaireObjets catalogue = AssetDatabase.LoadAssetAtPath<SO_InventaireObjets>(path);
            if (catalogue == null) continue;

            SerializedObject so       = new SerializedObject(catalogue);
            SerializedProperty objets = so.FindProperty("objets");
            bool modified             = false;

            for (int i = 0; i < objets.arraySize; i++)
            {
                SerializedProperty item = objets.GetArrayElementAtIndex(i);
                string id = item.FindPropertyRelative("identifiant").stringValue;

                if (!ItemLocalizationKeys.TryGetValue(id, out (string cleNom, string cleDesc) keys))
                    continue;

                SerializedProperty propNom  = item.FindPropertyRelative("cleNom");
                SerializedProperty propDesc = item.FindPropertyRelative("cleDescription");

                if (propNom.stringValue  != keys.cleNom)  { propNom.stringValue  = keys.cleNom;  modified = true; }
                if (propDesc.stringValue != keys.cleDesc) { propDesc.stringValue = keys.cleDesc; modified = true; }
            }

            if (modified)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(catalogue);
                Debug.Log($"[LocalizedTextBinder] Patched item localization keys in '{path}'.");
            }
        }

        AssetDatabase.SaveAssets();
    }

    // ── Cleanup wrongly-bound dynamic GameObjects ─────────────────────────────

    /// <summary>
    /// Removes <see cref="LocalizedText"/> from any GameObject whose name is in
    /// <see cref="DynamicGameObjectNames"/>. Repairs bindings created before the
    /// exclusion list was complete (e.g., Price labels with key "shop_buy").
    /// </summary>
    private static int CleanupDynamicComponents(Scene scene)
    {
        int removed = 0;
        List<LocalizedText> allLT = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            CollectLocalizedTextRecursive(root.transform, allLT);

        foreach (LocalizedText lt in allLT)
        {
            if (!DynamicGameObjectNames.Contains(lt.gameObject.name)) continue;

            Debug.Log($"[LocalizedTextBinder] Removing LocalizedText from dynamic GO '{GetPath(lt.transform)}' in '{scene.name}'");
            Undo.DestroyObjectImmediate(lt);
            removed++;
        }

        if (removed > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        return removed;
    }

    /// <summary>Recursively collects all LocalizedText components, including disabled GameObjects.</summary>
    private static void CollectLocalizedTextRecursive(Transform t, List<LocalizedText> results)
    {
        LocalizedText lt = t.GetComponent<LocalizedText>();
        if (lt != null)
            results.Add(lt);

        for (int i = 0; i < t.childCount; i++)
            CollectLocalizedTextRecursive(t.GetChild(i), results);
    }

    // ── Scene processing ─────────────────────────────────────────────────────

    private static void ProcessScene(Scene scene, out int bound, out int skipped, out int unmapped)
    {
        bound    = 0;
        skipped  = 0;
        unmapped = 0;

        // FindObjectsOfType does NOT include disabled GameObjects — we walk manually.
        List<TMP_Text> allTexts = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            CollectTMPRecursive(root.transform, allTexts);

        foreach (TMP_Text tmp in allTexts)
        {
            // Skip texts already bound (key already set)
            LocalizedText existing = tmp.GetComponent<LocalizedText>();
            if (existing != null)
            {
                SerializedObject exSo = new SerializedObject(existing);
                string existingKey = exSo.FindProperty("_key").stringValue;
                if (!string.IsNullOrEmpty(existingKey))
                {
                    skipped++;
                    continue;
                }
            }

            string rawText = tmp.text ?? string.Empty;
            string trimmed = rawText.Trim();

            // Skip dynamic GameObjects by name (price labels, description panels, etc.)
            if (DynamicGameObjectNames.Contains(tmp.gameObject.name))
            {
                skipped++;
                continue;
            }

            // Skip purely dynamic / numeric / empty texts
            if (string.IsNullOrEmpty(trimmed) || IsDynamic(trimmed))
            {
                skipped++;
                continue;
            }

            // Try to find the matching key
            string key = FindKey(trimmed);
            if (key == null)
            {
                Debug.LogWarning($"[LocalizedTextBinder] No key for \"{Truncate(trimmed, 60)}\" " +
                                 $"on '{GetPath(tmp.transform)}' in '{scene.name}'");
                unmapped++;
                continue;
            }

            // Add or update LocalizedText
            LocalizedText lt = tmp.GetComponent<LocalizedText>();
            if (lt == null)
                lt = Undo.AddComponent<LocalizedText>(tmp.gameObject);

            SerializedObject so = new SerializedObject(lt);
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(tmp.gameObject);
            bound++;
        }

        if (bound > 0)
            EditorSceneManager.MarkSceneDirty(scene);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Recursively collects all TMP_Text components, including on disabled GameObjects.</summary>
    private static void CollectTMPRecursive(Transform t, List<TMP_Text> results)
    {
        TMP_Text tmp = t.GetComponent<TMP_Text>();
        if (tmp != null)
            results.Add(tmp);

        for (int i = 0; i < t.childCount; i++)
            CollectTMPRecursive(t.GetChild(i), results);
    }

    /// <summary>
    /// Tries to match a text string to a localization key.
    /// First tries an exact French match, then a case-insensitive match.
    /// </summary>
    private static string FindKey(string text)
    {
        if (_frenchToKey.TryGetValue(text, out string key))
            return key;

        // Case-insensitive fallback
        foreach (KeyValuePair<string, string> pair in _frenchToKey)
        {
            if (string.Equals(pair.Key, text, StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        }

        return null;
    }

    private static bool IsDynamic(string text)
    {
        // Pure number
        if (float.TryParse(text, out _)) return true;
        // Known dynamic placeholder
        return DynamicPatterns.Contains(text.ToLowerInvariant());
    }

    /// <summary>Builds the French text → key reverse lookup from all LocalizationData assets.</summary>
    private static void BuildFrenchLookup()
    {
        _frenchToKey = new Dictionary<string, string>(StringComparer.Ordinal);

        string[] guids = AssetDatabase.FindAssets("t:LocalizationData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LocalizationData data = AssetDatabase.LoadAssetAtPath<LocalizationData>(path);
            if (data == null || !string.Equals(data.languageCode, "fr", StringComparison.OrdinalIgnoreCase))
                continue;

            // Use reflection to read the private _entries list
            System.Reflection.FieldInfo field = typeof(LocalizationData)
                .GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field == null) continue;

            System.Collections.IList entries = field.GetValue(data) as System.Collections.IList;
            if (entries == null) continue;

            foreach (object entry in entries)
            {
                System.Type entryType = entry.GetType();
                string k = entryType.GetField("key")?.GetValue(entry) as string;
                string v = entryType.GetField("value")?.GetValue(entry) as string;

                if (!string.IsNullOrEmpty(k) && !string.IsNullOrEmpty(v))
                    _frenchToKey[v] = k;
            }
        }

        Debug.Log($"[LocalizedTextBinder] French lookup built: {_frenchToKey.Count} entries.");
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t    = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
#endif
