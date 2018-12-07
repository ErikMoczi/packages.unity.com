using System;
using System.Collections;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Status values for IAsyncOperations
    /// </summary>
    public enum AsyncOperationStatus
    {
        None,
        Succeeded,
        Failed
    }

    /// <summary>
    /// Base interface of all async ops
    /// </summary>
    public interface IAsyncOperation : IEnumerator
    {
        /// <summary>
        /// returns the status of the operation
        /// </summary>
        /// <value><c>true</c> if is done; otherwise, <c>false</c>.</value>
        AsyncOperationStatus Status { get; }
        /// <summary>
        /// internal integrity check
        /// </summary>
        /// <returns></returns>
        bool Validate();
        /// <summary>
        /// used by Validate to ensure operation is in correct state
        /// </summary>
        bool IsValid { get; set; }
        /// <summary>
        /// Release operation back to internal cache. This can be used to avoid garbage collection.
        /// </summary>
        void Release();
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:ResourceManagement.IAsyncOperation"/> is done.
        /// </summary>
        /// <value><c>true</c> if is done; otherwise, <c>false</c>.</value>
        bool IsDone { get; }

        /// <summary>
        /// Gets the percent complete of this operation.
        /// </summary>
        /// <value>The percent complete.</value>
        float PercentComplete { get; }

        /// <summary>
        /// Reset status and error
        /// </summary>
        void ResetStatus();

        /// <summary>
        /// Gets the context object related to this operation, usually set to the IResourceLocation.
        /// </summary>
        /// <value>The context object.</value>
        object Context { get; }

        /// <summary>
        /// Gets the key related to this operation, usually set to the address.
        /// </summary>
        /// <value>The context object.</value>
        object Key { get; set; }

        /// <summary>
        /// Occurs when completed.
        /// </summary>
        event Action<IAsyncOperation> Completed;
        /// <summary>
        /// Gets the exception that caused this operation to change its status to Failure.
        /// </summary>
        /// <value>The exception.</value>
        Exception OperationException { get; }

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <value>The result.</value>
        object Result { get; }
    }

    /// <summary>
    /// Templated version of IAsyncOperation, provides templated overrides where possible
    /// </summary>
    public interface IAsyncOperation<T> : IAsyncOperation
    {
        /// <summary>
        /// Gets the result as the templated type.
        /// </summary>
        /// <value>The result.</value>
        new T Result { get; }
        /// <summary>
        /// Internally marks operations to not automatically release back to the cache.
        /// </summary>
        /// <returns>Passes back this</returns>
        IAsyncOperation<T> Retain();
        /// <summary>
        /// Occurs when completed.
        /// </summary>
        new event Action<IAsyncOperation<T>> Completed;
    }
}
