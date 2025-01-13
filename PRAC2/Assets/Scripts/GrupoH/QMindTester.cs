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
        private int numAcciones = 4; // Norte, Sur, Este, Oeste
        private int numEstados = 16 * 9; // Total de estados posibles

        public void Initialize(WorldInfo worldInfo)
        {
            Debug.Log("QMindTester: inicializando...");

            // Inicializaci�n del mundo y tabla Q
            mundo = worldInfo;
            tablaQ = new TablaQLearning(numAcciones, numEstados);

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
            int estadoActual = ObtenerEstado(currentPosition, otherPosition);
            Debug.Log($"Estado actual: {estadoActual}, Posici�n agente: ({currentPosition.x}, {currentPosition.y}), " +
                      $"Posici�n enemigo: ({otherPosition.x}, {otherPosition.y})");

            // Calcular la distancia Manhattan al enemigo
            float distanciaEnemigo = currentPosition.Distance(otherPosition, CellInfo.DistanceType.Manhattan);
            Debug.Log($"Distancia al enemigo: {distanciaEnemigo}");

            // Forzar escape si el enemigo est� demasiado cerca
            if (distanciaEnemigo <= 3)
            {
                Debug.Log("El enemigo est� cerca. Forzando movimiento de escape.");

                // Buscar una acci�n que aumente la distancia al enemigo
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
                    Debug.Log($"Acci�n de escape elegida: {mejorAccionEscape}");
                    return Movimiento.MovimientoAgente(mejorAccionEscape, currentPosition, mundo);
                }
            }

            // Seleccionar la mejor acci�n basada en la tabla Q
            int mejorAccion = tablaQ.ObtenerMejorAccion(estadoActual);
            Debug.Log($"Acci�n elegida: {mejorAccion}");

            // Mover el agente basado en la acci�n elegida
            CellInfo nuevaPosicionFinal = Movimiento.MovimientoAgente(mejorAccion, currentPosition, mundo);

            Debug.Log($"Nueva posici�n del agente: ({nuevaPosicionFinal.x}, {nuevaPosicionFinal.y})");
            return nuevaPosicionFinal;
        }



        private int ObtenerEstado(CellInfo posicionAgente, CellInfo posicionEnemigo)
        {
            // Calcular la posici�n relativa del oponente respecto al agente
            int deltaX = Mathf.Clamp(posicionEnemigo.x - posicionAgente.x, -1, 1) + 1; // Rango [0, 2]
            int deltaY = Mathf.Clamp(posicionEnemigo.y - posicionAgente.y, -1, 1) + 1; // Rango [0, 2]
            int posicionRelativa = deltaX * 3 + deltaY; // Combinar en un rango [0, 8]

            // Identificar celda �nica del agente
            int celdaAgente = posicionAgente.x * mundo.WorldSize.x + posicionAgente.y;

            // Combinar la posici�n del agente con la posici�n relativa del oponente
            int estado = (celdaAgente * 9 + posicionRelativa) % numEstados;

            // Depuraci�n
            Debug.Log($"ObtenerEstado - Agente: ({posicionAgente.x}, {posicionAgente.y}), Enemigo: ({posicionEnemigo.x}, {posicionEnemigo.y}), Estado: {estado}");

            return estado;
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
    }
}


