using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class IOSource
    {
        public readonly UriLockerManager lockerManager = new UriLockerManager();
        public abstract AsyncReadOperation<T> Read<T>(string uri, CancellationToken token);
        public abstract AsyncWriteOperation<T> Write<T>(string uri, T value, CancellationToken token);
    }
}