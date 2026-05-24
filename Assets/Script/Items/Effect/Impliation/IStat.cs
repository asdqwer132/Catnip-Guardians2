public interface IGameStat<T>
{
    T Clone();
    void Clamp();
}
public interface IBuffStat<T>
{
    void ApplyTo(T target);
}