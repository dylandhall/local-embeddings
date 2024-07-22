namespace LocalEmbeddings.Helpers;

public static class ConcurrentHelper
{
    public static async Task<(T1, T2)> GetValues<T1, T2>(Task<T1> task1, Task<T2> task2) =>
        (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false));
    
    public static async Task<(T1, T2, T3)> GetValues<T1, T2, T3>(Task<T1> task1, Task<T2> task2, Task<T3> task3) =>
        (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false), await task3.ConfigureAwait(false));
    
    public static async Task<(T1, T2, T3, T4)> GetValues<T1, T2, T3, T4>(Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4) =>
        (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false), await task3.ConfigureAwait(false), await task4.ConfigureAwait(false));
    
    public static async Task<(T1, T2, T3, T4, T5)> GetValues<T1, T2, T3, T4, T5>(Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5) =>
        (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false), await task3.ConfigureAwait(false), await task4.ConfigureAwait(false), await task5.ConfigureAwait(false));
    
    public static async Task<(T1, T2, T3, T4, T5, T6)> GetValues<T1, T2, T3, T4, T5, T6>(Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5, Task<T6> task6) =>
        (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false), await task3.ConfigureAwait(false), await task4.ConfigureAwait(false), await task5.ConfigureAwait(false), await task6.ConfigureAwait(false));
    
    public static async Task<(T1, T2, T3, T4, T5, T6, T7)> GetValues<T1, T2, T3, T4, T5, T6, T7>(Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5, Task<T6> task6, Task<T7> task7) =>
        (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false), await task3.ConfigureAwait(false), await task4.ConfigureAwait(false), await task5.ConfigureAwait(false), await task6.ConfigureAwait(false), await task7.ConfigureAwait(false));
    
    public static async Task<(T1, T2, T3, T4, T5, T6, T7, T8)> GetValues<T1, T2, T3, T4, T5, T6, T7, T8>(Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5, Task<T6> task6, Task<T7> task7, Task<T8> task8) =>
        (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false), await task3.ConfigureAwait(false), await task4.ConfigureAwait(false), await task5.ConfigureAwait(false), await task6.ConfigureAwait(false), await task7.ConfigureAwait(false), await task8.ConfigureAwait(false));
    public static async Task<(T1, T2, T3, T4, T5, T6, T7, T8, T9)> GetValues<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5, Task<T6> task6, Task<T7> task7, Task<T8> task8, Task<T9> task9) =>
        (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false), await task3.ConfigureAwait(false), await task4.ConfigureAwait(false), await task5.ConfigureAwait(false), await task6.ConfigureAwait(false), await task7.ConfigureAwait(false), await task8.ConfigureAwait(false), await task9.ConfigureAwait(false));
    
    public static async Task<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)> GetValues<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5, Task<T6> task6, Task<T7> task7, Task<T8> task8, Task<T9> task9, Task<T10> task10) =>
        (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false), await task3.ConfigureAwait(false), await task4.ConfigureAwait(false), await task5.ConfigureAwait(false), await task6.ConfigureAwait(false), await task7.ConfigureAwait(false), await task8.ConfigureAwait(false), await task9.ConfigureAwait(false), await task10.ConfigureAwait(false));

}