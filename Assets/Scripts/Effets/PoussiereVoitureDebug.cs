using UnityEngine;

namespace Barrage.Effets
{
    /// <summary>
    /// Composant de debug pour PoussiereVoiture.
    /// Ajouter sur le même GameObject que PoussiereVoiture dans chaque prefab.
    /// En Play mode : overlay OnGUI + Gizmos dans la Scene view.
    /// Désactiver le composant en production — aucun impact sur le rendu.
    /// </summary>
    [RequireComponent(typeof(PoussiereVoiture))]
    public sealed class PoussiereVoitureDebug : MonoBehaviour
    {
        // ── Config debug ──────────────────────────────────────────────────────

        [Header("Affichage")]
        [Tooltip("Affiche l'overlay OnGUI en Play mode.")]
        [SerializeField] private bool afficherOverlay = true;

        [Tooltip("Affiche les Gizmos dans la Scene view.")]
        [SerializeField] private bool afficherGizmos = true;

        [Tooltip("Position X de la fenêtre dans l'écran (en pixels). Décaler si deux instances se chevauchent.")]
        [SerializeField] private int positionX = 10;

        [Tooltip("Position Y de la fenêtre dans l'écran (en pixels).")]
        [SerializeField] private int positionY = 10;

        // ── Références internes ───────────────────────────────────────────────

        private PoussiereVoiture  _fx;
        private ScrollingElement  _scroll;
        private ParticleSystem[]  _sousSystèmes;
        private Rect              _rectFenêtre;
        private int               _idFenêtre;

        private static int _compteurId = 0;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _fx        = GetComponent<PoussiereVoiture>();
            _scroll    = GetComponentInParent<ScrollingElement>();
            _idFenêtre = _compteurId++;
        }

        private void Start()
        {
            // Les sous-systèmes sont créés dynamiquement par PoussiereVoiture.Awake().
            // On les capture dans Start() pour être sûr qu'ils existent.
            _sousSystèmes = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            _rectFenêtre  = new Rect(positionX, positionY, 330, 46 + _sousSystèmes.Length * 88);
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!afficherOverlay) return;
            if (_sousSystèmes == null || _sousSystèmes.Length == 0) return;

            _rectFenêtre = GUI.Window(
                _idFenêtre,
                _rectFenêtre,
                DessinerContenu,
                $"[PS DEBUG]  {transform.root.name}  /  {gameObject.name}"
            );
        }

        private void DessinerContenu(int id)
        {
            GUILayout.Space(2);

            // ── Infos véhicule ────────────────────────────────────────────────
            float scrollVitesse = _scroll != null ? _scroll.baseSpeed : -1f;
            GUILayout.Label($"Scroll speed    : <b>{scrollVitesse:F1} u/s</b>");
            GUILayout.Label($"Est Inversé     : <b>{_fx.EstInversé}</b>");
            GUILayout.Label($"Position Y world: {transform.position.y:F3}");
            GUILayout.Space(6);

            // ── Infos par sous-système ────────────────────────────────────────
            foreach (var ps in _sousSystèmes)
            {
                if (ps == null) continue;

                var m   = ps.main;
                var vel = ps.velocityOverLifetime;
                var shp = ps.shape;

                // Couleur selon l'état
                bool local = m.simulationSpace == ParticleSystemSimulationSpace.Local;
                GUI.color = local ? Color.cyan : Color.yellow;

                GUILayout.Label($"── {ps.name}");
                GUI.color = Color.white;

                GUILayout.Label(
                    $"  simSpace : {m.simulationSpace,-10}   " +
                    $"vel.space : {vel.space}");
                GUILayout.Label(
                    $"  vel.Y    : {FormatCurve(vel.y),-10}   " +
                    $"vel.radial: {FormatCurve(vel.radial)}");
                GUILayout.Label(
                    $"  shape    : {shp.shapeType,-12}  " +
                    $"radius: {shp.radius:F3}");
                GUILayout.Label(
                    $"  particles: {ps.particleCount,-6}     " +
                    $"startSpd: {FormatCurve(m.startSpeed)}");
                GUILayout.Space(4);
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!afficherGizmos) return;
            if (_sousSystèmes == null) return;

            foreach (var ps in _sousSystèmes)
            {
                if (ps == null) continue;

                var vel   = ps.velocityOverLifetime;
                var shp   = ps.shape;
                var m     = ps.main;
                bool local = m.simulationSpace == ParticleSystemSimulationSpace.Local;

                Vector3 origine = ps.transform.position;

                // Forme de spawn — jaune si World, cyan si Local
                Gizmos.color = local
                    ? new Color(0f, 1f, 1f, 0.25f)
                    : new Color(1f, 1f, 0f, 0.25f);
                Gizmos.DrawWireSphere(origine, shp.radius > 0.001f ? shp.radius : 0.05f);

                // Vecteur vélocité Y — rouge = vers le bas, vert = vers le haut
                float vy = vel.y.constantMax;
                if (Mathf.Abs(vy) > 0.001f)
                {
                    Gizmos.color = vy < 0f ? Color.red : Color.green;
                    Gizmos.DrawRay(origine, Vector3.up * Mathf.Clamp(vy * 0.15f, -2f, 2f));
                }

                // Vélocité radiale — bleu = convergence (négatif), magenta = divergence
                float vr = vel.radial.constantMax;
                if (Mathf.Abs(vr) > 0.001f)
                {
                    Gizmos.color = vr < 0f ? Color.blue : Color.magenta;
                    Gizmos.DrawWireSphere(origine, Mathf.Abs(vr) * 0.1f);
                }
            }

            // Ligne vers cible si disponible
            var cibleProp = _fx.Cible;
            if (cibleProp != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, cibleProp.position);
                Gizmos.DrawWireSphere(cibleProp.position, 0.08f);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string FormatCurve(ParticleSystem.MinMaxCurve c)
        {
            return c.mode == ParticleSystemCurveMode.Constant
                ? $"{c.constant:F2}"
                : $"[{c.constantMin:F1},{c.constantMax:F1}]";
        }
    }
}
