using System;
using Dalamud.Hooking;

namespace CriticalCommonLib.Helpers
{

    public interface IHookWrapper : IDisposable
    {
        public void Enable();
        public void Disable();

        public bool IsEnabled { get; }
        public bool IsDisposed { get; }

    }

    public class HookWrapper<T> : IHookWrapper where T : Delegate
    {

        private Hook<T> wrappedHook;


        public HookWrapper(Hook<T> hook)
        {
            this.wrappedHook = hook;
        }

        public void Enable()
        {
            if (_disposed) return;
            wrappedHook?.Enable();
        }

        public void Disable()
        {
            if (_disposed) return;
            wrappedHook?.Disable();
        }

        
        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if(!_disposed && disposing)
            {
                Disable();
                wrappedHook?.Dispose();
            }
            _disposed = true;         
        }

        public T Original => wrappedHook.Original;
        public bool IsEnabled => wrappedHook.IsEnabled;
        public bool IsDisposed => wrappedHook.IsDisposed;
    }
}