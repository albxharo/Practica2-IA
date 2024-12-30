using NavigationDJIA.Interfaces;
using System;
using NavigationDJIA.World;
using QMind;
using QMind.Interfaces;
using UnityEngine;
using System.IO;

namespace GrupoH
{
    public class QMindTrainer : IQMindTrainer
    {
        private TablaQLearning tablaQ; // la tabla Q
        private QMindTrainerParams parametros; // Par�metros de entrenamiento
        private WorldInfo mundo; // Informaci�n del mundo
        private INavigationAlgorithm algoritmoNavegacion; // Algoritmo de navegaci�n para el enemigo

        private int accionAnterior;
        private CellInfo posicionAnteriorAgente;

        public int CurrentEpisode { get; private set; }
        public int CurrentStep { get; private set; }
        public CellInfo AgentPosition { get; private set; }
        public CellInfo OtherPosition { get; private set; }
        public float Return { get; }
        public float ReturnAveraged { get; }
        public event EventHandler OnEpisodeStarted;
        public event EventHandler OnEpisodeFinished;

        private const string RUTA_CSV = "Assets/Scripts/GrupoH/TablaQ.csv";
        public void Initialize(QMindTrainerParams qMindTrainerParams, WorldInfo worldInfo, INavigationAlgorithm navigationAlgorithm)
        {
            Debug.Log("QMindTrainer: initialized");

            // Inicializaci�n
            parametros = qMindTrainerParams;
            mundo = worldInfo;
            algoritmoNavegacion = navigationAlgorithm;

            // Crear tabla Q
            int numAcciones = 4; // Norte, Sur, Este, Oeste
            int numEstados = 16 * 9; // Ejemplo: total de estados posibles
            tablaQ = new TablaQLearning(numAcciones, numEstados);

            // Cargar tabla Q si existe
            CargarTablaQ();

            AgentPosition = mundo.RandomCell();
            OtherPosition = mundo.RandomCell();
            OnEpisodeStarted?.Invoke(this, EventArgs.Empty);
        }

        public void DoStep(bool train)
        {
            // Selecci�n de acci�n
            int accion = SeleccionarAccion();
            
            // Realizar acci�n
            CellInfo nuevaPosicion = EjecutarAccion(accion);
            
            // Obtener recompensa
            float recompensa = CalcularRecompensa(nuevaPosicion);
            
            // Actualizar tabla Q (solo si est� en modo entrenamiento)
            if (train)
            {
                ActualizarQ(accion, recompensa, nuevaPosicion);
            }

            // Actualizar estado del juego
            posicionAnteriorAgente = AgentPosition;
            accionAnterior = accion;
            AgentPosition = nuevaPosicion;

            // Mover enemigo
            OtherPosition = QMind.Utils.MoveOther(algoritmoNavegacion, OtherPosition, AgentPosition);

            // Verificar condiciones de finalizaci�n
            if (AgentPosition.Equals(OtherPosition) || CurrentStep >= parametros.maxSteps)
            {
                OnEpisodeFinished?.Invoke(this, EventArgs.Empty);
                ReiniciarEpisodio();
            }
            else
            {
                CurrentStep++;
            }
        }
        private int SeleccionarAccion()
        {
            // Selecci�n e-greedy
            float probabilidad = UnityEngine.Random.Range(0f, 1f);
            if (probabilidad < parametros.epsilon)
            {
                return UnityEngine.Random.Range(0, 4); // Acci�n aleatoria
            }
            else
            {
                int estado = ObtenerEstado(AgentPosition);
                return tablaQ.ObtenerMejorAccion(estado); // Mejor acci�n seg�n tabla Q
            }
        }
        private CellInfo EjecutarAccion(int accion)
        {
            CellInfo nuevaPosicion = QMind.Utils.MoveAgent(accion, AgentPosition, mundo);

            if (!nuevaPosicion.Walkable)
            {
                Debug.LogWarning("Acci�n inv�lida. Penalizando.");
                nuevaPosicion = AgentPosition; // Mantener la posici�n actual
            }

            return nuevaPosicion;
        }

        private float CalcularRecompensa(CellInfo nuevaPosicion)
        {
            if (nuevaPosicion.Equals(OtherPosition))
            {
                return -100f; // Penalizaci�n alta si el enemigo atrapa al agente
            }

            float distanciaActual = Vector2.Distance(new Vector2(AgentPosition.x, AgentPosition.y),
                                                     new Vector2(OtherPosition.x, OtherPosition.y));
            float nuevaDistancia = Vector2.Distance(new Vector2(nuevaPosicion.x, nuevaPosicion.y),
                                                    new Vector2(OtherPosition.x, OtherPosition.y));

            return nuevaDistancia > distanciaActual ? 10f : -1f; // Recompensa positiva si se aleja
        }

        private void ActualizarQ(int accion, float recompensa, CellInfo nuevaPosicion)
        {
            int estadoActual = ObtenerEstado(AgentPosition);
            int nuevoEstado = ObtenerEstado(nuevaPosicion);

            float qActual = tablaQ.ObtenerQ(accion, estadoActual);
            float mejorQ = tablaQ.ObtenerMejorAccion(nuevoEstado);

            // F�rmula de actualizaci�n de Q-Learning
            float nuevoQ = qActual + parametros.alpha * (recompensa + parametros.gamma * mejorQ - qActual);
            tablaQ.ActualizarQ(accion, estadoActual, nuevoQ);
        }

        private int ObtenerEstado(CellInfo posicion)
        {
            return posicion.x * mundo.WorldSize.x + posicion.y; // Ejemplo b�sico de identificaci�n de estado
        }

        private void ReiniciarEpisodio()
        {
            AgentPosition = mundo.RandomCell();
            OtherPosition = mundo.RandomCell();
            CurrentStep = 0;
            CurrentEpisode++;

            // Guardar la tabla Q cada ciertos episodios
            if (CurrentEpisode % parametros.episodesBetweenSaves == 0)
            {
                GuardarTablaQ();
            }

            OnEpisodeStarted?.Invoke(this, EventArgs.Empty);
        }

        public void GuardarTablaQ()
        {
            tablaQ.GuardarEnCSV(RUTA_CSV);
            Debug.Log("Tabla Q guardada en archivo CSV.");
        }

        public void CargarTablaQ()
        {
            if (File.Exists(RUTA_CSV))
            {
                tablaQ.CargarDesdeCSV(RUTA_CSV);
                Debug.Log("Tabla Q cargada desde archivo CSV.");
            }
            else
            {
                Debug.LogWarning("No se encontr� un archivo CSV para cargar la tabla Q.");
            }
        }
    }
    }
}
