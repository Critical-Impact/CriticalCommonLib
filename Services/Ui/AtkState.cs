namespace CriticalCommonLib.Services.Ui
{
    public interface AtkState<T> 
    {
        public T State { get; set; }
        public void UpdateState(T newState);
    }
}