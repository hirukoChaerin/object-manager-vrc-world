using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Botón 3D interactivo para RESETEAR POSICIONES.
///
/// Configuración en Unity:
///   1. Crea un Cubo (o cualquier modelo 3D) en la escena.
///   2. Agrégale un Box Collider (ya lo trae por defecto).
///   3. Agrégale un UdonBehaviour con este script.
///   4. En el Inspector, arrastra el GameObject que tiene
///      el script ObjectManager al campo "objectManager".
///   5. Opcionalmente cambia el InteractionText en el
///      UdonBehaviour a algo como "Resetear Objetos".
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ResetButton : UdonSharpBehaviour
{
    [Header("Referencia al ObjectManager")]
    [Tooltip("Arrastra aquí el GameObject que tiene el script ObjectManager")]
    public ObjectManager objectManager;

    public override void Interact()
    {
        if (objectManager != null)
        {
            objectManager.ResetPositions();
        }
    }
}
