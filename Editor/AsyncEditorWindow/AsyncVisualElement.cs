using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace Metamatic.EditorTools
{
    public abstract class AsyncVisualElement: VisualElement
    {
        public AsyncVisualElement(CancellationToken cancellationToken): base()
        {
            InternalInitializeAsync(cancellationToken);
        }
        
        private void InternalInitializeAsync(CancellationToken cancellationToken)
        {
            InitializeAsync(cancellationToken).Forget();
        }
        
        protected abstract UniTaskVoid InitializeAsync(CancellationToken cancellationToken);
    }
}