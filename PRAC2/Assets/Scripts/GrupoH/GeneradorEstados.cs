using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GrupoH
{
    public class GeneradorEstados
    {
        public List<Estado> Estados = new List<Estado>(); // Lista de todos los estados posibles

        public void GenerarEstados()
        {
            int id = 0;

            // Generar todas las combinaciones de los posibles movimientos
            List<bool[]> combinacionesMovimiento = GenerarCombinacionesMovimiento();

            // Generar todas las combinaciones de posici�n relativa
            for (int x = -1; x <= 1; x++) // Posici�n relativa del enemigo en X
            {
                for (int y = -1; y <= 1; y++) // Posici�n relativa del enemigo en Y
                {
                    Vector2 posicionEnemigo = new Vector2(x, y);

                    // Combinar con todas las combinaciones de accesibilidad
                    foreach (bool[] accesibilidad in combinacionesMovimiento)
                    {
                        // Crear un nuevo estado
                        Estado estado = new Estado(id, posicionEnemigo, accesibilidad);
                        Estados.Add(estado);

                        id++; // Incrementar el identificador �nico
                    }
                }
            }
        }

        // M�todo para generar todas las combinaciones de true/false para las direcciones
        private List<bool[]> GenerarCombinacionesMovimiento()
        {
            List<bool[]> combinaciones = new List<bool[]>();

            // 16 combinaciones de true/false
            for (int i = 0; i < 16; i++) 
            {
                // Almacenamos los numeros en binario
                bool[] accesibilidad = new bool[4];

                // Convertir el n�mero a combinaciones de true/false directamente

                accesibilidad[0] = (i % 2) == 1;         // Norte
                accesibilidad[1] = ((i / 2) % 2) == 1;   // Este
                accesibilidad[2] = ((i / 4) % 2) == 1;   // Sur
                accesibilidad[3] = ((i / 8) % 2) == 1;   // Oeste

                combinaciones.Add(accesibilidad);
            }

            return combinaciones;
        }
    }
}
