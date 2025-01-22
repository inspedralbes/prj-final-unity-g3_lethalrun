
using UnityEngine;
using Cinemachine;

public class FirstPersonWallAvoidance : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    public float raycastDistance = 0.5f;
    public LayerMask wallLayers;
    public int raycastCount = 5; // Número de raycasts para verificar alrededor

    private CinemachineTransposer transposer;
    private Vector3 originalOffset;

    void Start()
    {
        transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null)
        {
            originalOffset = transposer.m_FollowOffset;
        }
        else
        {
            Debug.LogError("No se encontró el componente CinemachineTransposer en la cámara virtual.");
        }
    }

    void LateUpdate()
    {
        if (transposer == null) return;

        float closestDistance = raycastDistance;

        // Realizar múltiples raycasts en diferentes direcciones
        for (int i = 0; i < raycastCount; i++)
        {
            float angle = i * (360f / raycastCount);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, raycastDistance, wallLayers))
            {
                closestDistance = Mathf.Min(closestDistance, hit.distance);
            }
        }

        // Raycast adicional hacia abajo
        RaycastHit downHit;
        if (Physics.Raycast(transform.position, -transform.up, out downHit, raycastDistance, wallLayers))
        {
            closestDistance = Mathf.Min(closestDistance, downHit.distance);
        }

        // Ajustar el offset de la cámara
        if (closestDistance < raycastDistance)
        {
            transposer.m_FollowOffset = new Vector3(
                originalOffset.x,
                originalOffset.y,
                Mathf.Min(originalOffset.z, closestDistance - 0.1f)
            );
        }
        else
        {
            transposer.m_FollowOffset = originalOffset;
        }
    }
}
