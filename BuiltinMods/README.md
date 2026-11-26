# BuiltinMods —— 内置模组源码

这里放随游戏一起发布的模组，结构和玩家模组完全一样：

```
BuiltinMods/
└─ 模组名/
   ├─ 模组名.cs        ← 主文件，必须与文件夹同名，实现 UPBMod
   ├─ desc.json
   └─ icon.png
```

- `TemplateMod/` 就是随游戏的示例模组，玩家也能在「模组管理」里看到它。
- 打包时 `BuildGame.CopyBuiltinMods` 会把整个目录拷到
  `<输出目录>/<产品名>_Data/StreamingAssets/Mods/`，运行时 `ModManager` 从那里加载。
- **不要**把模组源码放进 `Assets/StreamingAssets`：Unity 会把 `.cs` 当游戏脚本编译进 Assembly-CSharp。
- 编辑器里调试时，`ModManager` 直接读这个目录（`Application.dataPath/../BuiltinMods`）。
- 内置模组和玩家目录里的模组同名时，内置优先，玩家那份会被跳过。
