using System.Collections;
using UnityEngine;

namespace VastCore.Testing
{
    /// <summary>
    /// テスト結果クラス
    /// </summary>
    public class TestResult
    {
        public string TestName { get; set; }
        public bool Passed { get; set; }
        public string Message { get; set; }
        public float Duration { get; set; }
    }

    /// <summary>
    /// テストスイートの基底クラス
    /// </summary>
    public abstract class BaseTestSuite : MonoBehaviour
    {
        protected System.Collections.Generic.List<TestResult> testResults;
        protected System.Text.StringBuilder testLog;
        protected System.DateTime testStartTime;

        protected virtual void InitializeTestSuite()
        {
            testResults = new System.Collections.Generic.List<TestResult>();
            testLog = new System.Text.StringBuilder();
            LogMessage($"{GetType().Name} initialized");
        }

        protected void LogMessage(string message)
        {
            string timestampedMessage = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            testLog.AppendLine(timestampedMessage);
            Debug.Log(timestampedMessage);
        }

        protected TestResult CreateTestResult(string testName, bool passed, string message = "", float duration = 0f)
        {
            return new TestResult
            {
                TestName = testName,
                Passed = passed,
                Message = message,
                Duration = duration
            };
        }

        protected void AddTestResult(TestResult result)
        {
            testResults.Add(result);
        }

        public abstract IEnumerator RunTests();
    }
}
