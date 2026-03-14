using UdonSharp;
using UnityEngine;
using TMPro;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// ObjectManager v3 - Usa TextMeshProUGUI (VRC)
///
/// Botón Reset: Cada click resetea UN objeto (avanza al siguiente).
/// Botón Toggle: Cada click oculta UN objeto. Al terminar, cambia
///               de modo y muestra uno por uno.
///
/// Canvas: Usa Text - TextMeshPro (VRC) para los contadores.
///
/// Configuración:
///   1. Arrastra los objetos al array "Objects".
///   2. En el Canvas crea textos con: UI → Text - TextMeshPro (VRC)
///   3. Arrastra cada texto TMP al campo correspondiente.
///   4. Conecta los botones 3D (ResetButton / ToggleButton).
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ObjectManager : UdonSharpBehaviour
{
    [Header("=== Lista de Objetos ===")]
    [Tooltip("Arrastra aquí los GameObjects (platos, vasos, etc.)")]
    public GameObject[] objects;

    [Header("=== UI Canvas - TextMeshPro (VRC) ===")]
    [Tooltip("Muestra cuántos ciclos completos de Reset se han hecho")]
    public TextMeshProUGUI resetCounterText;

    [Tooltip("Muestra el progreso del Reset (ej: 3/8)")]
    public TextMeshProUGUI resetProgressText;

    [Tooltip("Muestra cuántos ciclos completos de Toggle se han hecho")]
    public TextMeshProUGUI toggleCounterText;

    [Tooltip("Muestra el progreso del Toggle (ej: Ocultando 3/8)")]
    public TextMeshProUGUI toggleProgressText;

    [Tooltip("Muestra el nombre del último objeto afectado")]
    public TextMeshProUGUI lastObjectText;

    // --- Estado sincronizado ---
    [UdonSynced] private int resetIndex = 0;
    [UdonSynced] private int resetTotalCount = 0;

    [UdonSynced] private int toggleIndex = 0;
    [UdonSynced] private int toggleTotalCount = 0;
    [UdonSynced] private bool isHidingPhase = true;

    [UdonSynced] private int syncVersion = 0;
    [UdonSynced] private string lastAffectedName = "";

    // --- Datos locales ---
    private Vector3[] originalPositions;
    private Quaternion[] originalRotations;
    private int lastSyncVersion = 0;

    // =========================================================
    //  INICIALIZACIÓN
    // =========================================================
    void Start()
    {
        if (objects == null || objects.Length == 0)
        {
            Debug.LogWarning("[ObjectManager] No hay objetos asignados.");
            return;
        }

        originalPositions = new Vector3[objects.Length];
        originalRotations = new Quaternion[objects.Length];

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                originalPositions[i] = objects[i].transform.position;
                originalRotations[i] = objects[i].transform.rotation;
            }
        }

        _UpdateUI();
    }

    // =========================================================
    //  BOTÓN 1: RESETEAR POSICIÓN (uno por uno)
    // =========================================================
    public void StepReset()
    {
        if (objects == null || objects.Length == 0) return;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        // Resetear el objeto en el índice actual
        _ResetSingleObject(resetIndex);

        // Guardar nombre del objeto afectado
        if (objects[resetIndex] != null)
        {
            lastAffectedName = "<color=#FF6B35>[R]</color> " + objects[resetIndex].name;
        }

        // Avanzar índice (ciclo circular)
        resetIndex = (resetIndex + 1) % objects.Length;

        // Si volvió al inicio, completó un ciclo
        if (resetIndex == 0)
        {
            resetTotalCount++;
        }

        syncVersion++;
        RequestSerialization();
        _UpdateUI();
    }

    // =========================================================
    //  BOTÓN 2: TOGGLE VISIBILIDAD (uno por uno)
    // =========================================================
    public void StepToggle()
    {
        if (objects == null || objects.Length == 0) return;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        if (isHidingPhase)
        {
            _SetObjectVisible(toggleIndex, false);
            if (objects[toggleIndex] != null)
            {
                lastAffectedName = "<color=#FF4444>[Oculto]</color> " + objects[toggleIndex].name;
            }
        }
        else
        {
            _SetObjectVisible(toggleIndex, true);
            if (objects[toggleIndex] != null)
            {
                lastAffectedName = "<color=#44DD44>[Visible]</color> " + objects[toggleIndex].name;
            }
        }

        // Avanzar índice
        toggleIndex = (toggleIndex + 1) % objects.Length;

        // Si completó la lista, cambiar de fase
        if (toggleIndex == 0)
        {
            isHidingPhase = !isHidingPhase;
            toggleTotalCount++;
        }

        syncVersion++;
        RequestSerialization();
        _UpdateUI();
    }

    // =========================================================
    //  SINCRONIZACIÓN
    // =========================================================
    public override void OnDeserialization()
    {
        if (syncVersion != lastSyncVersion)
        {
            lastSyncVersion = syncVersion;
            _RebuildState();
            _UpdateUI();
        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (Networking.IsOwner(gameObject))
        {
            RequestSerialization();
        }
    }

    // =========================================================
    //  RECONSTRUIR ESTADO (Late Joiners y Sync)
    // =========================================================
    private void _RebuildState()
    {
        if (objects == null) return;

        if (isHidingPhase)
        {
            // Objetos antes del toggleIndex están ocultos
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    objects[i].SetActive(i >= toggleIndex);
                }
            }
        }
        else
        {
            // Objetos antes del toggleIndex están visibles
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    objects[i].SetActive(i < toggleIndex);
                }
            }
        }
    }

    // =========================================================
    //  MÉTODOS INTERNOS
    // =========================================================
    private void _ResetSingleObject(int index)
    {
        if (objects == null || index < 0 || index >= objects.Length) return;
        if (objects[index] == null) return;

        var pickup = (VRC_Pickup)objects[index].GetComponent(typeof(VRC_Pickup));
        if (pickup != null)
        {
            pickup.Drop();
        }

        var rb = objects[index].GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        objects[index].transform.position = originalPositions[index];
        objects[index].transform.rotation = originalRotations[index];
        objects[index].SetActive(true);
    }

    private void _SetObjectVisible(int index, bool visible)
    {
        if (objects == null || index < 0 || index >= objects.Length) return;
        if (objects[index] == null) return;

        objects[index].SetActive(visible);
    }

    // =========================================================
    //  ACTUALIZAR UI - TextMeshPro (VRC)
    // =========================================================
    private void _UpdateUI()
    {
        int total = (objects != null) ? objects.Length : 0;

        // --- Reset ---
        if (resetCounterText != null)
        {
            resetCounterText.text = string.Format(
                "Ciclos Reset: <color=#FF6B35>{0}</color>", resetTotalCount
            );
        }

        if (resetProgressText != null)
        {
            resetProgressText.text = string.Format(
                "Progreso Reset: <color=#FFFFFF>{0}</color> / {1}", resetIndex, total
            );
        }

        // --- Toggle ---
        if (toggleCounterText != null)
        {
            toggleCounterText.text = string.Format(
                "Ciclos Toggle: <color=#44AAFF>{0}</color>", toggleTotalCount
            );
        }

        if (toggleProgressText != null)
        {
            string phase = isHidingPhase ? "Ocultando" : "Mostrando";
            string phaseColor = isHidingPhase ? "#FF4444" : "#44DD44";
            toggleProgressText.text = string.Format(
                "<color={0}>{1}</color>: <color=#FFFFFF>{2}</color> / {3}",
                phaseColor, phase, toggleIndex, total
            );
        }

        // --- Último objeto ---
        if (lastObjectText != null)
        {
            if (string.IsNullOrEmpty(lastAffectedName))
            {
                lastObjectText.text = "Esperando accion...";
            }
            else
            {
                lastObjectText.text = lastAffectedName;
            }
        }
    }
}