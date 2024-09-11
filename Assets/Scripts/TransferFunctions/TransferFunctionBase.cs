using System;
using UnityEngine;

namespace UnityCTVisualizer {
    public delegate void VoidHandler();

    [Serializable]
    public class ControlPoint<P, T> {
        public VoidHandler OnValueChange;

        [SerializeField]
        P m_Position;
        public P Position {
            get => m_Position;
            set {
                m_Position = value;
                OnValueChange?.Invoke();
            }
        }

        [SerializeField]
        T m_Value;
        public T Value {
            get => m_Value;
            set {
                m_Value = value;
                OnValueChange?.Invoke();
            }
        }

        public ControlPoint(P position, T value) {
            Position = position;
            Value = value;
        }
    }

    public interface ITransferFunction {

        /// <summary>
        ///     Initializes the transfer function default state (e.g., control points data for 1D TFs, shapes for 2D TFs, etc.).
        /// </summary>
        /// 
        /// <remarks>
        ///     Should be called initially (i.e., think of this as a constructor that should be called before any subsequent
        ///     member calls/accesses)
        /// </remarks>
        void Init();

        /// <summary>
        ///     Similar to <seealso cref="TryUpdateColorLookupTexture">TryUpdateColorLookupTexture</seealso>.
        ///     Except that it ignores the dirty flag and forces the regeneration of the internal transfer
        ///     function. Use this function sparingly.
        /// </summary>
        void ForceUpdateColorLookupTexture();

        /// <summary>
        ///     Request an update for the internal transfer function. This checks a dirty flag then re-generates
        ///     the texture if necessary.
        /// </summary>
        /// 
        /// <remarks>
        ///     Intended workflow is to subscribe to TFColorsLookupTexChange event
        ///     to receive new color lookup textures and request textures updates by calling this function.
        /// </remarks>
        void TryUpdateColorLookupTexture();

        /// <summary>
        ///     Invoked whenever the underlying 1D transfer function texture has changed.
        /// </summary>
        /// 
        /// <remarks>
        ///     Intended workflow is to subscribe to this event to receive TF textures and to call
        ///     <seealso cref="TryUpdateColorLookupTexture">TryUpdateColorLookupTexture</seealso>
        ///     to request an update for the TF texture.
        /// </remarks>
        event Action<Texture2D> TFColorsLookupTexChange;
    }
}
