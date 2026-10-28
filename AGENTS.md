# GameJam2026 项目规则

## 项目背景

- 游戏引擎：Unity 2022.3.62f3c1
- 编程语言：C#
- 项目类型：Game Jam 限时游戏开发
- 当前状态：Unity 项目已创建，已完成 Git 初始提交。
- 外层工作区与 Git 仓库根目录：`D:\cby\project\GameJam2026`
- 内层 Unity 项目根目录：`D:\cby\project\GameJam2026\GameJam2026`；`Assets/`、`Packages/`、`ProjectSettings/` 位于该目录中。
- 实际项目模板：Universal 2D / URP（`com.unity.template.universal-2d@2.1.3`），不是原计划的 2D Core；URP 包版本为 `14.0.12`。
- 当前输入系统：旧版 Input Manager（`activeInputHandler: 0`）；未安装新 Input System 包（`com.unity.inputsystem`）。
- 目标平台：Windows；用户已检查构建平台正常，尚未验证最终构建。
- Git 初始提交：`1d1f094`（`Initial commit: Unity GameJam project setup`）。
- 主要目标：在有限时间内制作出稳定、可游玩的游戏

## 当前检查状态与已知问题

- 用户已确认项目可正常打开，Console 未发现其他红色错误。
- Package Manager 在线搜索曾出现账号访问令牌失效错误，尚未确认已彻底修复。
- Windows 构建平台、项目打开及 Console 状态来自用户的检查反馈；本次文档更新未重新操作 Unity Editor 验证。
- 尚未实际验证 Unity 编译、Play Mode 和最终构建。项目可打开、平台检查正常或静态检查通过，都不等于这些验证已经通过。

## 开发规则

- 实现功能前，先检查相关文件，并说明计划如何修改。
- 优先采用简单、可运行的实现，避免不必要的抽象。
- 优先复用已有脚本和组件，避免重复创建相同系统。
- 引入不熟悉的代码时，解释重要的 C# 和 Unity 概念。
- 只有实际执行过测试，才能声称代码已经通过测试。
- 如果无法执行测试，明确说明尚未验证的内容。

## 安全规则

- 未经明确许可，不删除项目资源或现有脚本。
- 安装 Package 或插件前，先解释原因并征求许可。
- 未经明确许可，不修改 Project Settings 或更换 Unity 版本。
- 覆盖已有文件前，先解释修改内容并征求许可。
- 所有修改尽量限制在当前任务范围内。

## Game Jam 优先级

1. 保持项目可运行。
2. 优先实现核心玩法循环。
3. 添加可选功能前，先解决阻塞性 Bug。
4. 时间紧张时，优先选择简单可靠的实现。
5. 为测试和最终构建预留时间。
