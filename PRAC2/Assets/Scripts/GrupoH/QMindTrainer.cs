using NavigationDJIA.Interfaces;
using System;
using NavigationDJIA.World;
using QMind;
using QMind.Interfaces;
using UnityEngine;
using System.IO;
using System.Globalization;
using Components.QLearning;

namespace GrupoH
{
    public class QMindTrainer : IQMindTrainer
    {
        private TablaQLearning tablaQ; // la tabla Q
        public QMindTrainerParams parametros; // Parámetros de entrenamiento
        private WorldInfo mundo; // Información del mundo
        private INavigationAlgorithm algoritmoNavegacion; // Algoritmo de navegación para el enemigo
        private float recompensaTotal; // Acumula la recompensa total del episodio actual

        private int accionAnterior;
        private CellInfo posicionAnteriorAgente;

        public int CurrentEpisode { get; private set; }
        public int CurrentStep { get; private set; }
        public CellInfo AgentPosition { get; private set; }
        public CellInfo OtherPosition { get; private set; }
        public float Return => recompensaTotal ; // Devuelve la recompensa total acumulada
        public float ReturnAveraged { get; private set; } // Calcula el promedio en ReiniciarEpisodio

        public event EventHandler OnEpisodeStarted;
        public event EventHandler OnEpisodeFinished;

        public const string RUTA_TABLA = "Assets/Scripts/GrupoH/TablaQ.csv";


        public void Initialize(QMindTrainerParams qMindTrainerParams, WorldInfo worldInfo, INavigationAlgorithm navigationAlgorithm)
        {
            Debug.Log("QMindTrainer: initialized");
            //Time.timeScale = 5f;
            // Inicialización
            parametros = qMindTrainerParams;

            mundo = worldInfo;
            algoritmoNavegacion = navigationAlgorithm;

            if (File.Exists(RUTA_TABLA))
            {
                tablaQ = new TablaQLearning(RUTA_TABLA);
                //CargarTablaQ();
                Debug.Log("Tabla Q cargada correctamente.");
            }
            else
            {
                tablaQ = new TablaQLearning();
                Debug.Log("Se ha creado una nueva tabla Q.");
            }


            AgentPosition = mundo.RandomCell();
            OtherPosition = mundo.RandomCell();
            CurrentEpisode = 0;
            CurrentStep = 0;
            ReturnAveraged = 0; 

            OnEpisodeStarted?.Invoke(this, EventArgs.Empty);
        }



        public void DoStep(bool train)
        {
            // Obtener el estado actual basado en la posición del agente y el enemigo
            int estadoActual = ObtenerEstado(AgentPosition, OtherPosition, mundo);

            // Elegir la acción (considerando si está en modo de entrenamiento o no)
            int accion = SeleccionarAccion(estadoActual, train);

            // Realizar la acción y obtener la nueva posición
            CellInfo nuevaPosicion = EjecutarAccion(accion);

            // Calcular el nuevo estado tras el movimiento
            int nuevoEstado = ObtenerEstado(nuevaPosicion, OtherPosition, mundo);

            
            Debug.Log($"DoStep - Estado actual: {estadoActual}, Acción elegida: {accion}, Nueva posición: ({nuevaPosicion.x}, {nuevaPosicion.y}), Nuevo estado: {nuevoEstado}");

            // Calcular recompensa y actualizar tabla Q si está en modo entrenamiento
            if (train)
            {
                float recompensa = CalcularRecompensa(nuevaPosicion);
                recompensaTotal += recompensa;
                tablaQ.CalcularQ(estadoActual, accion, recompensa, nuevoEstado,parametros.alpha,parametros.gamma);
            }


            // Actualizar la posición del agente y del enemigo
            posicionAnteriorAgente = AgentPosition;
            accionAnterior = accion;
            AgentPosition = nuevaPosicion;

            // Mover al enemigo
            OtherPosition = GrupoH.Movimiento.MovimientoEnemigo(algoritmoNavegacion, OtherPosition, AgentPosition);

            // Verificar si el episodio debe terminar
            if (AgentPosition.Equals(OtherPosition) || CurrentStep >= parametros.maxSteps)
            {
                Debug.Log("DoStep - Episodio terminado");
                OnEpisodeFinished?.Invoke(this, EventArgs.Empty);
                ReiniciarEpisodio();
            }
            float nuevaDistancia = AgentPosition.Distance(OtherPosition, CellInfo.DistanceType.Manhattan);
            if (nuevaDistancia >= 10)
            {
                Debug.Log("Agente se ha alejado lo suficiente. Fin del episodio.");
                OnEpisodeFinished?.Invoke(this, EventArgs.Empty);
                ReiniciarEpisodio();
                return;
            }
            else
            {
                CurrentStep++;
            }
            Debug.Log($" De {AgentPosition} -> {nuevaPosicion} | Enemigo en {OtherPosition} | Distancia: {nuevaPosicion.Distance(OtherPosition, CellInfo.DistanceType.Manhattan)}");

        }

        private int ObtenerEstado(CellInfo agente, CellInfo enemigo, WorldInfo mundo)
        {
            // Posición relativa del enemigo
            int deltaX = Mathf.Clamp(enemigo.x - agente.x, -1, 1) + 1; // [0,2]
            int deltaY = Mathf.Clamp(enemigo.y - agente.y, -1, 1) + 1; // [0,2]
            int posicionRelativa = deltaX * 3 + deltaY; // [0,8]

            // Caminabilidad del agente (N,E,S,O)
            bool[] direcciones = new bool[4];
            direcciones[0] = agente.y + 1 < mundo.WorldSize.y && mundo[agente.x, agente.y + 1].Walkable; // Norte
            direcciones[1] = agente.x + 1 < mundo.WorldSize.x && mundo[agente.x + 1, agente.y].Walkable; // Este
            direcciones[2] = agente.y - 1 >= 0 && mundo[agente.x, agente.y - 1].Walkable; // Sur
            direcciones[3] = agente.x - 1 >= 0 && mundo[agente.x - 1, agente.y].Walkable; // Oeste

            // Codificar la caminabilidad como un número binario
            int codAccesibilidad = 0;
            for (int i = 0; i < 4; i++)
                if (direcciones[i]) codAccesibilidad |= (1 << i); // Suma potencias de 2

            int estado = posicionRelativa * 16 + codAccesibilidad;

            Debug.Log($"ObtenerEstado - Relación enemigo: {posicionRelativa}, Caminabilidad: {codAccesibilidad}, Estado: {estado}");
            return estado;
        }


        private int SeleccionarAccion(int estado, bool explorar)
        {
            float probabilidad = UnityEngine.Random.Range(0f, 1f);

            if (explorar && probabilidad < parametros.epsilon)
            {
                // Acción aleatoria
                int accionAleatoria = UnityEngine.Random.Range(0, 4);
                Debug.Log($"SeleccionarAccion - Explorando. Acción aleatoria: {accionAleatoria}");
                return accionAleatoria;
            }
            else
            {
                // Mejor acción según tabla Q
                int mejorAccion = tablaQ.ObtenerMejorAccion(estado);
                Debug.Log($"SeleccionarAccion - Explotando. Mejor acción: {mejorAccion} para estado: {estado}");
                return mejorAccion;
            }
        }



        private CellInfo EjecutarAccion(int accion)
        {
            CellInfo nuevaPosicion = GrupoH.Movimiento.MovimientoAgente(accion, AgentPosition, mundo);

            if (!nuevaPosicion.Walkable)
            {
                Debug.LogWarning($"Movimiento inválido detectado. Acción: {accion}. Manteniendo posición...");
                return AgentPosition; // Mantener posición si no es válida
            }

            Debug.Log($"EjecutarAccion - Acción: {accion}, Nueva posición: ({nuevaPosicion.x}, {nuevaPosicion.y})");
            return nuevaPosicion;
        }


        private float CalcularRecompensa(CellInfo nuevaCelda)
        {
            float distanciaActual = AgentPosition.Distance(OtherPosition, CellInfo.DistanceType.Manhattan);
            float nuevaDistancia = nuevaCelda.Distance(OtherPosition, CellInfo.DistanceType.Manhattan);

            if (!nuevaCelda.Walkable)
                return -10f;

            if (nuevaCelda.Equals(OtherPosition))
                return -100f;

            if (nuevaDistancia > distanciaActual)
            {
                if(nuevaDistancia> 5f)
                {
                    return 50f;
                }
                return 20f;
            }
                
                

            if (nuevaDistancia < distanciaActual)
                return -5f;

            return 0;
 
        }

        private float sumaDeRetornos = 0f; // Acumula los retornos de todos los episodios
        private int episodiosTotales = 0;  // Contador de episodios totales

        private void ReiniciarEpisodio()
        {
            // Incrementar el contador de episodios
            episodiosTotales++;

            // Cálculo del promedio usando media ponderada exponencial
            ReturnAveraged = Mathf.Round((ReturnAveraged * 0.9f + Return * 0.1f) * 100) / 100;

            // También puedes calcular el promedio acumulado (opcional)
            sumaDeRetornos += Return;
            float promedioGlobal = Mathf.Round((sumaDeRetornos / episodiosTotales) * 100) / 100;

            Debug.Log($"Episodio {CurrentEpisode} finalizado: Total Reward = {Return}, Averaged Reward = {ReturnAveraged}, Promedio Global = {promedioGlobal}");

            // Actualizar epsilon dinámicamente para reducir exploración con el tiempo
            float t = (float)CurrentEpisode / parametros.episodes;

            if (t < 0.8f)
            {
                parametros.epsilon = Mathf.Lerp(0.7f, 0.3f, t / 0.8f); // Episodios 0-80%: de 0.7 a 0.3
            }

            Debug.Log($"Epsilon actualizado: {parametros.epsilon}");

            // Reiniciar variables del episodio
            recompensaTotal = 0f; // Reiniciar acumulador de recompensa
            CurrentStep = 0;
            CurrentEpisode++;

            // Actualizar posiciones iniciales del agente y el enemigo
            AgentPosition = mundo.RandomCell();
            OtherPosition = mundo.RandomCell();

            // Guardar la tabla Q periódicamente
            if (CurrentEpisode % parametros.episodesBetweenSaves == 0)
            {
                GuardarTablaQ();
            }

            // Disparar evento para iniciar un nuevo episodio
            OnEpisodeStarted?.Invoke(this, EventArgs.Empty);

            if (CurrentEpisode % 100 == 0)
            {
                int estadoEjemplo = UnityEngine.Random.Range(0, tablaQ.numEstados);
                string log = $"[DEBUG Q] Estado {estadoEjemplo}: ";
                for (int a = 0; a < tablaQ.numAcciones; a++)
                {
                    log += $"A{a}={tablaQ.ObtenerQ(a, estadoEjemplo):F2} ";
                }
                Debug.Log(log);
            }

        }



        public void GuardarTablaQ()
        {
            Debug.Log("Tabla Q guardada en archivo CSV.");

            try
            {
                using (StreamWriter writer = new StreamWriter(RUTA_TABLA))
                {
                    // Recorrer todos los estados y acciones de la tabla Q
                    for (int estado = 0; estado < tablaQ.numEstados; estado++)
                    {
                        for (int accion = 0; accion < tablaQ.numAcciones; accion++)
                        {
                            float qValor = tablaQ.ObtenerQ(accion, estado);
                            // Usar punto como separador decimal
                            writer.WriteLine($"{estado},{accion},{qValor.ToString(CultureInfo.InvariantCulture)}");
                        }
                    }
                }
                Debug.Log($"Tabla Q guardada exitosamente en {RUTA_TABLA}");
                Debug.Log($"[CargarTablaQ] Total de entradas cargadas: {tablaQ.numEstados * tablaQ.numAcciones}");

            }
            catch (Exception ex)
            {
                Debug.LogError($"Error al guardar la tabla Q en el archivo CSV: {ex.Message}");
            }
        }
    
        private void OnGUI()
        {
            GUIStyle guiStyle = new GUIStyle(GUI.skin.label);
            guiStyle.fontSize = 22;
            guiStyle.fontStyle = FontStyle.Bold;
            guiStyle.normal.textColor = Color.black;

            // Mostrar el episodio y el paso actual
            GUI.Label(new Rect(10, 10, 300, 30), $"Episode: {CurrentEpisode} [{CurrentStep}]", guiStyle);

            // Mostrar recompensa promedio (ReturnAveraged)
            GUI.Label(new Rect(10, 40, 300, 30), $"Averaged reward: {ReturnAveraged}", guiStyle);

            // Mostrar recompensa total (Return)
            GUI.Label(new Rect(10, 70, 300, 30), $"Total reward: {Return}", guiStyle);
        }

    }
}
