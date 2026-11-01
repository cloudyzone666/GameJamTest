# GameJam2026

Unity 2022.3.62f3c1 的 2D Game Jam 工程，使用 URP 14.0.12 和 New Input System 1.14.2，目标平台为 Windows。

## 打开工程

1. 在 Codex 中使用本仓库根目录作为项目目录。
2. 在 Unity Hub 中打开仓库内的 GameJam2026 文件夹。
3. 当前入口场景：GameJam2026/Assets/Scenes/SampleScene.unity。

## 当前可玩范围与操作

- 2026-10-30：入口场景已接入单房间白盒、角色左右移动和实体碰撞。当前范围没有跳跃、冲刺、能力拼装、关卡流程或正式美术。
- A 向左，D 向右；松开停止横向移动，同时按住 A 和 D 时横向输入抵消。方向键没有绑定。
- 玩家从橙色 TestPlatform 上方落下，可在平台上左右移动，也可以走下平台落到 Ground，再测试两侧墙体碰撞。
- 需要重新回到初始位置时，停止 Play Mode 后再次进入；本阶段没有增加重生或重置功能。
- 场景接线和文件配置已完成；编译、试玩及构建的实际验证状态见 ENV_CHECK.md。

以下按阶段保留能力装备开发记录，当前装备测试场景的操作与验收以“能力装备联动阶段”为准；2026-11-01 已按用户要求移除追加操作。

## 能力数据阶段（2026-10-31）

本阶段只建立能力配置，不包含装备管理、UI、输入交互或能力执行。ScriptableObject 是可在 Inspector 中编辑、可供多个系统引用的数据资源，不需要挂到 GameObject；当前没有新增场景或场景接线。

- 脚本位于 GameJam2026/Assets/Scripts/Abilities/Data/：AbilityData 定义公共字段和只读参数接口，JumpAbilityData / DashAbilityData 保存各自参数，AbilityType 预留四类，AbilitySystemConfig 保存两项独立限制，AbilityCatalog 保存分类与位置。
- GameJam2026.Abilities.asmdef 将能力数据放入独立程序集；没有引入额外 Package。能力资源不保存玩家装备状态，仓库内 ID 唯一校验不限制后续重复装备同一能力。
- 配置资源位于 GameJam2026/Assets/Data/Abilities/：

| 资源 | 当前配置 |
| --- | --- |
| DefaultAbilitySystemConfig.asset | Total Capacity = 12，Max Equipped Count = 4；两者可分别修改 |
| Jump_Test.asset | ID = jump.test；Slot Cost = 2，Vertical Height = 2，Max Horizontal Distance = 2 |
| Dash_Test.asset | ID = dash.test；Slot Cost = 4，Distance = 4 |
| TestAbilityCatalog.asset | 移动、跳跃、冲刺、特殊四类，每类四个位置；跳跃和冲刺各在本类首位，其余 14 位为 None |

### 本阶段 Inspector 验收

1. 用指定 Unity 版本打开工程，在 Project 窗口进入 Assets/Data/Abilities。依次选择四个资源，核对上表字段；图标暂为 None，参数单位尚未定义，本阶段只保存数值。
2. 展开 TestAbilityCatalog 的 Categories 和每类的 Slots，确认四列顺序、每列四位和两项能力引用。空位置保持 None，不补充未设计的能力。能力类型由具体数据类确定，无需再手动填写一份类型字段。
3. 在配置资源 Inspector 的上下文菜单中选择 Validate Configuration，Console 应输出配置有效；当前工具已做静态检查及 Unity 后台导入、编译，尚未操作交互界面运行此菜单。
4. 后续新增同类能力时，可在 Project 窗口通过 Create > GameJam2026 > Abilities > Jump Ability 或 Dash Ability 创建资源，填写独立 ID、名称、消耗和参数，再拖入对应分类的空位置。本轮不需要创建更多资源。

本阶段导入与编译证据见 ENV_CHECK.md；不代表装备规则、按键、UI 或角色能力已实现。

## 能力装备核心阶段（2026-10-31）

本阶段新增纯 C# 的 AbilityManager，不继承 MonoBehaviour，不需要挂载到 GameObject，也没有接入现有场景。创建管理器时传入 AbilitySystemConfig；管理器立即读取并固定当次会话容量和实例上限。修改 Inspector 资源只影响之后新建的管理器。

- TryEquip / CheckEquip：先验证能力配置，再检查最大实例数，最后检查剩余能力格。失败返回 AbilityEquipResult，TryEquip 的 out 实例为 null；不会改变已装备集合、消耗或发出事件。
- 每次成功装备都会创建 EquippedAbility，包含独立 InstanceId、能力定义引用，以及装备时保存的 AbilityId 和 ChargedSlotCost。同一资源可重复装备，单份成本为 0 时仍占一个实例位置。
- TryUnequip(instanceId, out removed)：按副本 ID 精确卸载。TryUnequipLast(ability, out removed) 按定义 ID 移除最后装备的匹配副本。两者都按副本保存的成本返还能力格；卸载失败返回 false，out 为 null。
- EquippedAbilities 提供只读列表；GetEquippedCount 按 AbilityId 统计同类副本。成功装备或卸载后发出一次 EquipmentChanged 事件，订阅者可在回调中读取完整的新状态。
- AbilityManager 只提供装备状态和只读能力定义，尚未让角色执行跳跃或冲刺，也没有输入、UI、持久化、运行时调节容量或组合效果。

### 本阶段 EditMode 验收

已使用 Unity Test Runner 对 GameJam2026.Abilities.Tests.AbilityManagerTests 执行 EditMode 测试，结果为 **29/29 通过**；编译和运行证据见 ENV_CHECK.md。需要手动重跑时，在 Window > General > Test Runner 选择 EditMode 并运行该测试类。测试改值作用于临时克隆，没有写回能力配置资源。

能力装备管理器仍不接入角色。下方独立白盒场景只负责显示 Catalog、选择能力和查看数据，不创建或模拟装备管理器状态。

## 能力装备 UI 白盒阶段（2026-10-31）

独立场景 `GameJam2026/Assets/Scenes/AbilityEquipmentTest.unity` 使用 uGUI、Screen Space Overlay Canvas 和 Input System 的 `InputSystemUIInputModule`。此阶段完成鼠标选择与数据展示；键盘输入见下一阶段说明。

- `AbilityUI` 挂在场景的 AbilityCanvas；Catalog、System Config、AbilitySlot Prefab、16 个槽位容器、顶部显示和右侧详情均已在场景中接线。Catalog 保留四类各四个固定位置，其中 Jump 和 Dash 各一个，其他 14 个为空；空槽也能选中。
- `AbilitySlotView` 在 `Assets/Prefabs/Abilities/AbilitySlot.prefab` 上，负责显示单格、空槽占位与选中框。选择状态由 `AbilityUI.SelectSlot(column, row)` 统一维护；右侧展示名称、类型、格数消耗、描述、原始参数及仅用于阶段说明的未装备状态文本。
- 顶部以 4 个实例位置和 12 个能力格表示初始配置，显示 `已装备 0 / 4`、`剩余能力格 12 / 12`。此阶段不动态模拟装备变化。
- 为避免将系统字体复制进仓库，缺省中文字体在 Windows Player/Editor 中通过 `Font.CreateDynamicFontFromOSFont` 从已安装字体查找；在 Inspector 指定 `fontOverride` 可覆盖。此白盒方案依赖目标 Windows 安装中文字体；发布前应加入许可合适的项目字体资源。
- 场景通过 Editor 脚本首次生成时使用 Unity Editor API；生成后的 Prefab 与场景是普通项目资源，不依赖再次运行生成器。不要从含有未保存场景的交互式 Editor 运行 `CreateScene`。

### 本阶段自动验收

指定 Unity 2022.3.62f3c1 与 Test Framework 1.1.33 批处理进入 Play Mode，UI 集成结果为 **7/7 通过**，最终结果文件为 `GameJam2026/Logs/ability-ui-integration-final-results.xml`。覆盖 16 槽和顶部初始化、Jump/Dash 参数详情、空槽与越界处理、按钮回调、EventSystem 配置和反复启停、动态字体释放、参考画布布局边界。日志记录 7 次进入/退出 Play Mode、结果保存和正常 batchmode 清理。首次以 PlayMode 平台过滤 Editor-only 测试时 Unity 未发现用例，该次没有执行测试，不计入结果。

### 本阶段人工验收

在 Unity 中打开上述场景并进入 Play Mode：确认中文字体无缺字、Canvas 与详情布局没有溢出；鼠标依次点选 Jump、Dash 和一个空槽，确认详情更新且空槽不保留旧内容。再在 Game 视图选择 1280×720 与 1920×1080 检查布局。2026-11-01 用户反馈上一阶段测试通过；具体操作与分辨率没有逐项补充记录，不作本次工具执行的验收结果。

## 能力装备输入阶段（2026-11-01）

第四阶段让键盘与鼠标共用同一套槽位选择，并发送装备意图事件，当时尚未连接 AbilityManager。事件是通知其他代码处理操作的 C# 接口；输入组件只负责发出请求，装备数量、容量和卸载副本由第五阶段的联动层处理。

- 新增 `Assets/Scripts/Abilities/Input/AbilityInput.cs`，位于独立 `GameJam2026.Abilities.Input` 程序集，引用核心、UI 和现有 Input System。它已挂到测试场景的 `AbilityCanvas`，Inspector 的 `Ui` 已绑定同对象的 `AbilityUI`。第四阶段曾开启 `Log Requests` 观察请求，联动阶段已关闭该调试输出。没有新增 Package 或 InputAction 资源。
- W/A/S/D 分别向上/左/下/右移动选择，每次按下移动一格，原方向键不再导航。向上行号减一，向下行号加一。边界停止，不循环、不长按连发；空槽保留并可选择。鼠标选中后，WASD 从该位置继续。UI 的 `MoveSelection` 按当前实际列数和各列位置数计算，不写死四列四行。
- Space 只发出 `ToggleRequested(AbilityData)`；Shift＋Space 按普通 Space 处理。单独按 Shift、按住 Space 后改变 Shift 都不会多发请求；空槽不发请求。原追加事件与分支已移除。
- 联动层将 Toggle 解释为“未装备时添加一个、已装备时卸载最后装备的一个匹配实例”，面板没有重复添加同一能力的入口。第四阶段只发送请求，顶部曾保持 `0/4` 和 `12/12`；第五阶段开始实际执行规则并刷新显示。
- InputActionMap 在 Awake 创建并注册回调；OnEnable/OnDisable 启停，OnDestroy 释放。禁用输入组件或 UI 后不处理输入，重新启用时不会补触发已经按住的 WASD 或 Space。EventSystem 的自动导航仍关闭，避免重复响应。

### 本阶段自动验收

修改为 WASD 之前，指定 Unity 2022.3.62f3c1 批处理执行能力系统回归，结果 **44/44 通过**：装备核心 29、UI 集成 7、输入集成 8。输入与 UI 用例从 EditMode 测试入口显式进入/退出 Play Mode，输入使用虚拟键盘，覆盖四向导航和边界、空槽、长按、两侧 Shift、Space 后追加 Shift、禁用/重启及鼠标选择与键盘选择共用状态。结果文件为 `GameJam2026/Logs/ability-input-regression-final-results.xml`，对应日志记录编译成功、保存结果和正常 batchmode 退出，进程退出码 0。这是改键前的验证记录；改键当轮没有重跑 Unity 测试，第五阶段的回归已覆盖 WASD。

首轮结果为 43/44，首次方向键用例失败。诊断确认无界面 Editor 没有焦点，虚拟键状态在 Editor 更新中变化而未触发游戏动作；测试现使用不保存的 InputSettings 克隆将虚拟输入送入游戏更新，结束后恢复原设置。修正后首键单例 1/1、完整回归 44/44；没有为此修改生产输入逻辑或项目输入设置。Test Runner 引起的 `runInBackground` 变化已恢复，22 份无关文件与本阶段开始前的 SHA256 一致。自动测试不代表真实键盘、窗口焦点、画面绘制或 Windows 构建已验收。

### 本阶段人工验收

2026-11-01 用户在输入阶段及 WASD 改键后反馈测试通过。此为用户反馈，没有逐项记录操作过程；当前实际装备操作改按下方联动阶段验收。

## 能力装备联动阶段（2026-11-01）

独立测试场景现已支持实际装备、卸载及 UI 动态刷新。2026-11-01 按用户要求删除 Shift＋Space 追加功能，面板操作使每种能力在未装备与装备一份之间切换。能力数值仍只保存和展示，本阶段不执行角色移动、跳跃或冲刺，也没有存档；重新进入 Play Mode 会创建空装备会话。

- 新增 `Assets/Scripts/Abilities/Integration/AbilityEquipmentController.cs` 及独立程序集，负责把输入请求转交给已有 AbilityManager。`Awake` 创建管理器，`Start` 在 UI 初始化后订阅输入和 `EquipmentChanged`；禁用时退订，重新启用时沿用该会话并刷新界面。没有新增单例、轮询或 Package。
- AbilityManager 仍不引用 UI；联动组件收到装备变化事件后调用 `AbilityUI.RefreshEquipment`。UI 只查询管理器，更新实例灯、剩余能力格、每种能力的副本数量和当前详情。`AbilitySlotView` 用绿色底色表示已装备，蓝色边框表示选中，两种状态可同时存在。
- WASD 移动选择；Space 在未装备时加一个，已有时卸下一个；左右 Shift＋Space 均按普通 Space 处理，不再追加。原方向键不导航，空槽和长按不额外提交。顶部操作提示仅显示 WASD 与 Space。
- 每次添加都由管理器检查配置、最大实例数和剩余容量。失败不修改状态，顶部 Hint 显示“最多装备”或“能力格不足”等原因；成功显示装备/卸载反馈，换选中恢复按键提示。
- 容量和实例上限读取配置资源，显示以管理器创建时保存的会话值为准；运行中修改资源不会改变当前会话上限。卸载按实例保存的成本退款。

### 场景接线

`AbilityCanvas` 已挂载 AbilityUI、AbilityInput 和 AbilityEquipmentController。Controller 的 System Config 绑定 `DefaultAbilitySystemConfig.asset`，Ui 和 Input 绑定同对象的两个组件；UI 的 Operation Feedback 绑定 `EquipmentPanel/Hint`，其余引用沿用现有场景。无需重新添加组件或运行场景生成器。备用生成器已同步这些字段，但本轮没有重新生成或覆盖场景及 Prefab。

### 本阶段自动验收

移除追加操作前，指定 Unity 2022.3.62f3c1 / Test Framework 1.1.33 后台编译并回归 **51/51 通过**：管理器 29、UI 7、输入 8、完整联动 7，进程退出码 0。结果为 `GameJam2026/Logs/ability-equipment-integration-results.xml`，日志为 `ability-equipment-integration.log`。UI、输入和联动用例从 EditMode 入口显式进入/退出 Play Mode，使用临时虚拟键盘和不保存的 InputSettings 克隆。原输入用例禁用联动组件以隔离检查请求；新增联动用例通过真实场景连接全部模块。

覆盖首装/卸载、不同类型混装、重复副本唯一 ID、只卸最后一个同类、两项限制分别失败、退款后重装、数量/详情/指示器同步、空槽、UI 禁用和联动组件反复启停。Test Runner 的 `runInBackground` 副作用已恢复；112 份无关文件与阶段开始前 SHA256 一致。此次没有执行交互式画面验收或 Windows 构建。

删除追加操作后，2026-11-01 使用同一 Unity 版本重新编译并回归，**51/51 通过**（管理器 29、UI 7、输入 8、联动 7），进程退出码 0。结果为 `GameJam2026/Logs/ability-remove-add-results.xml`，日志为 `ability-remove-add.log`。本轮验证 Space 与左右 Shift＋Space 均只切换装卸、单独 Shift 无操作，保留 WASD、空槽、长按、启停与 UI 刷新覆盖。容量与实例上限用例由测试直接调用管理器预置状态，再用 Space 验证失败和退款；测试前置状态不是玩家追加入口。管理器的数据模型与装备规则没有重写。

本轮没有修改场景、Prefab 或 Inspector 接线；Test Runner 的 `runInBackground: 0 → 1` 副作用已恢复，121 份无关文件与 `GameJam2026/Logs/ability-remove-add-preflight-hashes.json` 的 SHA256 一致。未执行本轮真实键盘/画面验收或 Windows 构建。

### 本阶段人工验收

1. 打开 `Assets/Scenes/AbilityEquipmentTest.unity`，进入 Play Mode 并点击 Game 视图。从左上空槽按 D 选中跳跃，Space 后应为 `1/4`、剩余 `10/12`、跳跃 `×1`；再按 Space 应卸载，回到 `0/4`、`12/12`、`×0`。
2. 选中跳跃，分别用左右 Shift＋Space 重复按下，结果应与普通 Space 相同，只在零份和一份之间切换。单独 Shift、按住 Space 后改变 Shift 均不额外操作。
3. 各装备一份跳跃和冲刺，应为 `2/4`、剩余 `6/12`；卸载跳跃只影响跳跃，冲刺保持 `×1`。当前仅两种测试能力，面板不能再通过重复装备填满两项上限；边界由自动测试预置状态验证，不为此新增能力资源。
4. 已装备时切到另一能力或空槽，检查绿色已装备底色与蓝色选中框相互独立；空槽详情清空且按键不改状态。返回原能力后，数量和详情应保持正确。
5. 临时禁用 AbilityEquipmentController，按装备快捷键不应改变状态；重新启用后状态保留，每次按键只处理一次。完成后保持组件开启。2026-11-01 用户反馈删除追加后的测试通过；具体操作和分辨率未逐项记录，Windows 产物验收另见下一节。

## 能力装备最终验证阶段（2026-11-01）

用户反馈删除追加后的测试通过；具体操作和分辨率没有逐项记录。该反馈与上一轮 51/51 自动回归分别保留，不代表 Windows 构建产物已验收。本阶段静态复核了运行时/Editor 程序集隔离、场景接线及配置引用，未发现确定的阻塞缺陷。

新增 `Assets/Scripts/Abilities/UI/Editor/AbilityEquipmentTestBuild.cs`，仅在 Editor 运行，不挂到 GameObject，也不需要 Inspector 绑定。它显式打包现有 `AbilityEquipmentTest.unity` 为 Windows x64 测试程序，每次输出到新的 `GameJam2026/Builds/Windows/AbilityEquipmentTest/<时间戳>/AbilityEquipmentTest.exe`。原构建列表仍只有 SampleScene；此入口不修改该列表、项目设置或场景内容。

需要手动重建时：保存所有场景、退出 Play Mode，在菜单选择 **GameJam2026 > Abilities > Build Windows Equipment Test**。构建成功后 Console 输出 `ABILITY_EQUIPMENT_WINDOWS_BUILD_SUCCESS` 和 EXE 完整路径；失败会显示原因。整个输出目录一起保留，不能只复制 EXE。后台构建与产物启动的实际结果见 ENV_CHECK.md。

本阶段指定 Unity 2022.3.62f3c1 后台编译并构建成功，进程退出码 0；产物为 `GameJam2026/Builds/Windows/AbilityEquipmentTest/20261101-123930-983/AbilityEquipmentTest.exe`。构建日志为 `GameJam2026/Logs/ability-equipment-windows-build.log`。该包的 Managed 目录包含四个能力运行时程序集，不包含能力 Editor 或测试程序集。本轮未重跑上一轮 51 项测试；原有运行时和测试代码哈希未变。

首次以 `-nographics` 启动存活 10 秒，但 Null 图形设备令 URP 报 Shader 不支持，因此不作为正常图形启动验收。随后改用 Direct3D 11 后台启动，实际创建 NVIDIA 图形设备并持续运行 15 秒，日志没有异常或 Shader 错误；见 `GameJam2026/Logs/ability-equipment-windows-player-d3d11.log` 与 `ability-equipment-windows-player-d3d11-smoke.json`。观察后由工具结束本次进程，未验证手动退出、真实按键或画面。构建自动写入的 URP 序列化字段已恢复，原有 127 份文件与 `ability-final-validation-preflight-hashes.json` 的 SHA256 一致。

Windows 产物人工验收：启动 EXE，在 1280×720 和 1920×1080 下检查中文、布局与详情；用 WASD 选择、Space 装卸，检查左右 Shift＋Space 不追加、空槽无操作、跳跃与冲刺各一份时为 `2/4` 和剩余 `6/12`。切换到其他窗口再返回，确认输入恢复且没有重复装卸。这些 Player 操作仍需单独记录反馈。

不同分辨率可从仓库根目录用 PowerShell 启动；关闭第一轮程序后再运行第二条：

```powershell
$abilityTestExe = Join-Path $PWD 'GameJam2026/Builds/Windows/AbilityEquipmentTest/20261101-123930-983/AbilityEquipmentTest.exe'
& $abilityTestExe -screen-fullscreen 0 -screen-width 1280 -screen-height 720
& $abilityTestExe -screen-fullscreen 0 -screen-width 1920 -screen-height 1080
```

## 场景对象与参数

- Main Camera、Global Light 2D：沿用原有相机和 URP 灯光。
- WhiteboxRoom：包含 Ground、WallLeft、WallRight、TestPlatform；每个实体均使用非 Trigger 的 BoxCollider2D。
- Player：挂载 PlayerController2D、Dynamic Rigidbody2D 和 BoxCollider2D；Move Speed 为 5，Gravity Scale 为 1，冻结 Z 旋转，开启 Interpolate 和 Continuous 碰撞检测。
- Player 的 Transform 缩放为 (1, 1, 1)，BoxCollider2D 的 Size 为 (0.8, 1.2)。零摩擦、零弹性的 PlayerMovement 物理材质用于避免贴墙时因摩擦悬停。
- Player/Visual：仅负责显示，SpriteRenderer 使用 Unity 2D Sprite 包自带的 Square 占位图，当前局部缩放为 (0.8, 1.2, 1)。
- New Input System 的 Move Action 由脚本创建并绑定键盘 A/D，无需额外挂载 PlayerInput 或绑定 InputAction 资产。

## 后续替换角色素材

1. 将图片导入工程，并将 Texture Type 设置为 Sprite (2D and UI)。
2. 选中 Player/Visual，将 SpriteRenderer 的 Sprite 替换为自己的图片，将 Color 调回白色以显示原图颜色。
3. 根据图片的 Pixels Per Unit 调整 Visual 的局部缩放和位置，使脚底与 Player 的碰撞体底部对齐。显示和物理相互独立，修改 Visual 不会缩放 Player 的碰撞体。
4. 如需调整碰撞外形，在 Player 的 BoxCollider2D 上使用 Edit Collider；之后重新验证落地和碰墙。后续动画组件也可放在 Visual 上。

## 本阶段人工验收

1. 用指定 Unity 版本打开工程，等待 Input System 导入和脚本编译，检查 Console。如果编辑器此前使用旧输入后端，重启编辑器后再测试。
2. 打开入口场景并进入 Play Mode，点击 Game 视图使其获得焦点；角色应落在橙色平台上并站稳。
3. 验证 A/D 两个方向、松开停止、同时按 A/D 停止；方向键和 Space 不应触发移动或跳跃。
4. 走下平台后应落在地面；持续向两侧墙移动时不穿墙、不翻倒，反向输入可以离开墙体。
5. 重进 Play Mode 回到平台，再确认角色移动不改变 Visual 相对于碰撞体的位置。

以上是待执行的人工验收步骤，不代表已经试玩通过。

## Windows 构建

1. 在 Unity 2022 中打开 File > Build Settings。
2. 确认目标为 PC, Mac & Linux Standalone / Windows，并检查 Scenes In Build 中的入口场景。
3. 将构建输出到 Unity 工程内的 Builds/Windows 文件夹；该目录已被 Git 忽略。
4. 构建结束后，从输出目录启动程序，检查画面及本版本实现的操作，验证关闭窗口或游戏退出功能，并将实际结果记录到 ENV_CHECK.md。

以上为角色白盒场景的构建步骤，尚未执行。独立能力装备包的构建与启动证据见“能力装备最终验证阶段”。
