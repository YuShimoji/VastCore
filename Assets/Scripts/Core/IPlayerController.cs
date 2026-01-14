using UnityEngine;

namespace Vastcore.Core
{
    /// <summary>
    /// プレイヤーコントローラーのインターフェース
    /// Terrain システムがプレイヤーにアクセスするための抽象化
    /// </summary>
    public interface IPlayerController
    {
        /// <summary>
        /// プレイヤーのTransform
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// プレイヤーの位置
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// プレイヤーの前方向
        /// </summary>
        Vector3 Forward { get; }

        /// <summary>
        /// プレイヤーのカメラ
        /// </summary>
        Camera PlayerCamera { get; }

        /// <summary>
        /// 移動速度
        /// </summary>
        float MoveSpeed { get; }

        /// <summary>
        /// ジャンプ可能かどうか
        /// </summary>
        bool CanJump { get; }

        /// <summary>
        /// 地面に接地しているかどうか
        /// </summary>
        bool IsGrounded { get; }

        /// <summary>
        /// プレイヤーが有効かどうか
        /// </summary>
        bool IsActive { get; }
    }
}
