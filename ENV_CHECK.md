# 环境与验证状态

记录整理日期：2026-11-01。
技术约束和目录说明见 AGENTS.md；下表只记录检查状态与证据。

| 检查项 | 当前结果 | 日期与依据 |
| --- | --- | --- |
| Unity 版本、URP、输入配置 | Unity 2022.3.62f3c1 / URP 14.0.12；按用户要求切换为 Input System 1.14.2，activeInputHandler: 1 | 2026-10-30，读取实际配置，并由指定版本 Unity 执行后台导入 |
| Input System 包导入 | 已完成；项目 PackageCache 中解析到 1.14.2，输入模块初始化成功 | 2026-10-30，本机已有完整包缓存；本次 Unity 导入日志 |
| Unity 交互界面打开 | 用户曾反馈正常 | 反馈日期未记录，本次仅后台导入，未通过交互界面复核 |
| Windows 构建平台 | 用户曾反馈设置正常 | 反馈日期未记录，不代表构建成功 |
| Console 界面 | 用户曾反馈无其他红色错误 | 反馈日期未记录，该历史反馈不作本次验收依据；本次编译结果见下方 |
| Package Manager 在线搜索 | 曾遇到账号访问令牌失效，是否恢复待确认 | 历史反馈，本次未复测 |
| Unity 编译（白盒阶段历史） | 当时后台导入和脚本编译成功，进程退出码 0；Assembly-CSharp.dll 已生成 | 2026-10-30，GameJam2026/Logs/whitebox-import.log 记录 Tundra build success 与 Exiting batchmode successfully now；不代替新改动验证 |
| 能力数据层静态检查 | 六个数据脚本、一个 asmdef、四个配置资源及对应元数据齐全；GUID 无重复、资源引用可解析；两项独立限制为 12 / 4，仓库四类各四位且保留 14 个空位 | 2026-10-31，读取新增文件并执行静态检查；本次只读范围复核确认未修改已有场景、角色、Packages 或 ProjectSettings |
| 能力数据层 Unity 导入与编译 | 指定 Unity 版本后台导入及编译成功，生成 GameJam2026.Abilities.dll；日志记录正常退出，return code 0 | 2026-10-31，GameJam2026/Logs/ability-data-import.log；首次沙箱内启动停滞且许可组件报错，结束本次进程后在沙箱外重试成功。此结果不代表 Play Mode 或构建通过 |
| 能力配置 Inspector 验收 | 尚未通过交互界面核对字段或执行 Validate Configuration 菜单 | 2026-10-31，本次只执行静态检查和 Unity 后台导入、编译；操作步骤见 README.md |
| 能力装备核心编译与 EditMode 测试 | GameJam2026.Abilities 与 Editor-only 测试程序集编译成功；EditMode 结果 29/29 通过 | 2026-10-31，指定 Unity 2022.3.62f3c1 与 Test Framework 1.1.33；GameJam2026/Logs/ability-core-editmode-rerun.log 记录 Tundra build success，ability-core-editmode-rerun-results.xml 记录 Passed / 29。Unity 保存测试结果后卡在退出清理，主代理确认结果已写盘后结束了本次后台进程；未取得正常进程退出码。初次试跑的测试字段覆写有误，修正为克隆资源加 SerializedObject 后复测通过；真实配置资源哈希未变 |
| 能力装备核心场景接线与 Play Mode（第二阶段历史） | 当时尚未接入场景，未做 Play Mode 或人工操作 | 2026-10-31，当时只实现并测试纯 C# 管理器；当前接线和回归结果见联动阶段 |
| 能力装备 UI Unity 编译与场景生成 | 新增 UI/Editor 程序集导入成功；测试场景及槽位 Prefab 已生成，引用在序列化场景中可读 | 2026-10-31，Unity 2022.3.62f3c1 批处理日志 `GameJam2026/Logs/ability-ui-scene-create.log` 记录 Tundra build success、`ABILITY_UI_SCENE_CREATED` 和正常 batchmode 退出；场景结构与 MonoBehaviour 资源引用静态复核。没有进入 GUI 检查画面 |
| 能力装备 UI Play Mode 集成测试 | 7/7 通过：16 槽/容量显示、能力详情、空槽/越界、按钮回调、EventSystem/重复启停、字体释放和参考画布边界 | 2026-10-31，Unity Test Framework 1.1.33；最终结果 `GameJam2026/Logs/ability-ui-integration-final-results.xml` 为 Passed/7，日志记录 7 次进入/退出 Play Mode、结果保存和正常 batchmode 清理。第一次按 PlayMode 平台筛选时发现 0 个用例，后修正入口后执行的结果才计入此行。`-nographics` 不验证字形绘制、屏幕截图或鼠标射线操作 |
| 能力装备 UI 交互式画面验收 | 用户反馈上一阶段测试通过 | 2026-11-01，用户反馈；具体操作和分辨率未逐项记录，不作本次工具执行的验收结果 |
| 能力装备输入接线与静态检查（第四阶段历史） | AbilityCanvas 已绑定 AbilityInput 与 AbilityUI；当时仅发送请求 | 2026-11-01，按用户要求将原方向键导航改为 W/A/S/D；当前实际装备规则由新增联动组件执行 |
| 能力装备输入编译与集成回归（改键前） | 44/44 通过：装备核心 29、UI 集成 7、输入集成 8；Unity 正常退出，进程退出码 0 | 2026-11-01，Unity 2022.3.62f3c1 / Test Framework 1.1.33；`GameJam2026/Logs/ability-input-regression-final-results.xml` 与 `ability-input-regression-final.log`。UI/输入用例从 EditMode 入口显式进入/退出 Play Mode；虚拟键盘覆盖边界、空槽、长按、修饰键、重复启停与鼠标/键盘共用选择。本结果适用于改键前版本 |
| WASD 改键验证 | 用户反馈测试通过；第五阶段后台输入测试 8/8 也已通过 | 2026-11-01，用户反馈未逐项记录操作；自动结果来自 `ability-equipment-integration-results.xml`，覆盖 WASD 导航、原方向键无响应及其余输入规则 |
| 输入首次按键测试问题与修复 | 首轮 43/44，首个方向键未触发；诊断确认无焦点 Editor 更新未派发游戏动作。仅修正测试环境后，首键用例 1/1、完整回归 44/44 | 2026-11-01，首轮 `ability-input-regression-results.xml`、诊断 `ability-input-diagnostic-results.xml`、聚焦复测 `ability-input-focus-fix-results.xml`。测试使用 HideAndDontSave 的 InputSettings 克隆并恢复原引用，没有保存或修改项目输入配置 |
| 输入阶段无关文件核对 | Test Runner 的 `runInBackground: 0 → 1` 副作用已恢复；22 份无关文件 SHA256 与阶段开始前一致，EditorBuildSettings 无差异 | 2026-11-01，对照 `GameJam2026/Logs/ability-input-preflight-hashes.json`，包括 ProjectSettings、Packages、SampleScene、角色脚本、能力配置/核心与槽位 Prefab |
| 能力装备输入真实键盘验收 | 用户反馈输入阶段测试通过 | 2026-11-01，用户在 WASD 改键后反馈通过；此反馈不代替第五阶段实际装备联动的人工验收 |
| 能力装备联动场景接线与静态检查 | AbilityCanvas 新增 AbilityEquipmentController，绑定已有配置、UI 和 Input；UI Operation Feedback 绑定现有 Hint；60 个资源 GUID 唯一、场景 114 个对象本地引用完整、8 个程序集依赖无环 | 2026-11-01，窄范围修改现有场景，没有重新运行生成器或改 Prefab；字段和 asmdef 静态核对，生成器已同步 |
| 能力装备联动编译与完整回归（移除追加前） | 51/51 通过：管理器 29、UI 7、输入 8、联动 7；Unity 正常退出，进程退出码 0 | 2026-11-01，Unity 2022.3.62f3c1 / Test Framework 1.1.33；`GameJam2026/Logs/ability-equipment-integration-results.xml` 与 `ability-equipment-integration.log`。联动测试通过虚拟键盘驱动真实场景，验证两种上限、重复实例、最后同类卸载、退款、UI 刷新、空槽及启停；适用于删除追加操作前版本，不验证屏幕绘制与物理键盘 |
| 联动阶段无关文件核对 | 112 份无关文件 SHA256 与阶段开始前一致；Test Runner 的 runInBackground 已恢复 0，ProjectSettings / Packages / EditorBuildSettings 无差异 | 2026-11-01，对照 `GameJam2026/Logs/ability-integration-preflight-hashes.json`；核心与数据资源、角色、SampleScene、槽位 Prefab 均保持原样 |
| 移除追加操作与静态审查 | 删除追加事件、输入分支、联动处理和 UI 提示；WASD/Space 保留，左右 Shift＋Space 等同普通 Space；本次范围内未发现确定缺陷 | 2026-11-01，按用户要求修改 AbilityInput、AbilityEquipmentController、AbilityUI 及输入/联动测试；AbilityManager 仅清理旧操作注释，场景接线保持原样 |
| 移除追加后编译与完整回归 | 51/51 通过：管理器 29、UI 7、输入 8、联动 7；Unity 正常退出，进程退出码 0 | 2026-11-01，Unity 2022.3.62f3c1 / Test Framework 1.1.33；`GameJam2026/Logs/ability-remove-add-results.xml` 与 `ability-remove-add.log`。虚拟键盘验证 Space/左右 Shift＋Space 只切换装卸、混装互不影响及原有导航/UI/启停；两项上限通过测试预置管理器状态验证，没有保留玩家追加入口 |
| 移除追加后无关文件核对 | 121 份无关文件 SHA256 与本轮开始前一致，Test Runner 的 runInBackground 已恢复 0 | 2026-11-01，对照 `GameJam2026/Logs/ability-remove-add-preflight-hashes.json`；包括场景、Prefab、配置资源、Packages、ProjectSettings 和其余脚本 |
| 能力装备联动人工验收 | 用户反馈删除追加后的测试通过 | 2026-11-01，用户明确回复“测试通过，下一阶段”；操作过程和分辨率未逐项记录，不作本次工具执行的证据，也不代替 Windows Player 验收 |
| 能力装备最终静态复核 | 未发现确定的阻塞缺陷；运行时/Editor 程序集隔离，场景、Prefab、配置引用一致 | 2026-11-01，本阶段只读审查能力数据、装备管理、输入、UI、联动及资源；默认构建列表仅 SampleScene，新增 Editor-only 入口显式打包 AbilityEquipmentTest，不改场景列表 |
| 能力装备 Windows x64 构建 | 指定 Unity 编译并构建成功，进程退出码 0；仅包含 AbilityEquipmentTest 场景 | 2026-11-01，`GameJam2026/Logs/ability-equipment-windows-build.log` 记录 Build Finished / Success、输出路径及正常退出；产物位于 `GameJam2026/Builds/Windows/AbilityEquipmentTest/20261101-123930-983/AbilityEquipmentTest.exe`，能力 Editor 和测试程序集未进入 Managed 输出 |
| 能力装备 Windows 启动检查 | Direct3D 11 实际创建设备，程序持续运行 15 秒，日志未见异常或 Shader 错误 | 2026-11-01，`GameJam2026/Logs/ability-equipment-windows-player-d3d11.log` 与 `ability-equipment-windows-player-d3d11-smoke.json`；观察后由工具结束本次后台进程，不作手动退出或真实画面/输入通过证据。首次 Null 图形设备存活 10 秒但报 Shader 不支持，记录保留在 `ability-equipment-windows-player.log`，未计为图形启动通过 |
| 最终验证阶段无关文件核对 | 原有 127 份文件 SHA256 与开始前一致；仅新增 Editor 构建入口及 .meta，并更新文档 | 2026-11-01，对照 `GameJam2026/Logs/ability-final-validation-preflight-hashes.json`；构建自动写入的 UniversalRP.asset 序列化字段已恢复，场景、Prefab、运行时/测试脚本、配置、Packages、ProjectSettings 均无本轮差异 |
| 能力装备 Windows 画面与输入验收 | 待用户在 EXE 中验证；此前通过反馈适用于 Editor 内操作 | 2026-11-01，检查中文、1280×720 / 1920×1080 布局、WASD、Space、Shift＋Space 不追加、空槽、切换窗口焦点及手动关闭；步骤见 README.md |
| 白盒场景与脚本接线 | 文件接线完成；场景、控制脚本和物理材质已由 Unity 导入；未发现重复或缺失的本地 fileID/GUID | 2026-10-30，本次静态引用核对和 Unity 导入日志；不代表 Play Mode 验收 |
| 角色白盒 Play Mode / 人工试玩 | 未实际验证 | 没有角色运行记录；能力 UI/输入的自动 Play Mode 测试另见上方 |
| Windows 角色白盒构建及产物启动 | 未实际验证 | 本轮只构建并启动独立装备测试包；SampleScene 角色白盒仍没有构建与启动记录 |

## 下一项验证

- 本次能力数据阶段仍可按 README.md 在 Inspector 核对配置资源并运行 Validate Configuration；能力装备核心的 EditMode 用例已由 Unity 命令行执行并通过。
- 编辑器内删除追加后的操作已获用户通过反馈；独立 Windows 装备测试包已构建并完成后台启动检查，下一项是该 EXE 的实际画面/输入验收。WASD 选择、Space 装卸，左右 Shift＋Space 不追加；两种能力各一份时为 2/4、剩余 6/12，切换窗口后应正常恢复输入。当前仅两种能力，两项上限由自动测试预置状态覆盖，步骤见 README.md。
- 在 SampleScene 中完成人工验收：A/D 移动、松开及同时按下停止、平台落地、走下平台后的落地、两侧墙体阻挡；操作步骤见 README.md。
- 角色白盒人工验收后，另行构建包含 SampleScene 的 Windows 包并从输出目录启动；独立装备包的构建结果不能替代角色白盒验收。

## 更新规则

- 只有新的检查证据才能改变结果；记录检查日期、检查方式、结果及来源。
- 只更新本次实际检查的项目，其余项目保留原状态。
- 运行结果只适用于被检查的版本；相关代码、资源或配置变更后，应明确哪些检查需要重做。
