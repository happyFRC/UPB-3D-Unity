using System;

/// <summary>
/// 沙盘可选恒星的数据（只存"注册参数"）。
///
/// 半径、光度、密度、光谱型这些都由 <see cref="SandboxStarModel"/> 按 SSE 实时算，
/// 不在这里存；temp_review 是注册时带的一份表面温度，启动时会被 SSE 结果校正一次。
/// </summary>
[Serializable]
public class StarData {
    public string name;
    /// <summary>质量，太阳质量。</summary>
    public double mass;
    /// <summary>金属度 Z。</summary>
    public double metallicity;
    /// <summary>年龄，Myr。</summary>
    public double age;
    /// <summary>表面温度（K），注册数据里的缓存值。</summary>
    public double temp_review;
    /// <summary>自转周期（天）。</summary>
    public double rotationPeriodDays = 25.38;
}
