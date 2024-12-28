using System.Collections;
using System.Collections.Generic;
using System.IO;
using NavigationDJIA.World;
using UnityEngine;

namespace GrupoH
{
    public class TablaQLearning 
    {
        private float[,] tablaQ; // Matriz de valores Q
        private int numAcciones; // Número de acciones (filas)
        private int numEstados; // Número de estados (columnas)

        public TablaQLearning(int numAcciones, int numEstados)
        {
            this.numAcciones = numAcciones;
            this.numEstados = numEstados;
            tablaQ = new float[numAcciones, numEstados];

            // Inicializar la tabla Q con valores 0
            for (int i = 0; i < numAcciones; i++)
            {
                for (int j = 0; j < numEstados; j++)
                {
                    tablaQ[i, j] = 0f;
                }
            }
        }

        // Obtener el valor Q para una acción y un estado
        public float ObtenerQ(int accion, int estado)
        {
            return tablaQ[accion, estado];
        }

        // Actualizar el valor Q para una acción y un estado
        public void ActualizarQ(int accion, int estado, float nuevoValor)
        {
            tablaQ[accion, estado] = nuevoValor;
        }

        // Obtener la mejor acción para un estado
        public int ObtenerMejorAccion(int estado)
        {
            int mejorAccion = 0;
            float mejorQ = float.MinValue;

            for (int i = 0; i < numAcciones; i++)
            {
                if (tablaQ[i, estado] > mejorQ)
                {
                    mejorQ = tablaQ[i, estado];
                    mejorAccion = i;
                }
            }

            return mejorAccion;
        }

        // Guardar la tabla Q en un archivo CSV
        public void GuardarEnCSV(string rutaArchivo)
        {
            using (StreamWriter writer = new StreamWriter(rutaArchivo))
            {
                for (int i = 0; i < numAcciones; i++)
                {
                    string fila = "";
                    for (int j = 0; j < numEstados; j++)
                    {
                        fila += tablaQ[i, j].ToString("F2") + (j == numEstados - 1 ? "" : ",");
                    }
                    writer.WriteLine(fila);
                }
            }
            Debug.Log($"Tabla Q guardada en {rutaArchivo}");
        }

        // Cargar la tabla Q desde un archivo CSV
        public void CargarDesdeCSV(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo))
            {
                Debug.LogError($"El archivo {rutaArchivo} no existe.");
                return;
            }

            string[] lineas = File.ReadAllLines(rutaArchivo);
            for (int i = 0; i < lineas.Length; i++)
            {
                string[] valores = lineas[i].Split(',');
                for (int j = 0; j < valores.Length; j++)
                {
                    tablaQ[i, j] = float.Parse(valores[j]);
                }
            }
            Debug.Log($"Tabla Q cargada desde {rutaArchivo}");
        }
    }
}
