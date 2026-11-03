# 环境与验证状态

记录整理日期：2026-11-03。
技术约束和目录说明见 AGENTS.md；下表只记录检查状态与证据。

| 检查项 | 当前结果 | 日期与依据 |
| --- | --- | --- |
| 第七阶段最终代码与场景复核 | 范围内未发现确定 P1/P2；主场景/测试场景共用原 Canvas，唯一装备宿主和角色引用正确；只修正跳跃资源过期说明，没有运行时 C# 改动 | 2026-11-03，只读代码与序列化检查；`GameJam2026/Logs/ability-stage7/review-summary.json`、`static-integration-report.json`。73 个 GUID 合法唯一，场景/Prefab 本地 fileID 无重复 |
| 第七阶段编译及完整回归 | **101/101 通过，进程退出码 0**：原 100 项＋1 条虚拟键盘完整流程测试；菜单集成共 10 项 | 2026-11-03，Unity 2022.3.62f3c1 / Test Framework 1.1.33；`GameJam2026/Logs/ability-stage7/regression-results.xml`、`regression.log`、`test-summary.json`。经 EditMode 入口切入 Play Mode，完整走空装备、原 UI 导航装两种能力、容量/Reader、实际跳跃落地/冲刺、卸载失效/基础移动及重载后单一新会话，没有绕过 UI 直接改装备 |
| 第七阶段 Windows x64 交付构建 | 构建成功，进程退出码 0；原构建列表仅 SampleScene，产物含角色及四个能力运行时程序集，不含 Editor/测试程序集 | 2026-11-03，`GameJam2026/Logs/ability-stage7/windows-build.log` 与 `build-summary.json`；产物 `GameJam2026/Builds/Windows/AbilityIntegrationStage7/20261103-174533-703/AbilityIntegrationStage7.exe`，保留全部同目录数据文件 |
| 第七阶段 Windows 图形启动检查 | 强制 Direct3D 11，实际创建 NVIDIA 图形设备并存活 15 秒，Player 日志未见异常、错误或 Shader 错误；观察后由工具结束进程 | 2026-11-03，`GameJam2026/Logs/ability-stage7/windows-player-d3d11.log`、`windows-player-smoke.json`、`startup-validation-summary.json`；后台启动不接受真实输入，不作为人工画面/手感、焦点恢复或手动退出通过证据 |
| 第七阶段 Console 与人工验收边界 | 本次后台编译/测试/构建无脚本编译错误、意外脚本异常或测试失败；未操作交互式 Console，真实键盘/鼠标、中文布局/手感、焦点和手动退出待用户验收 | 2026-11-03，Editor 日志仍有既有许可握手回退/令牌及无图形设备光照探针提示；过快临时冲刺配置的预期 Warning 由测试显式检查。README 第七阶段提供新包路径、两种分辨率启动命令与人工验收表 |
| 第七阶段修改范围与工作区保护 | 修改原菜单测试、Jump_Test.asset 单行说明、README、ENV_CHECK 共 4 份，其余 155 份基线文件未变；无新增项目资源，无暂存/提交/推送 | 2026-11-03，对照 `GameJam2026/Logs/ability-stage7/preflight-hashes.json` 与 `final-scope-audit.json`。Test Runner runInBackground 和构建自动追加的 32 个 URP 字段均核对恢复；场景/Prefab、运行时 C#、能力数值参数、Packages 与最终 ProjectSettings 均保持原样 |
| 第六阶段输入隔离与生命周期 | 沿用 Gameplay/AbilityMenu、输入门禁与队列；只补齐 UI、Input、Canvas、装备宿主单组件失效时的安全退出，恢复后可重开且装备不变 | 2026-11-03，原场景/Prefab 接线保持不变；Coordinator 与测试最终差异只读审查未发现确定缺陷。修复前 `GameJam2026/Logs/ability-stage6/diagnostic-results.xml` 单项实际复现禁用 AbilityUI 后 Canvas 未关闭 |
| 第六阶段编译及完整自动回归 | 100/100 通过，进程退出码 0：原 97 项与新增输入边界 3 项；菜单集成共 9 项 | 2026-11-03，Unity 2022.3.62f3c1 / Test Framework 1.1.33；`GameJam2026/Logs/ability-stage6/regression-results.xml`、`regression.log`、`test-summary.json`。通过 EditMode 入口切入 Play Mode，验证实际场景/刚体、虚拟键盘、多事件同批模式切换、4 种组件单独禁用/恢复、Coordinator 3 轮启停后单次装卸；原跳跃 11 与冲刺 14 项均通过 |
| 第六阶段测试准备修正与问题复现 | 初期两轮定向各 7/9，2 项遇普通协程等待限制；单项诊断实际命中旧功能断言；改用 fixedTime 推进加 yield null 后完整回归全部通过 | 2026-11-03，`boundary-before-results.xml`、`boundary-repro-results.xml`、`diagnostic-results.xml` 与最终回归保留在 `GameJam2026/Logs/ability-stage6/`。没有用 LogAssert 吞掉意外错误，也未修改游戏物理来绕过测试 |
| 第六阶段 Console 与验证边界 | 最终后台无脚本编译错误、意外脚本异常或测试失败；未打开交互式 Console，未做真实键盘/鼠标、画面/手感验收，未构建或启动 Windows 包 | 2026-11-03，最终日志仍有既有许可握手回退/令牌和无图形设备光照探针提示；过快冲刺临时配置测试的预期 Warning 保留。第五阶段构建结果仅作历史记录，其产物不含本次兜底修复 |
| 第六阶段改动边界 | 修改 Coordinator、原菜单集成测试、README、ENV_CHECK 共 4 份；其余 155 份基线文件保持原样，无新增项目资源；没有暂存/提交/推送 | 2026-11-03，对照 `GameJam2026/Logs/ability-stage6/preflight-hashes.json` 与 `final-scope-audit.json`；保留本轮前全部未提交内容，Test Runner 的 runInBackground 已核对恢复。原 Manager/运动/能力数据、场景/Prefab、Packages、ProjectSettings 均未改 |
| 第五阶段基础冲刺与场景接线 | 原 Player 新增 PlayerDash；Left Shift 读取实际装备，Distance 4 / Duration 0.2，方向输入优先、否则最后朝向；统一刚体速度写入、形状投射与连续碰撞、跳跃预算排除冲刺物理步均已接线 | 2026-11-03，现有脚本／数据／场景静态核对与只读复核。原主场景 63 个块仅修改 Player 组件列表，新增 200007，其余 62 块字节不变；Manager、装备结构、共享 Canvas、测试场景和跳跃资源未修改 |
| 第五阶段编译及完整回归 | 首轮 97/97 通过，进程退出码 0：原 75 项＋Duration 配置 8 项＋主场景冲刺集成 14 项 | 2026-11-03，Unity 2022.3.62f3c1 / Test Framework 1.1.33；`GameJam2026/Logs/ability-stage5/regression-results.xml`、`regression.log`、`test-summary.json`。覆盖实际 4 格行程、配置克隆／非整步时长、地面／空中冲刺、朝向、墙阻挡、耗尽跳跃预算后冲刺、首末步不扣预算、Shift＋Space 的地面／空中结束判断、重复实例、菜单／卸载取消和无冷却／长按不重播 |
| 第五阶段配置与验证边界 | Duration 要求有限且大于零；所需冲刺速度超过当前 Physics2D.maxTranslationSpeed 时拒绝并输出明确警告，默认速度 20 正常；后台未见本次编译错误或意外脚本异常 | 2026-11-03，指定版本实际编译和 97 项测试；其中一项用过快的临时配置验证预期警告与无位移。仍有既有许可握手回退／令牌及无图形设备提示。未操作交互式 Console、未做真实键盘／画面／手感验收；人工步骤见 README 第五阶段 |
| 第五阶段主场景 Windows x64 构建 | 构建成功，进程退出码 0，仅包含原构建列表的 SampleScene；未启动产物 | 2026-11-03，`GameJam2026/Logs/ability-stage5/windows-build.log` 记录 `Build Finished, Result: Success` 及正常退出；产物 `GameJam2026/Builds/Windows/PlayerDashStage5/20261103-validation/PlayerDashStage5.exe` |
| 第五阶段改动边界 | 保留阶段开始前所有未提交内容；现有 9 文件修改、新增 3 个脚本及对应 .meta，其他 149 份基线文件无差异；没有暂存／提交／推送 | 2026-11-03，对照 `GameJam2026/Logs/ability-stage5/preflight-hashes.json` 与 `final-scope-audit.json`。Test Runner 的 runInBackground 和构建追加的 32 个 URP 字段均已核对恢复；Packages、ProjectSettings、原 Manager、共享 UI 与测试场景保持原样 |
| 第四阶段实际跳跃与主场景接线 | Player 新增 PlayerJump，复用实际装备查询与原刚体；Space 起跳、真实接触落地、累计普通横移预算及菜单输入隔离已接线；没有实现冲刺 | 2026-11-03，脚本与场景静态复核；相对本阶段快照，原 62 个场景块仅修改 Player 组件列表，新增 200006，其他 61 块字节不变。跳跃高／水平预算均读取现有 JumpAbilityData，资源、AbilityManager、共享 UI 和测试场景未修改 |
| 第四阶段编译与完整回归 | 最终 75/75 通过，进程退出码 0：原 64 项＋新增真实场景跳跃测试 11 项 | 2026-11-03，Unity 2022.3.62f3c1 / Test Framework 1.1.33；`GameJam2026/Logs/ability-stage4/regression-final-results.xml`、`regression-final.log`。通过 EditMode 入口进入 Play Mode，覆盖配置高度变化、空中重复／长按、落地重跳、累计反向行程、0.23 格末步截断、撞墙零位移、菜单及卸装重装不补预算、装备重复实例与输入隔离 |
| 第四阶段测试夹具修正记录 | 首轮 64/75，新 11 项未通过准备阶段；定向复测 10/11，首项仍有不支持的 yield；准备阶段改为逐帧等待后最终全量通过 | 2026-11-03，`regression-results.xml`、`jump-rerun-results.xml` 与最终结果保留在 `GameJam2026/Logs/ability-stage4/`。Test Framework 1.1.33 的 UnitySetUp 不支持普通 WaitForFixedUpdate／AsyncOperation yield；改为 yield null 等待落地与场景加载，没有吞错误日志或修改游戏逻辑来绕过测试 |
| 第四阶段主场景 Windows x64 构建 | 构建成功，进程退出码 0；使用原构建列表，仅包含 SampleScene | 2026-11-03，`GameJam2026/Logs/ability-stage4/windows-build.log` 记录 `Build Finished, Result: Success` 及正常退出；产物 `GameJam2026/Builds/Windows/PlayerJumpStage4/20261103-validation/PlayerJumpStage4.exe`。没有启动该 EXE 或做真实键盘／画面验证 |
| 第四阶段改动边界与 Console | 已保存阶段前哈希，保留全部此前未提交修改；Test Runner 的 runInBackground 和构建生成的 URP 字段已核对并恢复；能力数据、Packages、ProjectSettings 无本阶段差异 | 2026-11-03，`GameJam2026/Logs/ability-stage4/preflight-hashes.json`、场景快照和最终范围审计；后台编译／测试／构建未见本次脚本错误，仍有许可客户端握手回退／访问令牌和无图形设备提示。未操作交互式 Console，人工手感／画面验收待执行 |
| 第三阶段角色查询与主场景接线 | PlayerAbilityController 已挂在原 Player；Coordinator 连接现有 Manager，直接查询并转发装备变化，Owner 销毁时通知解除旧会话；没有新增 Manager、独立装备列表或能力执行 | 2026-11-03，实际代码／场景静态核对；主场景新增组件 200005、Player 与 Coordinator 两项必要引用，其余 59 个原块字节不变；测试场景和共享 Canvas 与本阶段基线一致 |
| 第三阶段编译与完整回归 | 最终 64/64 通过，退出码 0：原 57 项＋新增查询连接 7 项（EditMode 3、进入 Play Mode 的场景测试 4） | 2026-11-03，指定 Unity 2022.3.62f3c1 / Test Framework 1.1.33；`GameJam2026/Logs/ability-stage3/regression-rerun-results.xml`、`regression-rerun.log`；覆盖真实装备同步、重复实例、查询顺序、事件启停／换源、UI 宿主销毁与场景重载 |
| 第三阶段测试发现修正 | 首轮编译和原 57 项通过，但新增测试 .meta GUID 为 33 位而被忽略；修正为合法 32 位并核对 Assets 所有 .meta 格式后，完整重跑实际执行 64 项 | 2026-11-03，首轮 `GameJam2026/Logs/ability-stage3/regression.log` 记录 GUID 无效／资源忽略；最终 rerun 日志记录正确导入并保存 64 项通过结果，首轮不算新增验证成功 |
| 第三阶段范围与验证边界 | 保留全部第二阶段未提交内容；AbilityManager、能力配置、移动脚本、测试场景、共享 Canvas、Packages 均未改；ProjectSettings 的 Test Runner 副作用已恢复 | 2026-11-03，对照 `GameJam2026/Logs/ability-stage3/preflight-hashes.json`；最终后台无本次编译错误或测试失败，仍有许可回退／令牌与无图形设备提示。未操作交互式 Console、真实键盘／画面或 Windows 构建；人工查询日志步骤见 README |
| 第二阶段共享 UI 与场景接线 | 指定 Unity 后台编译和一次性迁移成功，退出码 0；两个场景使用相同 Canvas Prefab，各一个装备宿主、Coordinator 与 EventSystem，主场景默认关闭／测试场景默认打开 | 2026-11-02，`GameJam2026/Logs/ability-stage2/migration-retry.log` 记录 Tundra build success、ABILITY_MENU_SCENES_INTEGRATED 和正常退出；场景／Prefab GUID、fileID、绑定及 .meta 静态检查无缺失 |
| 第二阶段编译与完整自动回归 | 最终 57/57 通过：核心 29、UI 7、输入 8、装备联动 7、新菜单集成 6；进程退出码 0 | 2026-11-02，指定 Unity 2022.3.62f3c1 / Test Framework 1.1.33；`GameJam2026/Logs/ability-stage2/regression-rerun-results.xml`、`regression-rerun.log`。从 EditMode 入口进入 Play Mode，验证虚拟键盘、实际场景、横移门禁与继续下落；不代表画面、真实键盘或构建通过 |
| 第二阶段失败与修正记录 | 沙箱内首次 Unity 启动未生成日志、未进入编译，结束仅该进程后沙箱外迁移成功；首轮测试 51/57，新 6 项为夹具嵌套协程错误，修正顶层 yield 后全量重跑通过 | 2026-11-02，`GameJam2026/Logs/ability-stage2/regression-results.xml`、`regression.log` 与最终 rerun 结果；未通过修改游戏功能绕开夹具失败 |
| 第二阶段 Console 与验证边界 | 未发现本次脚本编译错误；最终测试无失败或意外脚本异常。后台仍有许可客户端握手回退／令牌和无图形设备提示；未打开交互式 Console，未做画面／真实键盘复核、未重建 Windows 包 | 2026-11-02，本次后台编译／测试日志；此前 Windows 产物不包含本阶段改动，人工步骤见 README |
| 第二阶段改动边界 | 主场景原 38 个序列化块原样保留；原测试 EventSystem、AbilityManager、能力数据、Packages 不变；Test Runner 的 runInBackground 已恢复，ProjectSettings 无功能差异；原未跟踪 SceneTemplateSettings.json 保留 | 2026-11-02，与 `GameJam2026/Logs/ability-stage2/preflight-hashes.json` 及场景快照比对；UI 开关不销毁会话，无跳跃／冲刺实现或能力配置修改 |
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
| 角色白盒 Play Mode / 人工试玩 | 第七阶段实际场景自动 Play Mode 回归已通过；人工试玩待执行 | 自动输入与物理证据见上方第七阶段结果；没有本次真实键盘、交互式画面或手感验收记录 |
| Windows 角色白盒构建及产物启动 | 第七阶段 SampleScene 构建及 Direct3D 11 后台启动均成功；人工试玩与手动退出待执行 | 2026-11-03，当前产物和证据见上方第七阶段行；保留旧构建/启动记录，互不替代 |

## 下一项验证

- 第七阶段工具侧验收已完成：101 项回归、新 Windows 构建和 Direct3D 11 后台启动均通过。下一项由用户启动 README 第七阶段的新 EXE，按表验证真实按键、中文布局、操作手感、窗口焦点与手动退出；在 Unity 检查交互式 Console，反馈后再处理实际问题。当前人工验收仍未完成。
- 本次能力数据阶段仍可按 README.md 在 Inspector 核对配置资源并运行 Validate Configuration；能力装备核心的 EditMode 用例已由 Unity 命令行执行并通过。
- 编辑器内删除追加后的操作已获用户通过反馈；独立 Windows 装备测试包已构建并完成后台启动检查，下一项是该 EXE 的实际画面/输入验收。WASD 选择、Space 装卸，左右 Shift＋Space 不追加；两种能力各一份时为 2/4、剩余 6/12，切换窗口后应正常恢复输入。当前仅两种能力，两项上限由自动测试预置状态覆盖，步骤见 README.md。
- 在 SampleScene 中完成人工验收：A/D 移动、松开及同时按下停止、平台落地、走下平台后的落地、两侧墙体阻挡；操作步骤见 README.md。
- 角色白盒人工验收后，另行构建包含 SampleScene 的 Windows 包并从输出目录启动；独立装备包的构建结果不能替代角色白盒验收。

## 更新规则

- 只有新的检查证据才能改变结果；记录检查日期、检查方式、结果及来源。
- 只更新本次实际检查的项目，其余项目保留原状态。
- 运行结果只适用于被检查的版本；相关代码、资源或配置变更后，应明确哪些检查需要重做。
