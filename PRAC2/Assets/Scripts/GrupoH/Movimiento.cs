using System;
using NavigationDJIA.Interfaces;
using NavigationDJIA.World;
using UnityEngine;

namespace GrupoH
{
    public static class Movimiento
    {
        // Movimiento del enemigo hacia el agente
        public static CellInfo MovimientoEnemigo(INavigationAlgorithm algoritmo, CellInfo posicionEnemigo, CellInfo posicionAgente)
        {
            try
            {
                // Validar algoritmo
                if (algoritmo == null)
                {
                    UnityEngine.Debug.LogError("El algoritmo de navegación es nulo.");
                    return posicionEnemigo;
                }

                // Validar posiciones
                if (posicionEnemigo == null)
                {
                    UnityEngine.Debug.LogError("La posición del enemigo es nula.");
                    return posicionEnemigo;
                }

                if (posicionAgente == null)
                {
                    UnityEngine.Debug.LogError("La posición del agente es nula.");
                    return posicionEnemigo;
                }

                // Obtener el camino del enemigo al agente
                CellInfo[] camino = algoritmo.GetPath(posicionEnemigo, posicionAgente, maxDepth: 100);

                // Validar el camino obtenido
                if (camino != null && camino.Length > 0)
                {
                    return camino[0]; // El primer paso después de la posición inicial
                }
                else
                {
                    UnityEngine.Debug.LogWarning("No se pudo calcular un camino válido.");
                }
            }
            catch (Exception ex)
            {
                //UnityEngine.Debug.LogError($"Error al calcular el camino: {ex.Message}");
                //UnityEngine.Debug.LogError($"Pila de excepciones: {ex.StackTrace}");
            }

            // Mantener la posición si ocurre un error o no hay camino
            return posicionEnemigo;
        }




        // Movimiento del agente basado en la acción elegida
        public static CellInfo MovimientoAgente(int accion, CellInfo posicionActual, WorldInfo mundo)
        {
            int nuevaX = posicionActual.x;
            int nuevaY = posicionActual.y;

            switch (accion)
            {
                case 0: // Norte
                    nuevaY++;
                    break;
                case 1: // Este
                    nuevaX++;
                    break;
                case 2: // Sur
                    nuevaY--;
                    break;
                case 3: // Oeste
                    nuevaX--;
                    break;
                default:
                    return posicionActual;
            }

            // Acceso a la celda usando el indexador
            if (nuevaX >= 0 && nuevaY >= 0 && nuevaX < mundo.WorldSize.x && nuevaY < mundo.WorldSize.y)
            {
                CellInfo nuevaPosicion = mundo[nuevaX, nuevaY];
                if (nuevaPosicion != null && nuevaPosicion.Walkable)
                {
                    return nuevaPosicion;
                }
            }

            return posicionActual; // Mantener la posición actual si no es caminable
        }
    }
}

