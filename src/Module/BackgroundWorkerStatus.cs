using System;
using System.ComponentModel;
using Sage.Platform.Exceptions;

namespace Sage.SalesLogix.Migration.Module
{
    public sealed class BackgroundWorkerStatus : IOperationStatus
    {
        private readonly BackgroundWorker _worker;
        private readonly IExtendedLog _log;
        private int _steps;
        private int _currentStep;
        private int _parts;
        private int _currentPart;

        public BackgroundWorkerStatus(BackgroundWorker worker, IExtendedLog log)
        {
            Guard.ArgumentNotNull(worker, "worker");

            _worker = worker;
            _log = log;
        }

        #region IOperationStatus Members

        public void Reset(int steps)
        {
            _steps = steps;
            _currentStep = 0;
            _parts = 0;
            _currentPart = 0;
        }

        public IDisposable BeginStep(string message, int parts)
        {
            _parts = parts;
            ReportProgress(message);

            if (_log != null)
            {
                _log.Info(message);
            }

            return new StepScope(this);
        }

        public bool Advance()
        {
            if (IsCancelled)
            {
                return false;
            }
            else
            {
                _currentPart++;
                ReportProgress(null);
                return !IsCancelled;
            }
        }

        public bool IsCancelled
        {
            get { return _worker.CancellationPending; }
        }

        #endregion

        private void ReportProgress(string state)
        {
            if (_steps > 0 && _parts > 0)
            {
                int progress = (100*((_currentStep*_parts) + _currentPart))/(_steps*_parts);
                _worker.ReportProgress(progress, state);
            }
        }

        private void CompleteStep()
        {
            _currentStep++;
            _currentPart = 0;
            ReportProgress(null);
        }

        private sealed class StepScope : IDisposable
        {
            private readonly BackgroundWorkerStatus _status;

            public StepScope(BackgroundWorkerStatus status)
            {
                _status = status;
            }

            #region IDisposable Members

            public void Dispose()
            {
                _status.CompleteStep();
            }

            #endregion
        }
    }
}