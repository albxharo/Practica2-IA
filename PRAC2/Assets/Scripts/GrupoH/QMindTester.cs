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

            // Seleccionar la mejor acci�n basada en la tabla Q
            int mejorAccion = tablaQ.ObtenerMejorAccion(estadoActual);

            // Mover el agente basado en la acci�n elegida
            return Movimiento.MovimientoAgente(mejorAccion, currentPosition, mundo);
        }

        private int ObtenerEstado(CellInfo posicionAgente, CellInfo posicionEnemigo)
        {
            // Combina las posiciones del agente y enemigo para obtener el �ndice �nico de estado
            int posicionRelativaX = posicionEnemigo.x - posicionAgente.x;
            int posicionRelativaY = posicionEnemigo.y - posicionAgente.y;

            // Normalizar las posiciones relativas dentro de [-1, 0, 1]
            posicionRelativaX = Mathf.Clamp(posicionRelativaX, -1, 1);
            posicionRelativaY = Mathf.Clamp(posicionRelativaY, -1, 1);

            // Convertir las coordenadas relativas y direcciones caminables en un estado �nico
            int estadoId = (posicionRelativaX + 1) * 3 + (posicionRelativaY + 1); // Ejemplo b�sico
            return Mathf.Clamp(estadoId, 0, numEstados - 1); // Asegurar que est� dentro del rango
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
