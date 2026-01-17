namespace AzRebit.Features.TimerTriggered.ClientInterface;


/// <summary>
/// Interface used by the client for timer triggered azure functions needed for enabling resubmit
/// </summary>
public interface ITimerResubmit
{
    /// <summary>
    /// Main processing method for Timer Triggered functions
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <param name="payload"></param>
    /// <typeparam name="TResult">Result of the task</typeparam>
    /// <returns></returns>
    /// <remarks>When this method is called from the <code>/resubmit</code> endpoint it will receive the same data structure that was saved with <code>IRebitStoreOperations.SavePayloadForResubmit</code>.
    /// When invoking <code>IRebitStoreOperations.SavePayloadForResubmit</code> be sure that the data structure is the same what is passed to <code>HandleTimerTrigger</code>.
    /// </remarks>
    Task<TResult> HandleTimerTriggerAsync<TInput, TResult>(TInput payload);

    /// <summary>
    /// Main processing method for Timer Triggered functions
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <param name="payload"></param>
    /// <returns></returns>
    /// <remarks>When this method is called from the <code>/resubmit</code> endpoint it will receive the same data structure that was saved with <code>IRebitStoreOperations.SavePayloadForResubmit</code>.
    /// When invoking <code>IRebitStoreOperations.SavePayloadForResubmit</code> be sure that the data structure is the same what is passed to <code>HandleTimerTrigger</code>.
    /// </remarks>
    Task HandleTimerTriggerAsync<TInput>(TInput payload);
}
