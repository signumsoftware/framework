using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Utilities
{
    public class ProgressProxy
    {
        const int numUpdates = 10000;

        private string currentTask;

        private int min;
        private int max;
        private int position;

        public event EventHandler<ProgressArgs> Changed;

        public ProgressProxy()
        {
        }


        public int Min
        {
            get { return min; }
        }

        public int Max
        {
            get { return max; }
        }

        public int Position
        {
            get { return position; }
            set
            {
                if (min <= value && value <= max)
                {
                    position = value;
                        OnChanged(ProgressAction.Position);
                }
            }
        }

        public string CurrentTask
        {
            get { return currentTask; }
        }

        public void Start(int max)
        {
            this.min = 0;
            this.max = max;
            this.position = 0;
            OnChanged(ProgressAction.Interval);
        }

        public void Start(string currentTask)
        {
            this.currentTask = currentTask;
            this.position = -1;
            OnChanged(ProgressAction.Interval | ProgressAction.Task);
        }

        public void Start(int max, string currentTask)
        {
            Start(0, max, currentTask);
        }

        public void Start(int min, int max, string currentTask, int? position = null)
        {
            if (min < 0 || max < 0)
                throw new ArgumentException("Min and Max should be greater than 0");

            if (max < min)
                throw new ArgumentException("Max should be greater or equal than min");

            if (position.HasValue && !(min <= position && position <= max))
                throw new ArgumentException("position should be between min and max");

            this.currentTask = currentTask;
            this.min = min;
            this.position = position ?? min;
            this.max = max;

            OnChanged(ProgressAction.Interval | ProgressAction.Task);
        }

        public void NextTask(int position, string currentTask)
        {
            this.position = position;
            this.currentTask = currentTask;
            OnChanged(ProgressAction.Task | ProgressAction.Position);
        }

        public void NextTask(string currentTask)
        {
            this.position++;
            this.currentTask = currentTask;
            OnChanged(ProgressAction.Task | ProgressAction.Position);
        }

        public void Reset()
        {
            currentTask = null;
            position = min;
            OnChanged(ProgressAction.Task | ProgressAction.Position);
        }


        void OnChanged(ProgressAction pa)
        {
            Changed?.Invoke(this, new ProgressArgs(pa));
        }

        static int RoundToPowerOfTwoMinusOne(int n)
        {
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;
            return n;
        }

      
    }

    public enum ProgressAction
    {
        Interval = 1,
        Position = 2,
        Task = 4,
    }

    public class ProgressArgs : EventArgs
    {
        public readonly ProgressAction Action;
        public ProgressArgs(ProgressAction a)
        {
            Action = a;
        }
    }
}
