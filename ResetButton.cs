using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Botón 3D - Resetear posición uno por uno.
/// Cada click resetea el siguiente objeto de la lista.
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ResetButton : UdonSharpBehaviour
{
    [Header("Referencia al ObjectManager")]
    public ObjectManager objectManager;

    public override void Interact()
    {
        if (objectManager != null)
        {
            objectManager.StepReset();
        }
    }
}