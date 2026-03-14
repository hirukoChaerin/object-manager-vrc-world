using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Botón 3D - Ocultar/Mostrar uno por uno.
/// Cada click oculta el siguiente objeto. Al terminar la lista,
/// cambia de modo y empieza a mostrar uno por uno.
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ToggleButton : UdonSharpBehaviour
{
    [Header("Referencia al ObjectManager")]
    public ObjectManager objectManager;

    public override void Interact()
    {
        if (objectManager != null)
        {
            objectManager.StepToggle();
        }
    }
}