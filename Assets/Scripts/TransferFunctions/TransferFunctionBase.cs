using System;
using UnityEngine;

namespace UnityCTVisualizer
{
    public delegate void VoidHandler();

    [Serializable]
    public class ControlPoint<P, T>
    {
        public VoidHandler OnValueChange;

        [SerializeField]
        P m_Position;
        public P Position
        {
            get => m_Position;
            set
            {
                m_Position = value;
                OnValueChange?.Invoke();
            }
        }

        [SerializeField]
        T m_Value;
        public T Value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                OnValueChange?.Invoke();
            }
        }

        public ControlPoint(P position, T value)
        {
            this.Position = position;
            this.Value = value;
        }
    }

    public interface ITransferFunction
    {
        void Init();
        void ForceUpdateColorLookupTexture();
        void TryUpdateColorLookupTexture();
        event Action<Texture2D> TransferFunctionTexChange;
    }
}
