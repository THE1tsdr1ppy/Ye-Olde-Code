using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGLMinecraftClone
{
    public class MainThreadInvoker
    {
        private readonly GameWindow _gameWindow;
        private readonly Queue<Action> _actions = new Queue<Action>();
        private readonly object _lock = new object();

        public MainThreadInvoker(GameWindow gameWindow)
        {
            _gameWindow = gameWindow;
        }

        public void Invoke(Action action)
        {
            lock (_lock)
            {
                _actions.Enqueue(action);
            }
        }

        public void Update()
        {
            lock (_lock)
            {
                while (_actions.Count > 0)
                {
                    _actions.Dequeue()?.Invoke();
                }
            }
        }
    }
    public static class Dispatcher
    {
        private static GameWindow mainWindow;
        private static List<Action> pendingActions = new List<Action>();
        private static object lockObj = new object();

        public static void Initialize(GameWindow window)
        {
            mainWindow = window;
        }

        public static void Invoke(Action action)
        {
            lock (lockObj)
            {
                pendingActions.Add(action);
            }
        }

        public static void Update()
        {
            lock (lockObj)
            {
                foreach (var action in pendingActions)
                {
                    action();
                }
                pendingActions.Clear();
            }
        }
    }















    public class ChunkData
    {
        public OpenTK.Mathematics.Vector3 Position { get; set; }
        public List<OpenTK.Mathematics.Vector3> Vertices { get; set; }
        public List<OpenTK.Mathematics.Vector2> UVs { get; set; }
        public List<uint> Indices { get; set; }
    }

    public class MeshData
    {
        public List<OpenTK.Mathematics.Vector3> Vertices { get; set; }
        public List<OpenTK.Mathematics.Vector2> UVs { get; set; }
        public List<uint> Indices { get; set; }
    }
}
