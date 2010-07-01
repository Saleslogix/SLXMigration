using System;

namespace Sage.SalesLogix.Migration.Tests
{
    public sealed class EmptyOperationStatus : IOperationStatus
    {
        #region IOperationStatus Members

        public void Reset(int steps) {}

        public IDisposable BeginStep(string message, int parts)
        {
            return new EmptyStepScope();
        }

        public bool Advance()
        {
            return true;
        }

        public bool IsCancelled
        {
            get { return false; }
        }

        #endregion

        private sealed class EmptyStepScope : IDisposable
        {
            #region IDisposable Members

            public void Dispose() {}

            #endregion
        }
    }
}