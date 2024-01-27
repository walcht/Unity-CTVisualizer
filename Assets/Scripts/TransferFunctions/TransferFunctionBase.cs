namespace UnityCTVisualizer
{
    public enum TransferFunction
    {
        TF1D,
    }

    public struct ControlPoint<T>
    {
        public float position;
        public T value;
    }
}
