using System;

namespace Sage.SalesLogix.Migration
{
    public interface IOperationStatus
    {
        void Reset(int steps);
        IDisposable BeginStep(string message, int parts);
        bool Advance();
        bool IsCancelled { get; }
    }
}