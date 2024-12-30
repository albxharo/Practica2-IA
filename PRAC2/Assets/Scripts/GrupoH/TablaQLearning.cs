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
        public int numAcciones; // N�mero de acciones (filas)
        public int numEstados; // N�mero de estados (columnas)

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

        // Obtener el valor Q para una acci�n y un estado
        public float ObtenerQ(int accion, int estado)
        {
            return tablaQ[accion, estado];
        }

        // Actualizar el valor Q para una acci�n y un estado
        public void ActualizarQ(int accion, int estado, float nuevoValor)
        {
            tablaQ[accion, estado] = nuevoValor;
        }

        // Obtener la mejor acci�n para un estado
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
    }
}
