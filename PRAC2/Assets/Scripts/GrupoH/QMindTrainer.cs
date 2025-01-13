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
        public QMindTrainerParams parametros; // Par�metros de entrenamiento
        private WorldInfo mundo; // Informaci�n del mundo
        private INavigationAlgorithm algoritmoNavegacion; // Algoritmo de navegaci�n para el enemigo
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

            // Inicializaci�n
            parametros = qMindTrainerParams;


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
            // Obtener el estado actual basado en la posici�n del agente y el enemigo
            int estadoActual = ObtenerEstado(AgentPosition, OtherPosition);

            // Elegir la acci�n (considerando si est� en modo de entrenamiento o no)
            int accion = SeleccionarAccion(estadoActual, train);

            // Realizar la acci�n y obtener la nueva posici�n
            CellInfo nuevaPosicion = EjecutarAccion(accion);

            // Calcular el nuevo estado tras el movimiento
            int nuevoEstado = ObtenerEstado(nuevaPosicion, OtherPosition);

            // Logs detallados para depuraci�n
            Debug.Log($"DoStep - Estado actual: {estadoActual}, Acci�n elegida: {accion}, Nueva posici�n: ({nuevaPosicion.x}, {nuevaPosicion.y}), Nuevo estado: {nuevoEstado}");

            // Calcular recompensa y actualizar tabla Q si est� en modo entrenamiento
            if (train)
            {
                float recompensa = CalcularRecompensa(nuevaPosicion);
                ActualizarQ(estadoActual, accion, recompensa, nuevoEstado);

                Debug.Log($"DoStep - Recompensa obtenida: {recompensa}, Estado previo: {estadoActual}, Estado nuevo: {nuevoEstado}");
            }

            // Actualizar la posici�n del agente y del enemigo
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
            else
            {
                CurrentStep++;
            }
        }



        private int SeleccionarAccion(int estado, bool explorar)
        {
            float probabilidad = UnityEngine.Random.Range(0f, 1f);

            if (explorar && probabilidad < parametros.epsilon)
            {
                // Acci�n aleatoria
                int accionAleatoria = UnityEngine.Random.Range(0, 4);
                Debug.Log($"SeleccionarAccion - Explorando. Acci�n aleatoria: {accionAleatoria}");
                return accionAleatoria;
            }
            else
            {
                // Mejor acci�n seg�n tabla Q
                int mejorAccion = tablaQ.ObtenerMejorAccion(estado);
                Debug.Log($"SeleccionarAccion - Explotando. Mejor acci�n: {mejorAccion} para estado: {estado}");
                return mejorAccion;
            }
        }



        private CellInfo EjecutarAccion(int accion)
        {
            CellInfo nuevaPosicion = GrupoH.Movimiento.MovimientoAgente(accion, AgentPosition, mundo);

            if (!nuevaPosicion.Walkable)
            {
                Debug.LogWarning($"Movimiento inv�lido detectado. Acci�n: {accion}. Manteniendo posici�n...");
                return AgentPosition; // Mantener posici�n si no es v�lida
            }

            Debug.Log($"EjecutarAccion - Acci�n: {accion}, Nueva posici�n: ({nuevaPosicion.x}, {nuevaPosicion.y})");
            return nuevaPosicion;
        }







        private float CalcularRecompensa(CellInfo celda)
        {
            // Constantes ajustadas para recompensas y penalizaciones
            const float RECOMPENSA_BAJA = 10f;    // Recompensa por alejarse ligeramente
            const float RECOMPENSA_MEDIA = 50f;   // Recompensa por una distancia significativa
            const float RECOMPENSA_ALTA = 100f;   // Recompensa m�xima (objetivo ideal)
            const float PENALIZACION_BAJA = -10f; // Penalizaci�n por acercarse ligeramente
            const float PENALIZACION_MEDIA = -50f; // Penalizaci�n por estar cerca
            const float PENALIZACION_ALTA = -100f; // Penalizaci�n severa (ser atrapado)

            float recompensa = 0f;

            // Caso cr�tico: el agente es atrapado por el enemigo
            if (celda.Equals(OtherPosition))
            {
                recompensa = PENALIZACION_ALTA;
                Debug.Log("El agente fue atrapado por el enemigo. Penalizaci�n severa.");
                return recompensa;
            }

            // C�lculo de distancias Manhattan (actual y nueva)
            float distanciaActual = AgentPosition.Distance(OtherPosition, CellInfo.DistanceType.Manhattan);
            float nuevaDistancia = celda.Distance(OtherPosition, CellInfo.DistanceType.Manhattan);

            // Evaluar el cambio de distancia
            if (nuevaDistancia > distanciaActual)
            {
                // Alejarse del enemigo
                recompensa += RECOMPENSA_BAJA; // Base
                if (nuevaDistancia >= 10)
                {
                    recompensa += RECOMPENSA_MEDIA; // Recompensa adicional por gran distancia
                }
            }
            else if (nuevaDistancia < distanciaActual)
            {
                // Acercarse al enemigo
                recompensa += PENALIZACION_BAJA; // Penalizaci�n base
                if (nuevaDistancia <= 4)
                {
                    recompensa += PENALIZACION_MEDIA; // Penalizaci�n adicional por cercan�a peligrosa
                }
            }

            // Penalizaci�n por intentar una acci�n inv�lida
            if (!celda.Walkable)
            {
                recompensa += PENALIZACION_MEDIA;
                Debug.Log("Intento de moverse a una celda no v�lida. Penalizaci�n aplicada.");
            }

            return recompensa;
        }




        private void ActualizarQ(int estadoActual, int accion, float recompensa, int nuevoEstado)
        {

            float qActual = tablaQ.ObtenerQ(accion, estadoActual);
            float mejorQ = tablaQ.ObtenerMejorAccion(nuevoEstado);

            // F�rmula de actualizaci�n Q-Learning
            float nuevoQ = qActual + parametros.alpha * (recompensa + parametros.gamma * mejorQ - qActual);
            tablaQ.ActualizarQ(accion, estadoActual, nuevoQ);
            Debug.Log($"Estado: {estadoActual}, Acci�n: {accion}, Q antes: {qActual}, Q despu�s: {nuevoQ}");

        }
        private int ObtenerEstado(CellInfo agente, CellInfo oponente)
        {
            // Calcular la posici�n relativa del oponente respecto al agente
            int deltaX = Mathf.Clamp(oponente.x - agente.x, -1, 1) + 1; // Rango [0, 2]
            int deltaY = Mathf.Clamp(oponente.y - agente.y, -1, 1) + 1; // Rango [0, 2]
            int posicionRelativa = deltaX * 3 + deltaY; // Combinar en un rango [0, 8]

            // Identificar celda �nica del agente
            int celdaAgente = agente.x * mundo.WorldSize.x + agente.y;

            // Combinar la posici�n del agente con la posici�n relativa del oponente
            int estado = (celdaAgente * 9 + posicionRelativa) % numEstados;

            // A�adir mensaje de depuraci�n
            Debug.Log($"ObtenerEstado - Agente: ({agente.x}, {agente.y}), Enemigo: ({oponente.x}, {oponente.y}), Estado: {estado}");

            return estado;
        }




        private float sumaDeRetornos = 0f; // Acumula los retornos de todos los episodios
        private int episodiosTotales = 0;  // Contador de episodios totales

        private void ReiniciarEpisodio()
        {
            // Incrementar el contador de episodios
            episodiosTotales++;

            // C�lculo del promedio usando media ponderada exponencial
            ReturnAveraged = Mathf.Round((ReturnAveraged * 0.9f + Return * 0.1f) * 100) / 100;

            // Tambi�n puedes calcular el promedio acumulado (opcional)
            sumaDeRetornos += Return;
            float promedioGlobal = Mathf.Round((sumaDeRetornos / episodiosTotales) * 100) / 100;

            Debug.Log($"Episodio {CurrentEpisode} finalizado: Total Reward = {Return}, Averaged Reward = {ReturnAveraged}, Promedio Global = {promedioGlobal}");

            // Actualizar epsilon din�micamente para reducir exploraci�n con el tiempo
            parametros.epsilon = Mathf.Lerp(1f, 0.1f, (float)CurrentEpisode / parametros.episodes);
            Debug.Log($"Epsilon actualizado: {parametros.epsilon}");

            // Reiniciar variables del episodio
            recompensaTotal = 0f; // Reiniciar acumulador de recompensa
            CurrentStep = 0;
            CurrentEpisode++;

            // Actualizar posiciones iniciales del agente y el enemigo
            AgentPosition = mundo.RandomCell();
            OtherPosition = mundo.RandomCell();

            // Guardar la tabla Q peri�dicamente
            if (CurrentEpisode % parametros.episodesBetweenSaves == 0)
            {
                GuardarTablaQ();
            }

            // Disparar evento para iniciar un nuevo episodio
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
