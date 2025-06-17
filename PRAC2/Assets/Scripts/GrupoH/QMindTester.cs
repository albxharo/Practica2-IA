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
        private WorldInfo mundo; // Informaci�n del mundo


        public void Initialize(WorldInfo worldInfo)
        {
            Debug.Log("QMindTester: inicializando...");

            // Inicializaci�n del mundo y tabla Q
            mundo = worldInfo;
            tablaQ = new TablaQLearning();

            // Cargar la tabla Q previamente entrenada
            CargarTablaQ();

            Debug.Log("QMindTester: inicializaci�n completa.");
        }

        public CellInfo GetNextStep(CellInfo currentPosition, CellInfo otherPosition)
        {
            if (currentPosition == null || otherPosition == null)
            {
                Debug.LogError("Las posiciones proporcionadas son nulas.");
                return currentPosition; // Mantener la posici�n actual
            }

            // Calcular el estado actual
            int estadoActual = ObtenerEstado(currentPosition, otherPosition, mundo);
            Debug.Log($"Estado actual: {estadoActual}, Posici�n agente: ({currentPosition.x}, {currentPosition.y}), " +
                      $"Posici�n enemigo: ({otherPosition.x}, {otherPosition.y})");

            // Calcular la distancia Manhattan al enemigo
            float distanciaEnemigo = currentPosition.Distance(otherPosition, CellInfo.DistanceType.Manhattan);
            Debug.Log($"Distancia al enemigo: {distanciaEnemigo}");

            // Seleccionar la mejor acci�n basada en la tabla Q
            int mejorAccion = tablaQ.ObtenerMejorAccion(estadoActual);
            Debug.Log($"Acci�n elegida: {mejorAccion}");

            // Mover el agente basado en la acci�n elegida
            CellInfo nuevaPosicionFinal = Movimiento.MovimientoAgente(mejorAccion, currentPosition, mundo);

            Debug.Log($"Nueva posici�n del agente: ({nuevaPosicionFinal.x}, {nuevaPosicionFinal.y})");
            return nuevaPosicionFinal;
        }


        private void CargarTablaQ()
        {
            string rutaTabla = "Assets/Scripts/GrupoH/TablaQ.csv";
            if (!File.Exists(rutaTabla))
            {
                Debug.LogError("No se encontr� la tabla Q entrenada en la ruta especificada.");
                return;
            }

            try
            {
                using (StreamReader reader = new StreamReader(rutaTabla))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Ignorar encabezados o l�neas vac�as
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Estado")) continue;

                        // Separar por comas
                        var parts = line.Split(',');
                        if (parts.Length != 3)
                        {
                            Debug.LogWarning($"L�nea inv�lida: {line}. Formato esperado: estado,acci�n,qValor");
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
                            Debug.LogWarning($"Error al procesar la l�nea: {line}. No se pudieron parsear los valores.");
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

        private int ObtenerEstado(CellInfo agente, CellInfo enemigo, WorldInfo mundo)
        {
            int deltaX = Mathf.Clamp(enemigo.x - agente.x, -1, 1) + 1;
            int deltaY = Mathf.Clamp(enemigo.y - agente.y, -1, 1) + 1;
            int posicionRelativa = deltaX * 3 + deltaY;

            bool[] direcciones = new bool[4];
            direcciones[0] = agente.y + 1 < mundo.WorldSize.y && mundo[agente.x, agente.y + 1].Walkable;
            direcciones[1] = agente.x + 1 < mundo.WorldSize.x && mundo[agente.x + 1, agente.y].Walkable;
            direcciones[2] = agente.y - 1 >= 0 && mundo[agente.x, agente.y - 1].Walkable;
            direcciones[3] = agente.x - 1 >= 0 && mundo[agente.x - 1, agente.y].Walkable;

            int codAccesibilidad = 0;
            for (int i = 0; i < 4; i++)
                if (direcciones[i]) codAccesibilidad |= (1 << i);

            int estado = posicionRelativa * 16 + codAccesibilidad;
            Debug.Log($"ObtenerEstado - Relaci�n enemigo: {posicionRelativa}, Caminabilidad: {codAccesibilidad}, Estado: {estado}");
            return estado;
        }

    }
}


