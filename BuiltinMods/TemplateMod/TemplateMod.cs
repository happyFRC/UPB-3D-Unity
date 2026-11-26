using UnityEngine;

/// <summary>
/// 模组入口示例。
///
/// 游戏启动时会：
///   1. 编译本文件夹下所有 .cs（子文件夹不参与）；
///   2. 只在这个与文件夹同名的 TemplateMod.cs 里找实现 UPBMod 的类，
///      创建它的实例并调用 Startup(logger)。
///
/// 复制整个 TemplateMod 文件夹、改名（文件夹名、主文件名、类名保持一致），
/// 就可以开始写自己的模组。其它 .cs 会一起编译，但不能作为入口。
/// </summary>
public class TemplateMod : UPBMod
{
    public void Startup(UPBLogger logger)
    {
        logger.info("模组已加载，这里可以初始化你自己的内容。");
    }
}
