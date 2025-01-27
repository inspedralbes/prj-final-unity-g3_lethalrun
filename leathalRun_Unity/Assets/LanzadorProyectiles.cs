using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LanzadorProyectiles : MonoBehaviour, IActivable
{
    public GameObject prefabProyectil;
    public float velocidadProyectil = 10f;
    public Transform puntoDisparo;
    public int cantidadRafagas = 5;
    public float intervaloEntreRafagas = 0.5f;
    public float tiempoVidaProyectil = 5f;

    private List<GameObject> proyectilesActivos = new List<GameObject>();
    private bool estaActivado = false;

    public void Activar()
    {
        if (!estaActivado)
        {
            StartCoroutine(LanzarRafagas());
            estaActivado = true;
        }
    }

    private IEnumerator LanzarRafagas()
    {
        for (int i = 0; i < cantidadRafagas; i++)
        {
            LanzarProyectil();
            yield return new WaitForSeconds(intervaloEntreRafagas);
        }
        estaActivado = false;
    }

    private void LanzarProyectil()
    {
        GameObject proyectil = Instantiate(prefabProyectil, puntoDisparo.position, puntoDisparo.rotation);
        Rigidbody rb = proyectil.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity = false;
            rb.velocity = puntoDisparo.forward * velocidadProyectil;
        }

        proyectilesActivos.Add(proyectil);
        StartCoroutine(DestruirProyectilDespuesDeTiempo(proyectil));
    }

    private IEnumerator DestruirProyectilDespuesDeTiempo(GameObject proyectil)
    {
        yield return new WaitForSeconds(tiempoVidaProyectil);
        if (proyectil != null)
        {
            proyectilesActivos.Remove(proyectil);
            Destroy(proyectil);
        }
    }

    private void Update()
    {
        for (int i = proyectilesActivos.Count - 1; i >= 0; i--)
        {
            if (proyectilesActivos[i] != null)
            {
                proyectilesActivos[i].transform.Translate(puntoDisparo.forward * velocidadProyectil * Time.deltaTime);
            }
            else
            {
                proyectilesActivos.RemoveAt(i);
            }
        }
    }
}
