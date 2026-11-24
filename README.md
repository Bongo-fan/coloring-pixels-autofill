# Coloring Pixels Autofill

适用于 Windows Steam 版 Coloring Pixels 的 BepInEx 游戏内自动填色插件。它比较游戏当前关卡的目标颜色和实时进度，跳过正确的格子，调用游戏自己的选色和填色逻辑。支持给屏幕外的格子填色。

## 安装

1. 关闭游戏。在 Steam 里右击 Coloring Pixels，选择「管理 → 浏览本地文件」。
2. 从 [BepInEx 官方发布页](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6) 下载 `BepInEx_win_x86_5.4.23.6.zip`。将其内容解压到与 `ColoringPixels.exe` 相同的目录。
3. 下载本仓库 Release 的 `PixelAutofill.dll`，放入游戏目录下的 `BepInEx/plugins`。如果没有 plugins 子目录，可以创建。
4. 启动游戏，左上角会出现 Autofill 面板。可在 `BepInEx/LogOutput.log` 中检查加载结果。当前版本使用 Unity Mono 和 x86 加载器。

## 使用

打开未完成的图，在游戏内点击 **Start / resume**，或按 **F8** 开始与停止。**F7** 切换面板显示。默认按颜色分组，取消该选项就按行填。速度可用滑杆调节。再次开始时会以游戏的当前进度重新判断待填格子。

退出游戏后把 `PixelAutofill.dll` 移出 `BepInEx/plugins` 即可停用。

## 原理与构建

插件在 Unity 主线程比较 `ClickTest.mainGridValues` 与 `savedGridValues`，使用 `ChannelChange.SetColNumber` 选色。调用 `ClickTest.CastRay` 前临时把目标格映射到游戏内坐标，调用后恢复变换，再校验颜色。按帧分批执行，保存与完成事件沿用游戏流程。

在 Windows PowerShell 中运行下面的命令，路径换成 Steam 显示的实际安装位置：

```powershell
powershell -ExecutionPolicy Bypass -File .\Build.ps1 -GameDir 'E:\SteamLibrary\steamapps\common\Coloring Pixels'
```

输出 `PixelAutofill.dll`。构建从本机游戏和 BepInEx 读取引用，游戏更新后请重新验证兼容性。
