namespace Vastcore.Utilities
{
    /// <summary>
    /// ログ出力機能を備えたコンポーネントのインターフェース
    /// 統一されたログ管理を可能にする
    /// </summary>
    public interface ILoggable
    {
        /// <summary>
        /// ログ出力時のカテゴリ名
        /// </summary>
        string LogCategory { get; }

        /// <summary>
        /// 最小ログレベル
        /// このレベル以上のログのみを出力
        /// </summary>
        VastcoreLogger.LogLevel MinimumLogLevel { get; set; }
    }
}
