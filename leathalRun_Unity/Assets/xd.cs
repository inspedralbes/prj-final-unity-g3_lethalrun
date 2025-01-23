using UnityEngine;
using UnityEngine.InputSystem;

public class xd : MonoBehaviour
{
    public Transform player;           // El objeto que la cámara sigue (por ejemplo, el jugador)
    public float collisionRadius = 0.3f; // Radio del SphereCast
    public LayerMask collisionLayers;    // Capas con las que la cámara debe colisionar
    public float smoothSpeed = 10f;      // Velocidad de ajuste de la cámara
    public float minVerticalAngle = -30f; // Ángulo mínimo para mirar hacia abajo
    public float maxVerticalAngle = 60f; // Ángulo máximo para mirar hacia arriba

    private Vector3 defaultOffset;       // Offset inicial de la cámara respecto al jugador
    private float verticalRotation = 0f; // Rotación vertical acumulada

    private Mouse mouse; // Referencia al mouse del Input System

    void Start()
    {
        // Inicializa el Mouse del Input System
        mouse = Mouse.current;

        // Calcula la posición inicial de la cámara respecto al jugador
        defaultOffset = transform.localPosition;
    }

    void LateUpdate()
    {
        // Obtén el movimiento del mouse en el eje Y (vertical)
        float mouseY = mouse.delta.y.ReadValue();

        // Manejo de la rotación vertical de la cámara
        verticalRotation -= mouseY * 0.1f; // Multiplicamos por un factor para controlar la velocidad
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);

        // Aplica la rotación vertical a la cámara
        transform.localEulerAngles = new Vector3(verticalRotation, transform.localEulerAngles.y, 0f);

        // Calcula la posición deseada de la cámara
        Vector3 desiredPosition = player.position + player.TransformVector(defaultOffset);

        // Lanza un SphereCast desde el jugador hacia la posición deseada
        Ray ray = new Ray(player.position + Vector3.up * 1.0f, (desiredPosition - (player.position + Vector3.up * 1.0f)).normalized);
        RaycastHit hit;

        // Realiza un SphereCast para verificar colisiones solo con las capas que definimos en LayerMask
        if (Physics.SphereCast(ray, collisionRadius, out hit, defaultOffset.magnitude, collisionLayers))
        {
            // Si hay colisión, ajusta la posición de la cámara
            float adjustedDistance = Mathf.Max(hit.distance - collisionRadius, 0.5f); // Evita que la cámara se acerque demasiado
            transform.position = Vector3.Lerp(transform.position, player.position + ray.direction * adjustedDistance, Time.deltaTime * smoothSpeed);
        }
        else
        {
            // Si no hay colisión, mueve la cámara a la posición deseada
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);
        }
    }
}
