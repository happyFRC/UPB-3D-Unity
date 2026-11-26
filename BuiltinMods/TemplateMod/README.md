# TemplateMod —— 行星球宇宙模组模板

这个文件夹现在作为**内置模组**随游戏发布，你在游戏的「模组管理」里就能看到它（带 `内置` 标签）。
想自己写模组，复制一份改个名字即可。

## 复制一份开始写

1. 把整个 `TemplateMod` 文件夹复制到任意位置，改名（例如 `MyMod`）
2. 主文件名改成 `MyMod.cs`（**必须与文件夹同名**），里面的类名也改成 `MyMod`
3. 改 `desc.json` 里的 name / author / version / description
4. 放到下面任一位置：

| 想做成 | 放哪 | 游戏里显示 |
|---|---|---|
| 玩家模组 | `<游戏目录>/Mods/MyMod/`（与 exe 同级，游戏启动会自动建这个文件夹） | `外部` |
| 内置模组（随游戏发布） | 工程根目录 `BuiltinMods/MyMod/`，打包时由 `BuildGame` 自动拷进 `StreamingAssets/Mods/` | `内置` |

## 目录结构

```
MyMod/
├─ MyMod.cs         ← 主文件：与文件夹同名，必须有实现 UPBMod 的类，游戏只调它的 Startup
├─ 其它.cs          ← 会一起编译，供主文件引用；这里即使有 UPBMod 实现也不会被调用
├─ icon.png         模组图标（没有就用名字首字代替）
└─ desc.json        描述：name / author / version / description
```

## 入口

```csharp
public class MyMod : UPBMod {
    public void Startup(UPBLogger logger) {
        logger.info("模组已加载");      // 输出 [显示名 INFO] ...
        // logger.warn / logger.err / logger.floatText(s) / logger.floatText(s, color)
    }
}
```

- **只有主文件 `MyMod.cs` 里定义的 `UPBMod` 实现会被实例化并调用 `Startup`**；
- 主文件缺失、或主文件里没有实现 `UPBMod` 的类 → 该模组被跳过，面板显示「已跳过」，日志里有原因；
- 其它文件里的 `UPBMod` 实现一律忽略，但它们的类型可以被主文件引用（一起编译成一个程序集）；
- 子文件夹里的 .cs 也会一起编译（`bin/`、`obj/` 除外）。

## 模组资源（贴图 / 文本 / 任意文件）

`Resource(path, id)` 指向"某个根目录下的某个文件"：

- `path`：相对路径，例如 `"Textures/Planet.png"`、`"Data/planets.json"`；
- `id`：**模组文件夹名**（相对 `Mods/` 或 `BuiltinMods/`）；省略或写 `"default"` 就是内置 `Resources`。

```csharp
SandboxRegister.RegisterPlanet(new PlanetData {
    name = "我的行星",
    mass = 1.0, radius = 1.0, temperature = 288.0,
    surface    = SandboxRegister.Resource("Textures/Planet.png", "MyMod"),
    atmosphere = SandboxRegister.Resource("Textures/Clouds.png", "MyMod"),
});

// 它只是个引用，本身不做读取
Resource icon = SandboxRegister.Resource("icon.png", "MyMod");
```

- `Resource` 只保存 `id` + `path`（类似 Minecraft 的 `ResourceLocation`），不负责读文件；
- 需要磁盘文件时用 `GetFile()`（返回 `FileInfo`，内置 Resources 资源没有实体文件，返回 `null`）；
- 贴图由游戏在需要时加载：模组里的 png/jpg 直接读磁盘，内置走 `Resources`，找不到会回退并打警告；
- 序列化成 `模组名:相对路径` 存进存档，读档时自动还原。
