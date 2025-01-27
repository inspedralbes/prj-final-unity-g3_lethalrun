using UnityEngine;
using UnityEngine.InputSystem;

public class Boton3D : MonoBehaviour
{
    public MonoBehaviour trampaComponent;
    private IActivable trampa;
    private InputAction eAction;
    private bool jugadorCerca = false;

    private void Start()
    {
        if (trampaComponent != null)
        {
            trampa = trampaComponent as IActivable;
            if (trampa == null)
            {
                Debug.LogError("El componente asignado no implementa la interfaz IActivable");
            }
        }
    }

    private void Awake()
    {
        eAction = new InputAction("EKey", InputActionType.Button, "<Keyboard>/e");
        eAction.performed += ctx => ActivarTrampaSiJugadorCerca();
        eAction.Enable();
    }

    private void OnDestroy()
    {
        eAction.Disable();
        eAction.Dispose();
    }

    void ActivarTrampaSiJugadorCerca()
    {
        if (jugadorCerca && trampa != null)
        {
            trampa.Activar();
            Debug.Log("Trampa activada por la tecla E");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
        }
    }
}
