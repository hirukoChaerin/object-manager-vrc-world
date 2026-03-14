using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// ObjectManager - VRChat World Script (UdonSharp)
/// 
/// Funcionalidad:
///   - Botón 1 (ResetPositions): Reposiciona todos los objetos de la lista
///     a su posición y rotación original (la que tenían al iniciar el mundo).
///   - Botón 2 (ToggleVisibility): Oculta o muestra todos los objetos de la lista.
///   - Todo se sincroniza globalmente para todos los jugadores.
///
/// Configuración en Unity:
///   1. Crea un GameObject vacío y añade este script (UdonBehaviour).
///   2. En el Inspector, arrastra los objetos (platos, vasos, etc.) al array "Objects".
///   3. Crea dos botones (UI Button o VRC Interact):
///      - Botón Reset  → apunta al método ResetPositions()
///      - Botón Toggle → apunta al método ToggleVisibility()
///   4. Asegúrate de que el objeto con este script tenga un VRC Object Sync
///      o que los objetos hijos lo tengan si necesitan sincronización de físicas.
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ObjectManager : UdonSharpBehaviour
{
    [Header("Lista de Objetos")]
    [Tooltip("Arrastra aquí los GameObjects que quieres controlar (platos, vasos, etc.)")]
    public GameObject[] objects;

    // --- Estado sincronizado ---
    [UdonSynced] private bool isHidden = false;
    [UdonSynced] private bool requestReset = false;
    [UdonSynced] private int syncCounter = 0;

    // --- Datos locales (posiciones/rotaciones originales) ---
    private Vector3[] originalPositions;
    private Quaternion[] originalRotations;
    private int lastSyncCounter = 0;

    // =========================================================
    //  INICIALIZACIÓN
    // =========================================================
    void Start()
    {
        if (objects == null || objects.Length == 0)
        {
            Debug.LogWarning("[ObjectManager] No hay objetos asignados en la lista.");
            return;
        }

        // Guardar posiciones y rotaciones originales
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
    }

    // =========================================================
    //  BOTÓN 1: RESETEAR POSICIONES (Global)
    // =========================================================
    /// <summary>
    /// Llama este método desde un UI Button o un Interact Trigger.
    /// Resetea la posición de todos los objetos a su estado original.
    /// </summary>
    public void ResetPositions()
    {
        // Tomar ownership para poder sincronizar
        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        requestReset = true;
        syncCounter++;
        RequestSerialization();

        // Aplicar localmente de inmediato para feedback instantáneo
        _ApplyReset();
    }

    // =========================================================
    //  BOTÓN 2: OCULTAR / MOSTRAR (Toggle Global)
    // =========================================================
    /// <summary>
    /// Llama este método desde un UI Button o un Interact Trigger.
    /// Alterna la visibilidad de todos los objetos de la lista.
    /// </summary>
    public void ToggleVisibility()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        isHidden = !isHidden;
        syncCounter++;
        RequestSerialization();

        // Aplicar localmente de inmediato
        _ApplyVisibility();
    }

    // =========================================================
    //  SINCRONIZACIÓN - Cuando llegan datos del owner
    // =========================================================
    public override void OnDeserialization()
    {
        // Solo aplicar si el contador cambió (hay nuevos datos)
        if (syncCounter != lastSyncCounter)
        {
            lastSyncCounter = syncCounter;

            if (requestReset)
            {
                _ApplyReset();
                requestReset = false;
            }

            _ApplyVisibility();
        }
    }

    // =========================================================
    //  LATE JOINER - Sincronizar estado al unirse
    // =========================================================
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        // Si soy el owner, re-serializar para el nuevo jugador
        if (Networking.IsOwner(gameObject))
        {
            RequestSerialization();
        }
    }

    // =========================================================
    //  MÉTODOS INTERNOS
    // =========================================================
    private void _ApplyReset()
    {
        if (objects == null || originalPositions == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                // Si el objeto tiene VRC Pickup, soltar primero
                var pickup = (VRC_Pickup)objects[i].GetComponent(typeof(VRC_Pickup));
                if (pickup != null)
                {
                    pickup.Drop();
                }

                // Si tiene Rigidbody, resetear velocidad
                var rb = objects[i].GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                objects[i].transform.position = originalPositions[i];
                objects[i].transform.rotation = originalRotations[i];
            }
        }
    }

    private void _ApplyVisibility()
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(!isHidden);
            }
        }
    }
}
