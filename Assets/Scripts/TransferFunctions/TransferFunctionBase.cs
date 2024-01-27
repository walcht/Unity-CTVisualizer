namespace UnityCTVisualizer
{
    public enum TransferFunction
    {
        TF1D
    }

    public struct ControlPoint<T>
    {
        public float position;
        public T value;

        public ControlPoint(float position, T value)
        {
            this.position = position;
            this.value = value;
        }
    }
}
