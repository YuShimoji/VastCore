using UnityEngine;
using System.Collections;

namespace Vastcore.Testing
{
    /// <summary>
    /// テストケースのインターフェース
    /// </summary>
    public interface ITestCase
    {
        /// <summary>
        /// テストケースを実行
        /// </summary>
        /// <param name="testManager">テストマネージャーの参照</param>
        /// <returns>テスト実行のコルーチン</returns>
        IEnumerator Execute(VastcoreIntegrationTestManager testManager);
    }
}