using System;
using System.Collections;
using Barrage.UI;
using UnityEngine;
using UnityEngine.UI;

public class LightEndManager : MonoBehaviour
{
    [SerializeField] private GameObject _revivePanel;
    [SerializeField] private Image _whiteScreen;
    [SerializeField] private BackToMenu _backToMenu;
    [SerializeField] private SO_PlayerDatas _playerDatas;
    [SerializeField] private DonnéesSession _session;
    [SerializeField] private AudioClip _RoadMusic;

    [Header("Barrage")]
    [SerializeField] private GestionnaireGameOver _gestionnaireGameOver;
    [SerializeField] private BarrePatience _barrePatience;
    [SerializeField] private AffichageProchaineDemandeUI _affichageDemande;
    [SerializeField] private FormulaireUIManager _formulaireUIManager;
    [SerializeField] private BulleDialogueGardeUI _bulleDialogueHaute;
    [SerializeField] private BulleDialogueGardeUI _bulleDialogueBasse;
    [SerializeField] private FormulaireLibreManager _formulaireLibreManager;



    public int reviveCounter = 0;

    /// <summary>Déclenché dès que le joueur meurt, avant l'animation de game over.</summary>
    public static event Action OnPlayerDied;

    private void OnEnable()
    {
        reviveCounter = _session.revive;
        if (_barrePatience != null)
            _barrePatience.OnPatienceEpuisée += OnDeath;
        if (_gestionnaireGameOver != null)
            _gestionnaireGameOver.OnGameOverTerminé += SurGameOverTerminé;
    }

    private void OnDisable()
    {
        if (_barrePatience != null)
            _barrePatience.OnPatienceEpuisée -= OnDeath;
        if (_gestionnaireGameOver != null)
            _gestionnaireGameOver.OnGameOverTerminé -= SurGameOverTerminé;
    }

    /// <summary>Déclenché quand la patience est épuisée. Lance le game over et sauvegarde.</summary>
    public void OnDeath()
    {
        OnPlayerDied?.Invoke();
        _gestionnaireGameOver?.Déclencher();
        SaveScoreAndCoin();
    }

    /// <summary>Appelé quand l'animation de game over est terminée. Affiche le panel de revive.</summary>
    private void SurGameOverTerminé()
    {
        _revivePanel.SetActive(true);
        _backToMenu.StartTimer();
    }

    /// <summary>Le joueur a choisi de revivre. Masque le game over et déclenche la victoire du barrage.</summary>
    public void Revive()
    {
        _backToMenu.OnRevival();
        reviveCounter++;
        _revivePanel.SetActive(false);
        _gestionnaireGameOver?.Cacher();    // retire tampons, réactive mainDuGarde
        _barrePatience?.Geler();
        _formulaireUIManager?.NettoyerCartes();  // retire les formulaires restants
        _formulaireLibreManager?.NettoyerCartes();

        SoundManager.Instance?.PlayMusicWithLowPass(_RoadMusic);
        StartCoroutine(Whiter(0.3f));
        _affichageDemande?.LancerDirectement();  // affiche les icônes de la prochaine demande

        // Réaffiche la bulle de dialogue avec la réplique "prochain poste"
        // Les deux bulles sont notifiées ; seule la bulle active pour l'état courant du garde s'affiche.
        _bulleDialogueHaute?.AfficherSequenceSuivante();
        _bulleDialogueBasse?.AfficherSequenceSuivante();
    }


    private IEnumerator Whiter(float duration)
    {
        if (_whiteScreen == null)
        {
            Debug.LogWarning("[LightEndManager] Whiter : _whiteScreen non assigné, animation ignorée.");
            yield break;
        }

        Color baseColor = _whiteScreen.color;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float value = Mathf.Sin(t * Mathf.PI);
            Color c = baseColor;
            c.a = value;
            _whiteScreen.color = c;
            yield return null;
        }

        Color end = baseColor;
        end.a = 0f;
        _whiteScreen.color = end;
    }

    private void SaveScoreAndCoin()
    {
        if (_session == null || _playerDatas == null)
        {
            Debug.LogWarning("[LightEndManager] SaveScoreAndCoin : donnéesSession ou playerDatas non assigné.");
            return;
        }

        int score = _session.score;
        int coins = _session.pièces;

        _playerDatas.generalMonney += coins;
        _playerDatas.actualCoinsNotSaved = coins;
        _playerDatas.actualScoreNotSaved = score;

        if (score > _playerDatas.BestScore)
        {
            _playerDatas.BestScore = score;
            _playerDatas.isAnHighScore = true;
        }

        _playerDatas.SaveDatas();
    }


    /// <summary>Sauvegarde le compteur de revives dans les données de session.</summary>
    public void SauvegarderDansSession(DonnéesSession donnees)
    {
        donnees.revive = reviveCounter;
    }

    /// <summary>Restaure le compteur de revives depuis les données de session.</summary>
    public void RestaurerDepuisSession(int revive)
    {
        reviveCounter = revive;
    }
}
