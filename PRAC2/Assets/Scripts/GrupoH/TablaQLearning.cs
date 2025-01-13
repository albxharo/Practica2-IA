using System;
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
        public int numAcciones; // Número de acciones (filas)
        public int numEstados; // Número de estados (columnas)

        public TablaQLearning(int numAcciones, int numEstados)
        {
            this.numAcciones = numAcciones;
            this.numEstados = numEstados;
            tablaQ = new float[numAcciones, numEstados];

            // Inicializar la tabla Q con valores 0
            for (int accion = 0; accion < numAcciones; accion++)
            {
                for (int estado = 0; estado < numEstados; estado++)
                {
                    tablaQ[accion, estado] = 0f;
                }
            }
        }

        // Obtener el valor Q para una acción y un estado
        public float ObtenerQ(int accion, int estado)
        {
            ValidarIndices(accion, estado);
            return tablaQ[accion, estado];
        }

        // Actualizar el valor Q para una acción y un estado
        public void ActualizarQ(int accion, int estado, float nuevoValor)
        {
            ValidarIndices(accion, estado);
            tablaQ[accion, estado] = nuevoValor;
        }

        // Obtener la mejor acción para un estado
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

            // Selecciona una acción aleatoria entre las mejores
            return mejoresAcciones[UnityEngine.Random.Range(0, mejoresAcciones.Count)];
        }


        // Valida si los índices de acción y estado son válidos
        private void ValidarIndices(int accion, int estado)
        {
            if (accion < 0 || accion >= numAcciones)
            {
                throw new ArgumentOutOfRangeException(nameof(accion), "Acción fuera de rango.");
            }

            if (estado < 0 || estado >= numEstados)
            {
                throw new ArgumentOutOfRangeException(nameof(estado), "Estado fuera de rango.");
            }
        }

        // Valida si el índice de estado es válido
        private void ValidarEstado(int estado)
        {
            if (estado < 0 || estado >= numEstados)
            {
                throw new ArgumentOutOfRangeException(nameof(estado), "Estado fuera de rango.");
            }
        }
    }
}
