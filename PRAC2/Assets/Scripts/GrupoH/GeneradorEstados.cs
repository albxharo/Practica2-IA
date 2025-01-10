using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GrupoH
{
    public class GeneradorEstados
    {
        public List<Estado> Estados { get; } = new List<Estado>(); // Lista de todos los estados posibles

        public void GenerarEstados()
        {
            int id = 0;

            // Generar todas las combinaciones de los posibles movimientos
            List<bool[]> combinacionesMovimiento = GenerarCombinacionesMovimiento();

            // Generar todas las combinaciones de posición relativa
            for (int x = -1; x <= 1; x++) // Posición relativa del enemigo en X
            {
                for (int y = -1; y <= 1; y++) // Posición relativa del enemigo en Y
                {
                    Vector2 posicionEnemigo = new Vector2(x, y);

                    // Combinar con todas las combinaciones de accesibilidad
                    foreach (bool[] accesibilidad in combinacionesMovimiento)
                    {
                        // Crear un nuevo estado
                        Estado estado = new Estado(id, posicionEnemigo, accesibilidad);
                        Estados.Add(estado);

                        id++; // Incrementar el identificador único
                    }
                }
            }
        }

        // Método para generar todas las combinaciones de true/false para las direcciones
        private List<bool[]> GenerarCombinacionesMovimiento()
        {
            List<bool[]> combinaciones = new List<bool[]>();

            // 16 combinaciones de true/false
            for (int i = 0; i < 16; i++) 
            {
                // Almacenamos los numeros en binario
                combinaciones.Add(new bool[]
                // Convertir el número a combinaciones de true/false directamente
                {
                    (i % 2) == 1,         // Norte
                    ((i / 2) % 2) == 1,   // Este
                    ((i / 4) % 2) == 1,   // Sur
                    ((i / 8) % 2) == 1    // Oeste
                });
            }

            return combinaciones;
        }
    }
}

