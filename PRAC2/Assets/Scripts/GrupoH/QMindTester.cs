using System;
using NavigationDJIA.World;
using NavigationDJIA.Interfaces;
using QMind.Interfaces;
using UnityEngine;
using Components.QLearning;
using Components;
using NavigationDJIA.Algorithms.AStar;

namespace GrupoH
{
    public class QMindTester : MonoBehaviour
    {
        public Movable agent;
        public Movable oponente;
        public float velocidad = 1.0f;
        public string qMindClass;
        public int escenarios = 10; // Número de escenarios a evaluar

        private IQMind qMind;
        private WorldInfo mundo;
        private CellInfo posicionAgente;
        private CellInfo posicionOponente;

        private int pasosTotales = 0;
        private int capturas = 0;

        void Start()
        {
            mundo = WorldManager.Instance.WorldInfo;

            // Inicializar la clase IQMind
            Type qMindType = Type.GetType(qMindClass);
            if (qMindType == null)
            {
                Debug.LogError($"Clase {qMindClass} no encontrada.");
                enabled = false;
                return;
            }

            qMind = (IQMind)Activator.CreateInstance(qMindType);
            qMind.Initialize(mundo);

            EvaluarAgente();
        }

        void EvaluarAgente()
        {
            for (int i = 0; i < escenarios; i++)
            {
                Debug.Log($"Iniciando escenario {i + 1}/{escenarios}...");
                SimularEscenario();
            }

            float promedioPasos = pasosTotales / (float)escenarios;
            Debug.Log($"Promedio de pasos: {promedioPasos}");
            Debug.Log($"Capturas totales: {capturas}");
        }

        void SimularEscenario()
        {
            posicionAgente = mundo.RandomCell();
            posicionOponente = mundo.RandomCell();

            agent.transform.position = mundo.ToWorldPosition(posicionAgente);
            oponente.transform.position = mundo.ToWorldPosition(posicionOponente);

            int pasos = 0;
            bool capturado = false;

            while (pasos < 1000) // Límite de pasos
            {
                if (agent.DestinationReached && oponente.DestinationReached)
                {
                    // Movimiento del agente
                    CellInfo nuevaPosicionAgente = qMind.GetNextStep(posicionAgente, posicionOponente);
                    if (nuevaPosicionAgente != null)
                    {
                        posicionAgente = nuevaPosicionAgente;
                        agent.destination = mundo.ToWorldPosition(posicionAgente);
                    }

                    // Movimiento del oponente
                    CellInfo[] camino = new AStarNavigation().GetPath(posicionOponente, posicionAgente, 1);
                    if (camino.Length > 0)
                    {
                        posicionOponente = camino[0];
                        oponente.destination = mundo.ToWorldPosition(posicionOponente);
                    }

                    // Verificar captura
                    if (posicionAgente == posicionOponente)
                    {
                        capturas++;
                        Debug.Log($"Agente capturado en {pasos} pasos.");
                        capturado = true;
                        break;
                    }

                    pasos++;
                }
            }

            pasosTotales += pasos;

            if (!capturado)
            {
                Debug.Log($"Agente evitó al oponente durante {pasos} pasos.");
            }
        }
    }
}


