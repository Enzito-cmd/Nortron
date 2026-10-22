using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrullaHelicoptero : MonoBehaviour
{
    [Header("Configuración de Puntos")]
    [Tooltip("Lista de Transforms que representan los puntos de patrulla")]
    public Transform[] puntosPatrulla;

    [Header("Parámetros de Movimiento")]
    public float velocidadMovimiento = 15f;
    public float velocidadRotacion = 3f;
    public float distanciaLlegada = 2f;

    private int indicePuntoActual = 0;

    void Update()
    {
        if (puntosPatrulla.Length == 0) return;

        MoverHaciaPunto();
    }

    void MoverHaciaPunto()
    {
        Transform objetivoActual = puntosPatrulla[indicePuntoActual];
        Vector3 posicionObjetivo = objetivoActual.position;

        Vector3 direccion = (posicionObjetivo - transform.position).normalized;
        if (direccion != Vector3.zero)
        {
            //Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion) * Quaternion.Euler(0, 180, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            posicionObjetivo,
            velocidadMovimiento * Time.deltaTime
        );

        float distanciaRestante = Vector3.Distance(transform.position, posicionObjetivo);
        if (distanciaRestante <= distanciaLlegada)
        {
            indicePuntoActual = (indicePuntoActual + 1) % puntosPatrulla.Length;
        }
    }

    private void OnDrawGizmos()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < puntosPatrulla.Length; i++)
        {
            if (puntosPatrulla[i] != null)
            {
                Gizmos.DrawSphere(puntosPatrulla[i].position, 0.8f);

                int siguienteIndex = (i + 1) % puntosPatrulla.Length;
                if (puntosPatrulla[siguienteIndex] != null)
                {
                    Gizmos.DrawLine(puntosPatrulla[i].position, puntosPatrulla[siguienteIndex].position);
                }
            }
        }
    }
}