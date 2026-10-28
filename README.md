# GameJam2026

Unity 2022.3.62f3c1 的 2D Game Jam 工程，使用 URP 和旧版 Input Manager，目标平台为 Windows。

## 打开工程

1. 在 Codex 中使用本仓库根目录作为项目目录。
2. 在 Unity Hub 中打开仓库内的 GameJam2026 文件夹。
3. 当前入口场景：GameJam2026/Assets/Scenes/SampleScene.unity。

## 当前可玩范围与操作

- 当前为工程模板，尚未实现核心玩法或约定游戏操作。
- 环境检查与验证状态见 ENV_CHECK.md。

## Windows 构建

1. 在 Unity 2022 中打开 File > Build Settings。
2. 确认目标为 PC, Mac & Linux Standalone / Windows，并检查 Scenes In Build 中的入口场景。
3. 将构建输出到 Unity 工程内的 Builds/Windows 文件夹；该目录已被 Git 忽略。
4. 构建结束后，从输出目录启动程序，检查画面及本版本实现的操作，验证关闭窗口或游戏退出功能，并将实际结果记录到 ENV_CHECK.md。

以上为操作步骤，不代表构建或启动已经验证通过。