using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NavigationDJIA.World;
using UnityEngine;

namespace GrupoH
{
    public class TablaQLearning
    {
        private float[,] tablaQ; // Matriz de valores Q
        public int numAcciones = 4; // N�mero de acciones (filas): norte,sur,este y oeste
        public int numEstados =144; // N�mero de estados (columnas)

        public TablaQLearning()
        {
            tablaQ = new float[numAcciones, numEstados];

            // Inicializar la tabla Q con valores -1000
            for (int accion = 0; accion < numAcciones; accion++)
            {
                for (int estado = 0; estado < numEstados; estado++)
                {
                    tablaQ[accion, estado] = -1000f;
                }
            }
        }
        
        public TablaQLearning(string rutaTabla)
        {
            if (!File.Exists(rutaTabla))
            {
                Debug.LogError("No se encontr� la tabla Q entrenada en la ruta especificada.");
                return;
            }
            tablaQ = new float[numAcciones, numEstados];

            //try
            //{
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
                            ActualizarQ(accion, estado, qValor);
                        }
                        else
                        {
                            Debug.LogWarning($"Error al procesar la l�nea: {line}. No se pudieron parsear los valores.");
                        }
                    }
                }
                Debug.Log("Tabla Q cargada exitosamente.");
            //}
           /* catch (Exception ex)
            {
                Debug.LogError($"Error al cargar la tabla Q: {ex.Message}");
            }*/
        }


        public void CalcularQ(int estadoActual, int accion, float recompensa, int nuevoEstado, float alpha, float gamma)
        {
            if (tablaQ[accion,estadoActual] == -1000)
            {
                tablaQ[accion,estadoActual] = 0;
            }

            float qActual = ObtenerQ(accion, estadoActual);
            float mejorQ = ObtenerMejorAccion(nuevoEstado);

            // F�rmula de actualizaci�n Q-Learning
            float nuevoQ = qActual + alpha * (recompensa + gamma * mejorQ - qActual);
            ActualizarQ(accion, estadoActual, nuevoQ);
            Debug.Log($"Estado: {estadoActual}, Acci�n: {accion}, Q antes: {qActual}, Q despu�s: {nuevoQ}");

        }

        // Obtener el valor Q para una acci�n y un estado
        public float ObtenerQ(int accion, int estado)
        {
            ValidarIndices(accion, estado);
            return tablaQ[accion, estado];
        }

        // Actualizar el valor Q para una acci�n y un estado
        public void ActualizarQ(int accion, int estado, float nuevoValor)
        {
            ValidarIndices(accion, estado);
            tablaQ[accion, estado] = nuevoValor;
        }

        // Obtener la mejor acci�n para un estado
        public int ObtenerMejorAccion(int estado)
        {
            float mejorQ = float.MinValue;
            List<int> mejoresAcciones = new List<int>();

            // Encuentra las acciones con el mejor valor Q
            for (int accion = 0; accion < numAcciones; accion++)
            {
                float qValor = ObtenerQ(accion, estado);
                if (qValor > mejorQ)
                {
                    mejorQ = qValor;
                    mejoresAcciones.Clear();
                    mejoresAcciones.Add(accion);
                }
                else if (qValor == mejorQ)
                {
                    mejoresAcciones.Add(accion);
                }
            }

            // Selecciona una acci�n aleatoria entre las mejores
            return mejoresAcciones[UnityEngine.Random.Range(0, mejoresAcciones.Count)];
        }


        // Valida si los �ndices de acci�n y estado son v�lidos
        private void ValidarIndices(int accion, int estado)
        {
            if (accion < 0 || accion >= numAcciones)
            {
                throw new ArgumentOutOfRangeException(nameof(accion), "Acci�n fuera de rango.");
            }

            if (estado < 0 || estado >= numEstados)
            {
                throw new ArgumentOutOfRangeException(nameof(estado), "Estado fuera de rango.");
            }
        }

        // Valida si el �ndice de estado es v�lido
        private void ValidarEstado(int estado)
        {
            if (estado < 0 || estado >= numEstados)
            {
                throw new ArgumentOutOfRangeException(nameof(estado), "Estado fuera de rango.");
            }
        }
    }
}
