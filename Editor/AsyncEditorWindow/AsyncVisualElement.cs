using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace Metamatic.EditorTools
{
    public abstract class AsyncVisualElement: VisualElement
    {
        public AsyncVisualElement(CancellationSignal signal): base()
        {
            InternalInitializeAsync(signal);
        }
        
        private void InternalInitializeAsync(CancellationSignal cancellationSignal)
        {
            InitializeAsync(cancellationSignal).Forget();
        }
        
        protected abstract UniTaskVoid InitializeAsync(CancellationSignal signal);
    }
}