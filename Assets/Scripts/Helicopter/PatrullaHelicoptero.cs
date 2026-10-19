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
        // Obtener la posición del objetivo actual
        Transform objetivoActual = puntosPatrulla[indicePuntoActual];
        Vector3 posicionObjetivo = objetivoActual.position;

        // 1. Manejo de la Rotación (mirar suavemente hacia el objetivo)
        Vector3 direccion = (posicionObjetivo - transform.position).normalized;
        if (direccion != Vector3.zero)
        {
            //Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            // Añade 180 grados en el eje Y para invertir la dirección del frente
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion) * Quaternion.Euler(0, 180, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
        }

        // 2. Manejo del Movimiento
        transform.position = Vector3.MoveTowards(
            transform.position,
            posicionObjetivo,
            velocidadMovimiento * Time.deltaTime
        );

        // 3. Verificación de llegada al punto actual
        float distanciaRestante = Vector3.Distance(transform.position, posicionObjetivo);
        if (distanciaRestante <= distanciaLlegada)
        {
            // Pasar al siguiente punto (al llegar al final, vuelve al 0)
            indicePuntoActual = (indicePuntoActual + 1) % puntosPatrulla.Length;
        }
    }

    // Dibujar líneas en el Editor para visualizar la ruta fácilmente
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