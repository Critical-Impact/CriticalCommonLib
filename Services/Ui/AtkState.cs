namespace CriticalCommonLib.Services.Ui
{
    public interface AtkState<T> 
    {
        public bool HasState { get; set; }
        public bool NeedsStateRefresh { get; set; }
        public void UpdateState(T newState);
        public void Clear();
    }
}