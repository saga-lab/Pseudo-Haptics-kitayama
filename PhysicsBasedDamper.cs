using UnityEngine;

public class PhysicsBasedDamper : MonoBehaviour
{
    // 実際の手の位置を追従しているオブジェクト
    [Tooltip("現実の手の位置を追従している親のAnchorオブジェクト")]
    public Transform realHandAnchor;

    [Header("物理ベースのダンパパラメータ")]
    [Tooltip("抵抗の強さを決定するスケーリング係数 (rho*Cd*Aに相当)。値を大きくすると抵抗が強くなる。")]
    [Range(0.01f, 5.0f)]
    public float dragScale = 1.0f;

    [Tooltip("抵抗による追従遅延の最小追従係数。この値を下回ると追従速度が制限される (極端なズレ防止)。")]
    [Range(0.001f, 0.5f)]
    public float minFollowFactor = 0.05f;

    // 仮想の手の目標位置と回転を格納する変数
    private Vector3 virtualPosition;
    private Quaternion virtualRotation;

    void Start()
    {
        if (realHandAnchor == null)
        {
            Debug.LogError("Real Hand Anchorが設定されていません。Inspectorで設定してください。");
            enabled = false;
            return;
        }

        // 初期位置を実手の位置に設定
        virtualPosition = realHandAnchor.position;
        virtualRotation = realHandAnchor.rotation;
    }

    void LateUpdate()
    {
        if (realHandAnchor == null) return;

        // --- 位置の物理ベース減衰計算 ---

        // 1. 実際の手と仮想の手の間のズレ (変位誤差) を取得
        Vector3 displacementError = realHandAnchor.position - virtualPosition;

        float deltaTime = Time.deltaTime;
        if (deltaTime == 0) return;

        // 2. 仮想の手が追いつくために必要な相対速度 (m/s) を計算
        // v = |変位誤差| / Δt
        float relativeSpeed = displacementError.magnitude / deltaTime;

        // 3. 速度の二乗に比例する抵抗係数 (R_drag) を計算
        // R_drag = K * v^2。KがdragScaleに相当します。
        float resistanceFactor = dragScale * relativeSpeed * relativeSpeed;

        // 4. 動的なLerp係数 (alpha_dyn) の計算
        // 抵抗が強いほど、補間係数を小さく (追従を遅く) する
        // 計算式: 1 / (1 + R_drag)
        // R_dragが小さい(低速) -> 係数1に近く(即追従)
        // R_dragが大きい(高速) -> 係数0に近く(抵抗大)
        float dynamicLerpFactor = 1.0f / (1.0f + resistanceFactor);

        // 最小追従係数でクランプ: 極端に速い動きで係数が0になりすぎないように保護
        dynamicLerpFactor = Mathf.Max(dynamicLerpFactor, minFollowFactor);

        // 5. Lerpによる位置更新と適用
        virtualPosition = Vector3.Lerp(virtualPosition, realHandAnchor.position, dynamicLerpFactor);
        transform.position = virtualPosition;

        // --- 回転の更新 ---
        // 回転は物理抵抗の対象にしなくても自然なため、ここでは従来の線形補間を適用
        virtualRotation = Quaternion.Slerp(virtualRotation, realHandAnchor.rotation, 0.1f);
        transform.rotation = virtualRotation;
    }
}