public interface IGameStat<T>
{
    T Clone();
    void Clamp();
}