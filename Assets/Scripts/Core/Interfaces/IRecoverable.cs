using System.Collections;

namespace Vastcore.Core
{
    /// <summary>
    /// エラー回復機能を備えたシステムのインターフェース
    /// </summary>
    /// <typeparam name="TParams">回復処理に必要なパラメータ型</typeparam>
    /// <typeparam name="TResult">回復処理の結果型</typeparam>
    public interface IRecoverable<TParams, TResult>
    {
        /// <summary>
        /// エラー回復処理の実行
        /// </summary>
        /// <param name="parameters">回復に必要なパラメータ</param>
        /// <param name="onSuccess">回復成功時のコールバック</param>
        /// <param name="onFailure">回復失敗時のコールバック</param>
        /// <returns>回復処理のコルーチン</returns>
        IEnumerator AttemptRecovery(TParams parameters,
            System.Action<TResult> onSuccess,
            System.Action onFailure);
    }
}
