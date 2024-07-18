using System;

public struct DisposableNullable<T> : IDisposable
where T: IDisposable
{
    private T m_Disposable;
    public DisposableNullable(T disposable)
    {
        m_Disposable = disposable;
    }
    public void Dispose()
    {
        if (m_Disposable == null) return;
        m_Disposable.Dispose();
    }
}