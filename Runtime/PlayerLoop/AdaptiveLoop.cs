using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Metamatic
{
    /// <summary>
    /// Experimental loop that adapts to the execution time of actions.
    /// @WIP - Not ready for production
    /// </summary>
    public class AdaptiveLoop : IDisposable
    {
        private class ActionBatch
        {
            public List<Func<bool>> Actions = new List<Func<bool>>();
            public float LastExecutionTime; // Время выполнения последнего запуска
        }

        private List<ActionBatch> _batches = new List<ActionBatch>();
        private readonly List<Func<bool>> _pendingAdditions = new List<Func<bool>>();
        private readonly object _lock = new object();
        private int _currentBatchIndex = 0;
        private readonly float _upperTimeLimit;
        private readonly float _lowerTimeLimit;
        private bool _isProcessing = false;
        private event Action OnDisposed;

        public AdaptiveLoop(float upperTimeLimit = 0.05f, float lowerTimeLimit = 0.01f)
        {
            _upperTimeLimit = upperTimeLimit;
            _lowerTimeLimit = lowerTimeLimit;

            // Инициализируем первый пакет
            _batches.Add(new ActionBatch());
        }

        public void Run()
        {
            _isProcessing = true;

            // Обрабатываем отложенные добавления
            if (_pendingAdditions.Count > 0)
            {
                lock (_lock)
                {
                    foreach (var action in _pendingAdditions)
                    {
                        AddActionToBatch(action);
                    }
                    _pendingAdditions.Clear();
                }
            }

            if (_batches.Count == 0)
            {
                _isProcessing = false;
                return;
            }

            var batch = _batches[_currentBatchIndex];

            var stopwatch = Stopwatch.StartNew();

            // Выполняем действия пакета
            for (int i = batch.Actions.Count - 1; i >= 0; i--)
            {
                var action = batch.Actions[i];
                if (action != null)
                {
                    bool removeAction = false;

                    try
                    {
                        removeAction = action();
                    }
                    catch (Exception e)
                    {
                        Console.Exception("AdaptiveLoop", $"Exception in Loop action: {e}");
                        removeAction = true; // Удаляем действие при исключении
                    }

                    if (removeAction)
                    {
                        batch.Actions.RemoveAt(i);
                    }
                }
            }

            stopwatch.Stop();
            batch.LastExecutionTime = (float)stopwatch.Elapsed.TotalSeconds;

            // Переходим к следующему пакету
            _currentBatchIndex = (_currentBatchIndex + 1) % _batches.Count;

            // Выполняем оптимизацию пакетов
            OptimizeBatches();

            _isProcessing = false;
        }

        private void OptimizeBatches()
        {
            // Проверяем, нужно ли разделить пакет
            var batch = _batches[_currentBatchIndex];

            if (batch.LastExecutionTime > _upperTimeLimit && batch.Actions.Count > 1)
            {
                // Разделяем пакет на два
                SplitBatch(batch);
            }
            else if (batch.LastExecutionTime < _lowerTimeLimit && _batches.Count > 1)
            {
                // Объединяем пакет с другим
                MergeBatch(batch);
            }
        }

        private void SplitBatch(ActionBatch batch)
        {
            int midIndex = batch.Actions.Count / 2;
            var newBatch = new ActionBatch();

            // Переносим половину действий в новый пакет
            newBatch.Actions.AddRange(batch.Actions.GetRange(midIndex, batch.Actions.Count - midIndex));
            batch.Actions.RemoveRange(midIndex, batch.Actions.Count - midIndex);

            _batches.Insert(_currentBatchIndex + 1, newBatch);
        }

        private void MergeBatch(ActionBatch batch)
        {
            // Находим пакет с наименьшим временем выполнения (исключая текущий)
            ActionBatch minBatch = null;
            float minExecutionTime = float.MaxValue;
            int minBatchIndex = -1;

            for (int i = 0; i < _batches.Count; i++)
            {
                var currentBatch = _batches[i];

                // Пропускаем текущий пакет
                if (currentBatch == batch)
                    continue;

                if (currentBatch.LastExecutionTime < minExecutionTime)
                {
                    minExecutionTime = currentBatch.LastExecutionTime;
                    minBatch = currentBatch;
                    minBatchIndex = i;
                }
            }

            if (minBatch != null)
            {
                // Объединяем действия
                batch.Actions.AddRange(minBatch.Actions);

                // Удаляем объединённый пакет
                _batches.RemoveAt(minBatchIndex);

                // Корректируем текущий индекс, если необходимо
                if (minBatchIndex < _currentBatchIndex)
                {
                    _currentBatchIndex--;
                }
            }
            else
            {
                // Если остался только один пакет (текущий), ничего не делаем
            }
        }

        public void AddAction(Func<bool> action)
        {
            if (_isProcessing)
            {
                _pendingAdditions.Add(action);
            }
            else
            {
                lock (_lock)
                {
                    AddActionToBatch(action);
                }
            }
        }

        private void AddActionToBatch(Func<bool> action)
        {
            // Находим пакет с наименьшим временем выполнения
            ActionBatch targetBatch = null;
            float minExecutionTime = float.MaxValue;

            foreach (var batch in _batches)
            {
                if (batch.LastExecutionTime < minExecutionTime)
                {
                    minExecutionTime = batch.LastExecutionTime;
                    targetBatch = batch;
                }
            }

            // Добавляем действие в найденный пакет
            targetBatch.Actions.Add(action);
        }

        public void Dispose()
        {
            OnDisposed?.Invoke();
            OnDisposed = null;
            _batches.Clear();
            _pendingAdditions.Clear();
        }
    }
}