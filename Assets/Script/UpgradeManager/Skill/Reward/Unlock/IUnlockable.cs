public interface IUnlockable
{
    bool RequireUnlock { get; }
    DataType UnlockType { get; }
    string UnlockId { get; }
}