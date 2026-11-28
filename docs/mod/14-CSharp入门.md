# 14 · C# 入门（写给完全没学过编程的人）

这一篇不急着做模组，专门补"看懂代码"需要的最小知识。**不用全记住**，写的时候回来查就行。

## 1. 代码是什么

模组里的 `.cs` 文件就是**一行行给游戏看的指令**。游戏启动时按顺序读、按顺序执行。大部分指令长这样：

```csharp
变量 = 值;
```

或者：

```csharp
做一件事(参数);
```

每一行结尾通常有一个**英文分号** `;`，就像句子结尾的句号。

## 2. 注释：写给人看的内容

```csharp
// 这是单行注释，游戏会忽略
int a = 1;   // 也可以写在代码后面

/*
   这是多行注释
   中间的内容都会被忽略
*/
```

注释用来给自己写备忘，不影响运行。

## 3. 变量：给数据起个名字

```csharp
int count = 3;            // 整数
double mass = 1.5;        // 小数（双精度，游戏里大量使用）
float color = 0.5f;       // 小数（单精度，注意结尾的 f）
string name = "地球";      // 文字（字符串）
bool enabled = true;      // 真/假
```

规则：

- 等号左边是名字，右边是值；
- 名字自己起，建议用英文，比如 `mass`、`name`；
- 类型写在前面的（`int`、`double`…）表示这个变量只能放这种数据；
- `float` 后面必须加 `f`，比如 `0.5f`，否则会被当成 `double`。

### 类型对照表

| 类型 | 放什么 | 例子 |
|---|---|---|
| `int` | 整数 | `5`、`-1` |
| `double` | 小数（默认） | `1.5`、`288.0` |
| `float` | 小数（要加 f） | `0.5f` |
| `string` | 文字 | `"蓝星"` |
| `bool` | 真/假 | `true`、`false` |

## 4. 运算

```csharp
int a = 2 + 3;        // 5
int b = 10 - 4;       // 6
double c = 2.0 * 3.0; // 6
int d = 7 / 2;        // 3（整数除法，会丢掉小数）
double e = 7.0 / 2.0; // 3.5
int f = 7 % 2;        // 1（取余数）
```

温度换算的例子：

```csharp
double celsius = 15.0;
double kelvin = celsius + 273.15;   // 288.15
```

## 5. 文字（字符串）

```csharp
string name = "蓝星";
string text = "行星：" + name;          // 用 + 拼接
string text2 = $"行星：{name}";          // 插值写法，推荐
string number = "编号 " + 3;             // 数字会自动变成文字
```

- 字符串必须用**英文双引号** `"..."`；
- 中文引号 `“”` 会报错；
- `$"..."` 里面用 `{变量}` 插入变量的值。

## 6. 判断（if / else）

```csharp
double temperature = 700.0;

if (temperature > 1000.0) {
    // 条件成立时执行
} else if (temperature > 300.0) {
    // 上一个不成立、这个成立时执行
} else {
    // 都不成立时执行
}
```

比较符号：

| 符号 | 意思 |
|---|---|
| `>` `<` | 大于 / 小于 |
| `>=` `<=` | 大于等于 / 小于等于 |
| `==` | 等于（注意是两个等号） |
| `!=` | 不等于 |
| `&&` | 并且 |
| `\|\|` | 或者 |

> `=` 是赋值，`==` 才是"等于"。这是新手最容易错的点之一。

## 7. 循环

### for 循环：重复固定次数

```csharp
for (int i = 0; i < 3; i++) {
    // 会执行 3 次，i 分别是 0、1、2
}
```

读法：

- `int i = 0`：从 0 开始；
- `i < 3`：只要 i 小于 3 就继续；
- `i++`：每次结束把 i 加 1。

### foreach 循环：把一堆东西逐个拿出来

```csharp
string[] names = { "水星", "金星", "地球" };

foreach (string n in names) {
    // 会执行 3 次，n 依次是三个名字
}
```

## 8. 数组和 List

### 数组：长度固定的列表

```csharp
string[] names = { "A", "B", "C" };
int length = names.Length;     // 3
string first = names[0];       // "A"，下标从 0 开始
```

### List：可以增删的列表

```csharp
using System.Collections.Generic;   // 文件开头需要这一行

List<string> names = new List<string>();
names.Add("A");
names.Add("B");
int count = names.Count;            // 2
```

模组里常见用法：

```csharp
List<PlanetData> planets = new List<PlanetData>();
planets.Add(new PlanetData { name = "A", mass = 1.0, radius = 1.0 });
```

## 9. 方法（函数）

方法就是"一段可以重复使用的代码"。

```csharp
double ToCelsius(double kelvin) {
    return kelvin - 273.15;
}
```

- `double` 是返回值的类型（返回一个数）；
- `ToCelsius` 是名字；
- `(double kelvin)` 是参数，调用时要传进来；
- `return` 把结果交回去。

调用：

```csharp
double c = ToCelsius(288.0);   // 14.85
```

没有返回值的方法用 `void`：

```csharp
void SayHello(UPBModInstance instance, string name) {
    instance.logger.info("你好，" + name);
}
```

> 模组里统一用 `instance.logger.info(...)` 输出日志，而不是 `Debug.Log`，这样日志里会带模组名前缀，方便排查。

模组入口就是一个方法：

```csharp
public void Startup(UPBModInstance instance) {
    // 游戏会调用这个方法
}
```

## 10. 类、对象、字段

"类"可以理解成**一个数据模板**。`PlanetData` 就是一个类，它有很多字段（名字、质量、半径……）。

创建"实例"（对象）用 `new`：

```csharp
PlanetData p = new PlanetData();
p.name = "蓝星";
p.mass = 1.0;
p.radius = 1.0;
```

也可以一步写完，叫"对象初始化器"：

```csharp
PlanetData p = new PlanetData {
    name = "蓝星",
    mass = 1.0,
    radius = 1.0,
};
```

两种写法效果一样，模组里基本都用第二种。

> 注意：初始化器里每一项后面是**逗号**，最后一项后面可以留逗号（C# 允许）。

## 11. Lambda 表达式：把"一段代码"当参数

事件里经常看到这种写法：

```csharp
UPBEventBus.GetBlackbodyColorEvent = temperature => {
    return (0.4f, 1.0f, 0.4f);
};
```

`temperature => { ... }` 就是一个 lambda：

- `temperature` 是参数（温度）；
- `=>` 读作"变成"；
- `{ ... }` 是这段代码；
- 整体表示"给我一个温度，我返回一个颜色"。

单行可以简写：

```csharp
UPBEventBus.GetBlackbodyColorEvent = temperature => (0.4f, 1.0f, 0.4f);
```

## 12. 元组：一次返回几个值

```csharp
(float r, float g, float b) color = (0.4f, 1.0f, 0.4f);
float red = color.r;
```

模组里的颜色就是三个 0~1 的小数：红、绿、蓝。

## 13. `static` 是什么

带 `static` 的东西**不需要创建对象就能直接用**，比如：

```csharp
SandboxRegister.RegisterPlanet(...);   // 直接用类名.方法
UPBEventBus.GetBlackbodyColorEvent;    // 直接用
```

你现在不用理解原理，只要照着写：**看到 `类名.方法(...)` 就照着抄**。

## 14. null：空

`null` 表示"什么都没有"。

```csharp
UPBEventBus.GetBlackbodyColorEvent = null;   // 取消事件
```

游戏里很多时候会用"是不是 null"来判断"有没有设置过"。

## 15. 怎么读报错

编译失败时日志会写：

```
HelloWorld.cs(12,5): error CS1002: 应输入 ;
```

- `(12,5)`：第 12 行、第 5 个字符附近；
- `CS1002`：错误编号；
- 后面是中文说明。

常见错误：

| 编号 | 意思 | 常见原因 |
|---|---|---|
| CS1002 | 应输入 `;` | 行尾少分号 |
| CS1513 | 应输入 `}` | 大括号少了一个 |
| CS0103 | 名称不存在 | 拼写错、大小写不对 |
| CS0246 | 找不到类型 | 拼写错，或少了 `using` |
| CS0117 | 不包含某字段 | 字段名写错 |
| CS1061 | 不包含某方法 | 方法名写错 |
| CS1010 | 常量中有换行符 | 字符串引号没闭合 |

## 16. 代码风格建议

- 缩进用 4 个空格（编辑器会自动做）；
- 每行只写一件事；
- 大括号成对写，写一个 `{` 就顺手写 `}`；
- 名字用英文，比如 `mass`、`temperature`；
- 改完先保存，再重启游戏。

## 17. 小练习

把下面的代码放进一个模组（文件夹 `Exercise`，主文件 `Exercise.cs`），开启并重启，然后看日志。

1. 声明三个变量：一个整数、一个小数、一个字符串，并把它们打印出来：

```csharp
using UnityEngine;

public class Exercise : UPBMod {
    public void Startup(UPBModInstance instance) {
        int a = 3;
        double b = 1.5;
        string c = "测试";

        instance.logger.info(a + " " + b + " " + c);
    }
}
```

2. 把第 1 题 `Startup` 方法里的内容换成 `for` 循环，把 1 到 5 打印出来：

```csharp
for (int i = 1; i <= 5; i++) {
    instance.logger.info("i = " + i);
}
```

3. 写一个方法 `double DoubleIt(double x)`，返回 `x * 2`，并在 `Startup` 里调用它：

```csharp
public void Startup(UPBModInstance instance) {
    instance.logger.info("3 的两倍是 " + DoubleIt(3.0));
}

double DoubleIt(double x) {
    return x * 2;
}
```

下一篇：[更多实例](15-更多实例.md)，把学到的语法用到真实模组里。

此文档由AI生成仅供参考，如有问题请在QQ群内提问
