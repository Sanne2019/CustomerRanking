using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerRanking.Common.Extensions
{
    public sealed class ReaderLock : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;
        public ReaderLock(ReaderWriterLockSlim rwLock)
        {
            _lock = rwLock;
            _lock.EnterReadLock();
        }
        public void Dispose() => _lock.ExitReadLock();
    }

    public sealed class WriterLock : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;
        public WriterLock(ReaderWriterLockSlim rwLock)
        {
            _lock = rwLock;
            _lock.EnterWriteLock();
        }
        public void Dispose() => _lock.ExitWriteLock();
    }
}
