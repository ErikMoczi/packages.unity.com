using System;
using System.Collections;

namespace ResourceManagement
{
    /// <summary>
    /// Status values for IAsyncOperations
    /// </summary>
    public enum AsyncOperationStatus
    {
        None,
        Succeeded,
        Failed
    };

    /// <summary>
    /// Base interface of all async ops
    /// </summary>
    public interface IAsyncOperation : IEnumerator
    {
        /// <summary>
        /// returns the status of the operation
        /// </summary>
        /// <value><c>true</c> if is done; otherwise, <c>false</c>.</value>
        AsyncOperationStatus status { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:ResourceManagement.IAsyncOperation"/> is done.
        /// </summary>
        /// <value><c>true</c> if is done; otherwise, <c>false</c>.</value>
        bool isDone { get; }

        /// <summary>
        /// Gets the percent complete of this operation.
        /// </summary>
        /// <value>The percent complete.</value>
        float percentComplete { get; }

        /// <summary>
        /// Reset status and error
        /// </summary>
        void ResetStatus();

        /// <summary>
        /// Gets the context object related to this operation, usually set to the IResourceLocation.
        /// </summary>
        /// <value>The context object.</value>
        object context { get; }

        /// <summary>
        /// Occurs when completed.
        /// </summary>
        event Action<IAsyncOperation> completed;

        /// <summary>
        /// Gets the exception that caused this operation to change its status to Failure.
        /// </summary>
        /// <value>The exception.</value>
        Exception error { get; }

		/// <summary>
		/// Gets the result.
		/// </summary>
		/// <value>The result.</value>
		object result { get; }
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
        new T result { get; }

        /// <summary>
        /// Occurs when completed.
        /// </summary>
        new event Action<IAsyncOperation<T>> completed;
    }
}
