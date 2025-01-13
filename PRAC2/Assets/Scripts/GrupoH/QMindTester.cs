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

            // Mostrar los valores Q del estado actual
            Debug.Log($"Valores Q del estado {estadoActual}: " +
                      $"{tablaQ.ObtenerQ(0, estadoActual)}, {tablaQ.ObtenerQ(1, estadoActual)}, " +
                      $"{tablaQ.ObtenerQ(2, estadoActual)}, {tablaQ.ObtenerQ(3, estadoActual)}");

            // Seleccionar la mejor acción basada en la tabla Q
            int mejorAccion = tablaQ.ObtenerMejorAccion(estadoActual);
            Debug.Log($"Acción elegida: {mejorAccion}");

            // Mover el agente basado en la acción elegida
            CellInfo nuevaPosicion = Movimiento.MovimientoAgente(mejorAccion, currentPosition, mundo);

            // Validar si la nueva posición es válida
            if (!nuevaPosicion.Walkable)
            {
                Debug.LogWarning($"Movimiento inválido detectado. Acción: {mejorAccion}. Manteniendo posición actual.");
                return currentPosition;
            }

            Debug.Log($"Nueva posición del agente: ({nuevaPosicion.x}, {nuevaPosicion.y})");
            return nuevaPosicion;
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


