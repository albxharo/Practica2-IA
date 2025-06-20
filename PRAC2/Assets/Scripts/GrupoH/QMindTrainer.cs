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
        int numEstados = 144; // Total de estados posibles

        public void Initialize(QMindTrainerParams qMindTrainerParams, WorldInfo worldInfo, INavigationAlgorithm navigationAlgorithm)
        {
            Debug.Log("QMindTrainer: initialized");
            Time.timeScale = 5f;
            // Inicializaci�n
            parametros = qMindTrainerParams;
            // Decrecimiento de epsilon
            float t = (float)CurrentEpisode / parametros.episodes;

            if (t < 0.8f)
            {
                parametros.epsilon = Mathf.Lerp(0.7f, 0.3f, t / 0.8f); // Episodios 0-80%: de 0.7 a 0.3
            }


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
            int estadoActual = ObtenerEstado(AgentPosition, OtherPosition, mundo);

            // Elegir la acci�n (considerando si est� en modo de entrenamiento o no)
            int accion = SeleccionarAccion(estadoActual, train);

            // Realizar la acci�n y obtener la nueva posici�n
            CellInfo nuevaPosicion = EjecutarAccion(accion);

            // Calcular el nuevo estado tras el movimiento
            int nuevoEstado = ObtenerEstado(nuevaPosicion, OtherPosition, mundo);

            // Logs detallados para depuraci�n
            Debug.Log($"DoStep - Estado actual: {estadoActual}, Acci�n elegida: {accion}, Nueva posici�n: ({nuevaPosicion.x}, {nuevaPosicion.y}), Nuevo estado: {nuevoEstado}");

            // Calcular recompensa y actualizar tabla Q si est� en modo entrenamiento
            if (train)
            {
                float recompensa = CalcularRecompensa(nuevaPosicion);
                recompensaTotal += recompensa; 
                ActualizarQ(estadoActual, accion, recompensa, nuevoEstado);
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

        private int ObtenerEstado(CellInfo agente, CellInfo enemigo, WorldInfo mundo)
        {
            // Posici�n relativa del enemigo
            int deltaX = Mathf.Clamp(enemigo.x - agente.x, -1, 1) + 1; // [0,2]
            int deltaY = Mathf.Clamp(enemigo.y - agente.y, -1, 1) + 1; // [0,2]
            int posicionRelativa = deltaX * 3 + deltaY; // [0,8]

            // Caminabilidad del agente (N,E,S,O)
            bool[] direcciones = new bool[4];
            direcciones[0] = agente.y + 1 < mundo.WorldSize.y && mundo[agente.x, agente.y + 1].Walkable; // Norte
            direcciones[1] = agente.x + 1 < mundo.WorldSize.x && mundo[agente.x + 1, agente.y].Walkable; // Este
            direcciones[2] = agente.y - 1 >= 0 && mundo[agente.x, agente.y - 1].Walkable; // Sur
            direcciones[3] = agente.x - 1 >= 0 && mundo[agente.x - 1, agente.y].Walkable; // Oeste

            // Codificar la caminabilidad como un n�mero binario
            int codAccesibilidad = 0;
            for (int i = 0; i < 4; i++)
                if (direcciones[i]) codAccesibilidad |= (1 << i); // Suma potencias de 2

            int estado = posicionRelativa * 16 + codAccesibilidad;

            Debug.Log($"ObtenerEstado - Relaci�n enemigo: {posicionRelativa}, Caminabilidad: {codAccesibilidad}, Estado: {estado}");
            return estado;
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
            const float RECOMPENSA_POR_SOBREVIVIR = 1f;
            const float PENALIZACION_POR_MUERTE = -100f;

            if (celda.Equals(OtherPosition))
            {
                Debug.Log("Agente atrapado. Penalizaci�n.");
                return PENALIZACION_POR_MUERTE;
            }

            return RECOMPENSA_POR_SOBREVIVIR;
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

            // Guardar la tabla Q peri�dicamente
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
                Debug.Log($"[CargarTablaQ] Total de entradas cargadas: {tablaQ.numEstados * tablaQ.numAcciones}");

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
