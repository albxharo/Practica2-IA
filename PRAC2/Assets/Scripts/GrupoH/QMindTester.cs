using NavigationDJIA.World;
using QMind.Interfaces;
using System.IO;
using System;
using UnityEngine;
using System.Globalization;

namespace GrupoH
{
    public class QMindTester : IQMind
    {
        private TablaQLearning tablaQ; // Tabla Q entrenada
        private WorldInfo mundo; // Información del mundo
        private int numAcciones = 4; // Norte, Sur, Este, Oeste
        private int numEstados = 16 * 9; // Total de estados posibles

        public void Initialize(WorldInfo worldInfo)
        {
            Debug.Log("QMindTester: inicializando...");

            // Inicialización del mundo y tabla Q
            mundo = worldInfo;
            tablaQ = new TablaQLearning(numAcciones, numEstados);

            // Cargar la tabla Q previamente entrenada
            CargarTablaQ();

            Debug.Log("QMindTester: inicialización completa.");
        }

        public CellInfo GetNextStep(CellInfo currentPosition, CellInfo otherPosition)
        {
            if (currentPosition == null || otherPosition == null)
            {
                Debug.LogError("Las posiciones proporcionadas son nulas.");
                return currentPosition; // Mantener la posición actual
            }

            // Calcular el estado actual
            int estadoActual = ObtenerEstado(currentPosition, otherPosition);
            Debug.Log($"Estado actual: {estadoActual}, Posición agente: ({currentPosition.x}, {currentPosition.y}), " +
                      $"Posición enemigo: ({otherPosition.x}, {otherPosition.y})");

            // Calcular la distancia Manhattan al enemigo
            float distanciaEnemigo = currentPosition.Distance(otherPosition, CellInfo.DistanceType.Manhattan);
            Debug.Log($"Distancia al enemigo: {distanciaEnemigo}");

            // Forzar escape si el enemigo está demasiado cerca
            if (distanciaEnemigo <= 3)
            {
                Debug.Log("El enemigo está cerca. Forzando movimiento de escape.");

                // Buscar una acción que aumente la distancia al enemigo
                int mejorAccionEscape = -1;
                float mejorDistancia = float.MinValue;

                for (int accion = 0; accion < numAcciones; accion++)
                {
                    CellInfo nuevaPosicion = Movimiento.MovimientoAgente(accion, currentPosition, mundo);

                    if (nuevaPosicion.Walkable)
                    {
                        float nuevaDistancia = nuevaPosicion.Distance(otherPosition, CellInfo.DistanceType.Manhattan);
                        if (nuevaDistancia > mejorDistancia)
                        {
                            mejorDistancia = nuevaDistancia;
                            mejorAccionEscape = accion;
                        }
                    }
                }

                if (mejorAccionEscape != -1)
                {
                    Debug.Log($"Acción de escape elegida: {mejorAccionEscape}");
                    return Movimiento.MovimientoAgente(mejorAccionEscape, currentPosition, mundo);
                }
            }

            // Seleccionar la mejor acción basada en la tabla Q
            int mejorAccion = tablaQ.ObtenerMejorAccion(estadoActual);
            Debug.Log($"Acción elegida: {mejorAccion}");

            // Mover el agente basado en la acción elegida
            CellInfo nuevaPosicionFinal = Movimiento.MovimientoAgente(mejorAccion, currentPosition, mundo);

            Debug.Log($"Nueva posición del agente: ({nuevaPosicionFinal.x}, {nuevaPosicionFinal.y})");
            return nuevaPosicionFinal;
        }



        private int ObtenerEstado(CellInfo posicionAgente, CellInfo posicionEnemigo)
        {
            // Calcular la posición relativa del oponente respecto al agente
            int deltaX = Mathf.Clamp(posicionEnemigo.x - posicionAgente.x, -1, 1) + 1; // Rango [0, 2]
            int deltaY = Mathf.Clamp(posicionEnemigo.y - posicionAgente.y, -1, 1) + 1; // Rango [0, 2]
            int posicionRelativa = deltaX * 3 + deltaY; // Combinar en un rango [0, 8]

            // Identificar celda única del agente
            int celdaAgente = posicionAgente.x * mundo.WorldSize.x + posicionAgente.y;

            // Combinar la posición del agente con la posición relativa del oponente
            int estado = (celdaAgente * 9 + posicionRelativa) % numEstados;

            // Depuración
            Debug.Log($"ObtenerEstado - Agente: ({posicionAgente.x}, {posicionAgente.y}), Enemigo: ({posicionEnemigo.x}, {posicionEnemigo.y}), Estado: {estado}");

            return estado;
        }

        private void CargarTablaQ()
        {
            string rutaTabla = "Assets/Scripts/GrupoH/TablaQ.csv";
            if (!File.Exists(rutaTabla))
            {
                Debug.LogError("No se encontró la tabla Q entrenada en la ruta especificada.");
                return;
            }

            try
            {
                using (StreamReader reader = new StreamReader(rutaTabla))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Ignorar encabezados o líneas vacías
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Estado")) continue;

                        // Separar por comas
                        var parts = line.Split(',');
                        if (parts.Length != 3)
                        {
                            Debug.LogWarning($"Línea inválida: {line}. Formato esperado: estado,acción,qValor");
                            continue;
                        }

                        // Validar y convertir datos
                        if (int.TryParse(parts[0], out int estado) &&
                            int.TryParse(parts[1], out int accion) &&
                            float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float qValor))
                        {
                            tablaQ.ActualizarQ(accion, estado, qValor);
                        }
                        else
                        {
                            Debug.LogWarning($"Error al procesar la línea: {line}. No se pudieron parsear los valores.");
                        }
                    }
                }
                Debug.Log("Tabla Q cargada exitosamente.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error al cargar la tabla Q: {ex.Message}");
            }
        }
    }
}


