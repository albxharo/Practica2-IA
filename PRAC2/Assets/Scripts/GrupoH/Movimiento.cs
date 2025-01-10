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
            // Obtener el camino del enemigo al agente
            CellInfo[] camino = algoritmo.GetPath(posicionEnemigo, posicionAgente, 1);

            // Retornar el próximo paso si existe
            if (camino != null && camino.Length > 1)
            {
                return camino[1]; // El primer paso después de la posición inicial
            }

            return posicionEnemigo; // Mantener la posición si no hay camino
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

