using System;
using System.Linq;
using Object = UnityEngine.Object;

namespace Metamatic
{
    public abstract class VirtualPlug: IPlug
    {
        public IPin[] outputPins;
        public IPin[] inputPins;
        public Type[] inputTypes => GetInputTypes();
        public Type[] outputTypes => GetOutputTypes();
        public Type[] inputPinTypes => GetInputPinTypes();
        public Type[] outputPinTypes => GetOutputPinTypes();
        protected abstract Type[] GetInputTypes();
        protected abstract Type[] GetOutputTypes();
        protected abstract Type[] GetInputPinTypes();
        protected abstract Type[] GetOutputPinTypes();
        public abstract SocketScheme socketScheme { get; }
    }
    
    public class VirtualPlug<TScheme>: VirtualPlug, IPlug<TScheme>, IDisposable
    where TScheme: SocketScheme, new()
    {
        private Socket<TScheme> m_Socket;
        private TScheme m_Scheme;
        public TScheme scheme => m_Scheme ??= new TScheme();
        public override SocketScheme socketScheme => scheme;
        
        public VirtualPlug(Object relatedObject)
        {
            scheme.debug_SetRelatedObject(relatedObject);
            /*
             * Initialize the scheme with the input and output pins, but switch the input and output pins because plug has reversed input and output pins.
             */
            scheme.Initialize(relatedObject);
            inputPins = scheme.outputPins;
            outputPins = scheme.inputPins;
        }
        
        /// <summary>
        /// Create pin reader for specific pin type and index
        /// </summary>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public VirtualPinReader<T> CreatePinReader<T>(int index = 0)
        {
            if (inputPins[index] is not IPin<T> pin)
            {
                throw new InvalidOperationException($"Pin at index {index} is not of type {typeof(T)}");
            }
            return new VirtualPinReader<T>(scheme.debug_GetRelatedObject(), pin);
        }
        
        public VirtualPinWriter<T> CreatePinWriter<T>(int index = 0)
        {
            if (outputPins[index] is not IPin<T> pin)
            {
                throw new InvalidOperationException($"Pin at index {index} is not of type {typeof(T)}");
            }
            return new VirtualPinWriter<T>(scheme.debug_GetRelatedObject(), pin);
        }
        
        protected override Type[] GetInputTypes()
        {
            return scheme.outputTypes;
        }

        protected override Type[] GetOutputTypes()
        {
            return scheme.inputTypes;
        }
        protected override Type[] GetInputPinTypes()
        {
            return scheme.outputPinTypes;
        }
        protected override Type[] GetOutputPinTypes()
        {
            return scheme.inputPinTypes;
        }

        public void Dispose()
        {
            scheme.Dispose();
        }
        
        
    }
}