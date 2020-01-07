using System.Collections.Generic;

namespace BulletHell
{
    public class Pool<T> where T : new()
    {
        public struct Node
        {
            internal int NodeIndex;
            public T Item;
            public bool Active;
        }

        public Node[] Nodes;
        private Queue<int> Available;

        public int ActiveNodes
        {
            get
            {
                return Nodes.Length - AvailableCount;
            }
        }

        public int AvailableCount
        {
            get { return Available.Count; }
        }

        public Pool(int capacity)
        {
            Nodes = new Node[capacity];
            Available = new Queue<int>(capacity);

            for (int i = 0; i < capacity; i++)
            {
                Nodes[i] = new Node();

                Nodes[i].Active = false;
                Nodes[i].NodeIndex = i;
                Nodes[i].Item = new T();

                Available.Enqueue(i);
            }
        }

        public void Clear()
        {
            Available.Clear();
            for (int i = 0; i < Nodes.Length; i++)
            {
                Nodes[i].Active = false;
                Available.Enqueue(i);
            }
        }

        public Node Get()
        {
            int index = Available.Dequeue();
            Nodes[index].Active = true;

            return Nodes[index];
        }

        public void Return(int index)
        {
            if (Nodes[index].Active)
            {
                Nodes[index].Active = false;
                Available.Enqueue(Nodes[index].NodeIndex);
            }
        }

    }

}