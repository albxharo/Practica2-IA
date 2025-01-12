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

        private const string RUTA_TABLA = "Assets/Scripts/GrupoH/TablaQ.csv";

        int numAcciones = 4; // Norte, Sur, Este, Oeste
        int numEstados = 16 * 9; // Total de estados posibles

        public void Initialize(QMindTrainerParams qMindTrainerParams, WorldInfo worldInfo, INavigationAlgorithm navigationAlgorithm)
        {
            Debug.Log("QMindTrainer: initialized");

            // Inicialización
            parametros = qMindTrainerParams;
            parametros.epsilon = Mathf.Lerp(1f, 0.1f, (float)CurrentEpisode / parametros.episodes);


            mundo = worldInfo;
            algoritmoNavegacion = navigationAlgorithm;

            // Crear tabla Q
            tablaQ = new TablaQLearning(numAcciones, numEstados);

            // Cargar tabla Q 
            if (File.Exists(RUTA_TABLA))
            {
                CargarTablaQ();
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
            // Obtener el estado actual
            int estadoActual = ObtenerEstado(AgentPosition, OtherPosition);

            // Seleccionar la acción (considera si está en modo entrenamiento o no)
            int accion = SeleccionarAccion(estadoActual, train);

            // Realizar la acción y obtener la nueva posición
            CellInfo nuevaPosicion = EjecutarAccion(accion);

            // Calcular la recompensa basada en la nueva posición
            float recompensa = CalcularRecompensa(nuevaPosicion);

            // Acumular recompensa total
            recompensaTotal += recompensa;


            // Actualizar la tabla Q si está en modo entrenamiento
            if (train)
            {
                int nuevoEstado = ObtenerEstado(nuevaPosicion, OtherPosition);
                ActualizarQ(estadoActual, accion, recompensa, nuevoEstado);
            }

            // Actualizar el estado del agente
            posicionAnteriorAgente = AgentPosition;
            accionAnterior = accion;
            AgentPosition = nuevaPosicion;

            // Mover al enemigo
            OtherPosition = GrupoH.Movimiento.MovimientoEnemigo(algoritmoNavegacion, OtherPosition, AgentPosition);

            // Verificar si el episodio termina
            if (AgentPosition.Equals(OtherPosition) || CurrentStep >= parametros.maxSteps)
            {
                OnEpisodeFinished?.Invoke(this, EventArgs.Empty);
                ReiniciarEpisodio();
            }
            else
            {
                CurrentStep++;
            }

            // Depuración
            Debug.Log($"Paso: {CurrentStep}, Recompensa: {recompensa}, Recompensa Total: {recompensaTotal}");
        }


        private int SeleccionarAccion(int estado, bool explorar)
        {
            float probabilidad = UnityEngine.Random.Range(0f, 1f);
            if (explorar && probabilidad < parametros.epsilon)
            {
                // Acción aleatoria
                return UnityEngine.Random.Range(0, 4);
            }
            else
            {
                // Mejor acción según tabla Q
                return tablaQ.ObtenerMejorAccion(estado);
            }
        }

        private CellInfo EjecutarAccion(int accion)
        {
            CellInfo nuevaPosicion = GrupoH.Movimiento.MovimientoAgente(accion, AgentPosition, mundo);

            if (!nuevaPosicion.Walkable)
            {
                Debug.LogWarning($"Movimiento inválido detectado. Acción: {accion}. Buscando alternativa...");

                // Intentar todas las acciones en orden de prioridad
                for (int nuevaAccion = 0; nuevaAccion < 4; nuevaAccion++)
                {
                    CellInfo posicionAlternativa = GrupoH.Movimiento.MovimientoAgente(nuevaAccion, AgentPosition, mundo);
                    if (posicionAlternativa.Walkable)
                    {
                        Debug.Log($"Acción alternativa seleccionada: {nuevaAccion}");
                        return posicionAlternativa;
                    }
                }

                // Si ninguna acción es válida, penalizar quedarse en su lugar
                recompensaTotal -= 50f; // Penalización por estar atrapado
                return AgentPosition;
            }

            return nuevaPosicion;
        }





        private float CalcularRecompensa(CellInfo celda)
        {
            // Constantes para las penalizaciones y recompensas
            const float BAJA = 10f;
            const float MEDIA = 50f;
            const float ALTA = 100f;
            const float CRITICA = 1000f;

            float refuerzo = 0f;

            // Caso crítico: el agente es atrapado por el oponente
            if (celda.Equals(OtherPosition))
            {
                refuerzo -= CRITICA; // Penalización máxima
                return refuerzo;
            }
            /*if (celda.Equals(AgentPosition))
            {
                return -30f; // Penalización significativa por no moverse
            }
            
            // Cálculo de las distancias Manhattan (actual y nueva)
            int distanciaActual = Mathf.Abs(AgentPosition.x - OtherPosition.x) +
                                  Mathf.Abs(AgentPosition.y - OtherPosition.y);
            int nuevaDistancia = Mathf.Abs(celda.x - OtherPosition.x) +
                                 Mathf.Abs(celda.y - OtherPosition.y);
            */
            // Cálculo de las distancias Manhattan (actual y nueva)
            float distanciaActual = AgentPosition.Distance(OtherPosition, CellInfo.DistanceType.Manhattan);
            float nuevaDistancia = celda.Distance(OtherPosition, CellInfo.DistanceType.Manhattan);
            
            // Evaluar el cambio de distancia
            if (nuevaDistancia > distanciaActual)
            {
                // Recompensa base por alejarse
                refuerzo += ALTA;

                // Recompensa adicional si la distancia es significativamente grande
                if (nuevaDistancia >= 10)
                {
                    refuerzo += MEDIA;
                }
            }
            else
            {
                // Penalización base por acercarse
                refuerzo -= BAJA;

                // Penalización adicional si la distancia es peligrosamente baja
                if (nuevaDistancia <= 4)
                {
                    refuerzo -= ALTA;
                }
            }

            return refuerzo;
        }



        private void ActualizarQ(int estadoActual, int accion, float recompensa, int nuevoEstado)
        {

            float qActual = tablaQ.ObtenerQ(accion, estadoActual);
            float mejorQ = tablaQ.ObtenerMejorAccion(nuevoEstado);

            // Fórmula de actualización Q-Learning
            float nuevoQ = qActual + parametros.alpha * (recompensa + parametros.gamma * mejorQ - qActual);
            tablaQ.ActualizarQ(accion, estadoActual, nuevoQ);
            Debug.Log($"Estado: {estadoActual}, Acción: {accion}, Q antes: {qActual}, Q después: {nuevoQ}");

        }
        private int ObtenerEstado(CellInfo agente, CellInfo oponente)
        {
            // Calcular la posición relativa del oponente respecto al agente
            int deltaX = Mathf.Clamp(oponente.x - agente.x, -1, 1) + 1; // Rango [0, 2]
            int deltaY = Mathf.Clamp(oponente.y - agente.y, -1, 1) + 1; // Rango [0, 2]
            int posicionRelativa = deltaX * 3 + deltaY; // Combinar en un rango [0, 8]

            // Identificar celda única del agente
            int celdaAgente = agente.x * mundo.WorldSize.x + agente.y;

            // Combinar la posición del agente con la posición relativa del oponente
            return (celdaAgente * 9 + posicionRelativa) % numEstados;
        }

        private float sumaDeRetornos = 0f; // Acumula los retornos de todos los episodios
        private void ReiniciarEpisodio()
        {
            // Calcular recompensa promedio al final del episodio
            if (CurrentStep > 0)
            {
                ReturnAveraged = recompensaTotal / CurrentStep;
            }
            Debug.Log($"Episodio {CurrentEpisode} finalizado: Total Reward = {recompensaTotal}, Average Reward = {ReturnAveraged}");
            // Reiniciar variables para el nuevo episodio
            recompensaTotal = 0f; // Reiniciar acumulador
            CurrentStep = 0;
            CurrentEpisode++;

            // Actualizar ReturnAveraged
            sumaDeRetornos += Return;
            ReturnAveraged = sumaDeRetornos / (CurrentEpisode + 1); // Promedio hasta el episodio actual


            AgentPosition = mundo.RandomCell();
            OtherPosition = mundo.RandomCell();

            // Guardar la tabla Q cada ciertos episodios
            if (CurrentEpisode % parametros.episodesBetweenSaves == 0)
            {
                GuardarTablaQ();
            }

            OnEpisodeStarted?.Invoke(this, EventArgs.Empty);
            

        }


        public void GuardarTablaQ()
        {
            Debug.Log("Tabla Q guardada en archivo CSV.");
            try
            {
                using (StreamWriter writer = new StreamWriter(RUTA_TABLA))
                {
                    // Escribir encabezados (opcional)
                    writer.WriteLine("Estado,Accion,Q-Valor");

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
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error al guardar la tabla Q en el archivo CSV: {ex.Message}");
            }
        }


        public void CargarTablaQ()
        {
            try
            {
                using (StreamReader reader = new StreamReader(RUTA_TABLA))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(',');
                        int estado = int.Parse(parts[0]);
                        int accion = int.Parse(parts[1]);
                        float qValor = float.Parse(parts[2]);
                        tablaQ.ActualizarQ(accion, estado, qValor);
                    }
                }
                Debug.Log("Tabla Q cargada exitosamente.");
            }
            catch (Exception ex)
            {
                //Debug.LogError($"Error al cargar la tabla Q: {ex.Message}");
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
