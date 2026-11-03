# GameJam2026

Unity 2022.3.62f3c1 的 2D Game Jam 工程，使用 URP 14.0.12 和 New Input System 1.14.2，目标平台为 Windows。

## 打开工程

1. 在 Codex 中使用本仓库根目录作为项目目录。
2. 在 Unity Hub 中打开仓库内的 GameJam2026 文件夹。
3. 当前入口场景：GameJam2026/Assets/Scenes/SampleScene.unity。

## 当前可玩范围与操作

- 2026-11-03：入口场景包含单房间白盒、角色左右移动、实体碰撞和能力装备 UI；装备跳跃后 Space 起跳，装备冲刺后 Left Shift 水平冲刺。普通跳跃横移累计最多 2 格，冲刺另算。没有新增关卡流程或正式美术。
- A 向左，D 向右；松开停止横向移动，同时按住 A 和 D 时横向输入抵消。方向键没有绑定。
- Tab 打开／关闭能力菜单，Esc 关闭。菜单中沿用 WASD 选择、Space 装备／卸载，没有额外的 Shift 或 Ctrl 装备命令；菜单打开时停止角色横移、屏蔽角色输入，重力和世界物理继续运行。
- 游戏中 Shift＋Space 先冲刺，冲刺结束时只有已落地且仍装备跳跃才起跳；没有冲刺装备时 Space 正常判断跳跃。冲刺过程中按 Space 同样等到冲刺结束判断一次，空中结束即丢弃请求。
- 玩家从橙色 TestPlatform 上方落下，可在平台上左右移动，也可以走下平台落到 Ground，再测试两侧墙体碰撞。
- 需要重新回到初始位置时，停止 Play Mode 后再次进入；本阶段没有增加重生或重置功能。
- 场景接线和文件配置已完成；编译、试玩及构建的实际验证状态见 ENV_CHECK.md。

以下按阶段保留开发记录；当前可玩操作以本节与第七阶段交付记录为准。旧阶段的“尚未实现”描述属于当时状态；2026-11-01 已按用户要求移除追加操作。

## 主场景能力菜单接入：第二阶段（2026-11-02）

两个场景均已接线，无需手动添加组件或重新运行生成器：

- `Assets/Prefabs/Abilities/AbilityEquipmentCanvas.prefab` 由原测试场景的完整 AbilityCanvas 提取，保留现有 UI、目录资源、槽位 Prefab、AbilityInput 和 AbilityEquipmentController。
- `SampleScene` 和 `AbilityEquipmentTest` 各使用一个该 Prefab 实例、一个场景级 EventSystem 和一个 `GameSystems/GameplayAbilityCoordinator`。主场景绑定原 Player，`Start With Menu Open` 为 false；测试场景不绑定玩家，此值为 true，保留进入场景即显示装备面板的行为。
- Coordinator 的 Canvas、UI、Input、Equipment Controller 字段均绑定同一个 Canvas 实例的对应组件。Prefab 不引用场景玩家，也不包含 EventSystem。原 Player 的 Rigidbody2D、Collider2D、Move Speed = 5 和速度计算保持原样，仅增加 `SetGameplayInputEnabled` 门禁。
- Coordinator 显式维护 Gameplay / AbilityMenu 状态。关闭时停用 AbilityInput 的 ActionMap、Canvas 和 GraphicRaycaster，保持根对象、AbilityUI 与装备联动组件激活；打开时从现有 Manager 刷新显示。管理器仍只由原装备联动组件在 Awake 创建，不因开关菜单重建或重复订阅。
- 同一输入批次先处理 Tab/Esc，再派发 UI 导航和 Space 请求；Esc 优先，关闭时丢弃待处理 UI 命令。重新打开不会补触发已经按住的 Space 或 WASD。角色门禁清除水平输入与速度并停用 Move Action，保留竖直速度、重力和物理更新。

装备状态属于当前场景的运行会话：菜单开关和 UI 根对象临时禁用不会丢失装备；重新进入 Play Mode 或销毁／重载整个场景会创建新会话。本阶段没有新增跨场景持久化、存档或第二个管理器。

`Assets/Editor/AbilityMenuSceneIntegration.cs` 是本次已执行的一次性迁移工具，只使用 Editor API 提取现有 Canvas 和添加接线；目标 Prefab 或 Coordinator 已存在时会中止，避免覆盖。正常打开和游玩工程不依赖再次运行它。

### 本阶段实际验证

指定 Unity 2022.3.62f3c1 后台编译与场景迁移成功，进程退出码 0。完整回归最终 **57/57 通过**：管理器 29、原 UI 7、原输入 8、原装备联动 7、新场景／输入隔离 6，进程退出码 0。结果为 `GameJam2026/Logs/ability-stage2/regression-rerun-results.xml`，日志为同目录的 `migration-retry.log` 和 `regression-rerun.log`。

测试通过 EditMode 入口显式进入 Play Mode，使用虚拟键盘验证 Tab/Esc、两个场景的唯一装备宿主、装备实例 ID 保留、隐藏 UI 不处理输入、同批次切换与 Space 冲突、持键不重放、横移停止而重力继续、原 A/D 恢复，以及 UI 根禁用／重启／销毁后释放输入。首轮原 51 项通过、新 6 项因测试夹具嵌套协程失败；仅修正夹具后完整重跑通过，没有用功能绕过来消除测试失败。

主场景原 38 个序列化数据块逐字保留，仅追加本阶段对象；测试场景原 EventSystem 保留。Test Runner 的 runInBackground 副作用已恢复，Packages、ProjectSettings、能力数据与原 AbilityManager 未修改。后台日志仍有许可客户端握手回退／访问令牌及无图形设备提示；本次未打开交互式 Console、未做画面或真实键盘验收，也未重新构建 Windows 包。此前 Windows 测试包不包含本阶段接入。

### 本阶段人工复核（待执行）

1. 打开 SampleScene 进入 Play Mode，点击 Game 视图；菜单初始隐藏，A/D 移动和松键停止保持原样。
2. 在角色下落时按 Tab：横移停止、角色继续下落；用 WASD 选择跳跃，Space 装备后顶部显示 `1/4` 和剩余 `10/12`，角色不响应这些按键。
3. Tab 关闭，A/D 恢复；再次 Tab 打开，装备与选择保留，Space 卸载，Esc 关闭。反复切换确认没有重复装卸。当前装备跳跃或冲刺仍不执行角色能力，这是后续阶段的工作。
4. 单独打开 AbilityEquipmentTest，确认菜单初始显示，Tab/Esc 同样有效，原布局、中文、鼠标选择与 WASD/Space 操作保持正常。

第二阶段只完成 UI 接入与输入隔离；角色能力查询连接见下方第三阶段。跳跃预算、冲刺 Duration 和具体能力执行均未在第二阶段实现。

## 玩家能力查询连接：第三阶段（2026-11-03）

主场景原 `Player` 已挂载 `GameJam2026.Abilities.PlayerAbilityController`；`GameSystems/GameplayAbilityCoordinator` 的 `Player Abilities` 字段绑定该组件。Coordinator 在 Start（全部 Awake 完成后）把原 `AbilityEquipmentController.Manager` 注入 Player。测试场景没有玩家，不添加占位角色；共享 Canvas、移动脚本、Rigidbody2D、Collider2D 和能力配置均保持原样，无需手动补接线。

角色组件只保存 Manager 引用，所有查询直接读取现有装备数据，不维护额外装备列表或能力布尔值，也不读取 UI 图标、选择状态。后续能力行为可调用：

- `IsBound`：是否已连接装备会话。
- `HasAbility` / `GetEquippedCount`：按 AbilityType 查询类型，或传入 AbilityData，复用管理器按稳定 AbilityId 匹配定义的规则。
- `TryGetEquippedAbility(type, out definition)`：返回实际装备顺序中的第一个匹配 AbilityData 引用，不复制数据、不叠加参数。多个不同定义的执行策略仍需在相应能力阶段确定。
- `AbilitiesChanged`：转发当前 Manager 的装备变化；换源或重新启用时通知一次刷新。同源重复绑定不重复订阅，禁用期间退订但直接查询仍读取当前真实数据。

菜单关闭及 Canvas 根临时禁用不解除装备绑定。原装备联动组件新增 `SessionEnded` 通知，仅在宿主销毁时结束会话；Coordinator 立即解除 Player 的旧源，避免普通 C# Manager 在 Unity 宿主销毁后残留。Coordinator 和 Player 销毁时均退订；场景重载仍创建空的新会话，不增加跨场景持久化或存档。AbilityManager 的数据结构、实例与容量规则没有修改，也没有新增 Manager 创建入口。

一次性 Editor 迁移工具只同步未来创建时的组件挂载和引用检查，本阶段没有重跑它、重新提取 Canvas 或重建场景。主场景仅改 Player 组件列表和 Coordinator 绑定并新增一个组件块，其余 59 个原序列化块字节不变。

### 本阶段实际验证

指定 Unity 2022.3.62f3c1 后台编译与完整回归最终 **64/64 通过**，进程退出码 0：原 57 项加新增查询连接 7 项（3 项 EditMode 查询／事件测试、4 项通过 EditMode 入口进入 Play Mode 的真实场景测试）。结果为 `GameJam2026/Logs/ability-stage3/regression-rerun-results.xml`，日志为 `regression-rerun.log`。覆盖装卸立即同步、重复实例最后一份卸载、同类型定义查询顺序、换源事件、禁用／重启、菜单关闭、UI 宿主禁用／销毁及场景重载。

首轮原 57 项通过，但新增测试 `.meta` 的 GUID 长度错误，被 Unity 忽略，不能算新增测试已执行；修正为 32 位 GUID 并检查所有 `.meta` 格式后，完整重跑确认发现并执行 64 项。Test Runner 的 runInBackground 副作用已恢复；后台许可客户端回退／令牌和无图形设备提示仍存在，未发现本次脚本编译错误或最终测试失败。未操作交互式 Console、未做本轮真实键盘／画面验收或 Windows 构建。

### 本阶段人工复核（待执行）

1. 打开 SampleScene 进入 Play Mode，选中 Player，在 PlayerAbilityController 组件菜单执行 `Debug/Log Equipped Abilities`；Console 应显示 `connected`，四类数量均为 0。
2. Tab 打开菜单，选中跳跃并按 Space 装备，关闭菜单后再次执行该日志命令，Jump 应为 1；卸载后立即为 0。对冲刺重复检查，Dash 对应为 1／0。日志直接执行当前查询，没有 Inspector 状态缓存。
3. 检查 Tab/Esc、WASD/Space 菜单操作和关闭后的 A/D 移动。当前按 Space 或 Shift 仍不执行角色跳跃／冲刺；第三阶段只完成读取连接。

第三阶段的读取连接保持原样；经用户批准后的实际跳跃接入见下方第四阶段。

## 实际跳跃接入：第四阶段（2026-11-03）

主场景 `Player` 已完成接线，无需重新创建角色或手动绑定资源：保留 `PlayerController2D`、`Rigidbody2D`、`BoxCollider2D` 和第三阶段的 `PlayerAbilityController`，新增 `PlayerJump`。测试场景没有玩家，继续复用原 Canvas，不添加占位角色。

### 职责与参数

- `PlayerController2D` 新增 Space Button Action：按键回调只记录请求，下一次 FixedUpdate 消费一次；仍由这个控制器统一写入刚体速度，Move Speed 保持 5。菜单屏蔽时同时禁用 Move/Jump Action、清除待处理起跳和水平速度，保留竖直速度。
- `PlayerJump.CalculateVelocity` 只计算本次速度、落地状态和行程余额，不写 Rigidbody2D。每次请求直接查询现有 `PlayerAbilityController` → 原 Manager；未装备、未绑定、空中、无有效向下重力或配置高度为零时不施加起跳。
- 跳跃读取实际装备的 `JumpAbilityData.VerticalHeight` 和 `MaxHorizontalDistance`。当前资源仍为高度 2、水平累计预算 2、消耗 2；未修改能力资源或数据结构。沿用已有查询顺序，多个实例不叠加参数，最后一个实例卸载后不能再起跳；已经产生的物理运动不被回滚。
- 已确认 16×16 像素 tile、PPU 16，因此 1 格为 1 世界单位。起跳速度为 `sqrt(2 × 实际重力加速度 × VerticalHeight)`，重力使用 `Physics2D.gravity.y` 和当前 Rigidbody2D.gravityScale。离散物理积分存在步长误差，不以视觉逐像素等于 2 作为验收。
- 落地使用 Rigidbody2D.GetContacts 的实际接触，忽略 Trigger，并要求接触法线 y ≥ Inspector 的 Minimum Ground Normal Y（0.65）。上升时拒绝判定落地，避免起跳后的残留接触开放二次跳跃；墙面和顶面不算地面。无需新增 Ground Layer 或修改碰撞矩阵。
- 普通跳跃横移速度沿用 5；每个 FixedUpdate 用上一物理步实际 `abs(body.position.x - previousX)` 扣预算。反向同样扣、撞墙零位移不扣，末步通过 `余额 / fixedDeltaTime` 限速，耗尽即停止普通横移。落地结束本次预算，下一次有效起跳重新读取配置；UI 开关、卸载重装不能补充余额。仅主动跳跃启用此预算，走落平台保留原有行为。
- 未添加长按自动跳、空中连跳、跳跃缓冲、冲刺或存档；第五阶段冲刺接入时需区分普通移动和冲刺的物理步位移，避免消耗普通跳跃预算。

### 编辑器验收（待人工操作）

1. 打开 `Assets/Scenes/SampleScene.unity`，选中 `Player`：确认上述组件齐全，Move Speed 为 5、Minimum Ground Normal Y 为 0.65；Rigidbody2D 的 Dynamic、Gravity Scale 1、Continuous 和原 BoxCollider 保持原样。进入 Play，等待角色落到原平台。
2. 空装备时 A/D 移动、松键停止；Space 不跳，Shift 不冲刺。
3. Tab 打开菜单，WASD 选跳跃、Space 装备，Esc 关闭；Space 起跳，连续按／按住不能空中连跳或自动落地连跳。落地后重新按 Space 可以再次起跳。
4. 跳起时向右再向左，累计普通横移最多 2 格，反向不会退还距离；用完后水平停止但继续下落，落地后下一跳恢复预算。不要用起落点净位移代替累计路程。
5. 腾空后 Tab 打开：水平停止、重力继续；菜单 Space 只装卸，关闭时按住的 Space 不补发跳跃。菜单中卸载跳跃后，落地再按 Space 应无效。
6. 检查 Tab＋Space、Esc＋Space 同帧没有意外起跳或装卸；检查墙体阻挡和原测试场景的 WASD/Space/Tab/Esc。重复装备实例仍仅由原 Manager 管理，本阶段没有新增玩家重复装备操作。

### 本阶段实际验证

- 指定 Unity 后台编译及完整回归最终 **75/75 通过、退出码 0**：原有 64 项＋新增跳跃集成测试 11 项。结果为 `GameJam2026/Logs/ability-stage4/regression-final-results.xml` 和 `regression-final.log`，覆盖实际刚体运动、配置克隆、反向累计路程、末步截断、碰墙、落地恢复、卸载与菜单隔离。
- 首轮 64/75、定向复测 10/11，失败来自测试准备阶段使用了 Test Framework 1.1.33 不支持的普通 yield。改为逐帧等待后全量通过；未修改游戏逻辑来绕过测试，初次失败记录保留在同目录。
- 主场景 Windows x64 构建成功，退出码 0，见 `GameJam2026/Logs/ability-stage4/windows-build.log`；产物为 `GameJam2026/Builds/Windows/PlayerJumpStage4/20261103-validation/PlayerJumpStage4.exe`。使用原构建场景列表，没有新增打包工具或修改项目设置。
- 未启动本次 EXE、未操作交互式 Console、未验收画面／真实键盘和手感。后台仍有许可客户端握手回退／令牌和无图形设备提示。本次启动前的未提交内容均保留；Test Runner 的 runInBackground 和构建追加的 URP 序列化字段已核对恢复，能力资源、Manager、共享 Canvas、测试场景、Packages 和 ProjectSettings 没有本阶段差异。

第四阶段的跳跃已保留。随后经用户批准接入的冲刺 Duration、位移来源区分和 Shift／Space 顺序见下方第五阶段。

### 技术学习要点

| 层次 | 技术与本项目职责 | 选择原因与学习重点 |
| --- | --- | --- |
| 基础，需掌握 | MonoBehaviour、GetComponent、Rigidbody2D.velocity、Collider2D | 复用原组件和物理碰撞；阅读 PlayerController2D 的 Awake / FixedUpdate，掌握速度怎样实际作用于角色 |
| 核心，需独立掌握 | InputAction.performed 回调、bool 请求、Update / FixedUpdate 分工 | 短按不能只在物理帧读取；请求只消费一次，菜单切换清除请求，避免漏键、连跳和重复执行 |
| 核心，可深入练习 | 接触法线、ContactFilter2D、重力公式、实际位移累计及末步限速 | 保持现有 Rigidbody2D 方案；阅读 PlayerJump.CalculateVelocity / HasGroundContact，掌握向量法线、预算与物理步的关系 |
| 暂时了解即可 | ScriptableObject 配置、只读属性、RequireComponent / DisallowMultipleComponent | 继续从已有能力资源和实际装备查询取值；属性便于观察测试，组件约束避免错误挂载，不需要引入框架 |

## 基础冲刺接入：第五阶段（2026-11-03）

### 职责、配置与场景接线

- 主场景原 `Player` 新增 `PlayerDash`，`Collision Skin = 0.01`，无需手动绑定；保留原 Rigidbody2D 的 Dynamic、Gravity Scale 1、Drag 0、Continuous 和 BoxCollider2D。没有修改 Layer、物理矩阵、输入系统或构建场景列表。原测试场景没有玩家，仍共享原能力 Canvas。
- `DashAbilityData` 新增可编辑的 Duration，默认资源为 Distance 4、Duration 0.2 秒、消耗 4；16×16 像素 tile / PPU 16 对应 1 格 = 1 世界单位，所以速度由 `Distance / Duration` 得 20 世界单位／秒。原 UI 使用 GetParameters 自动显示两项参数，没有重新制作 UI。
- `PlayerDash` 从实际装备查询读取起步参数，只保存本次动作所需的方向、速度和剩余时间，不复制装备列表。Duration 必须有限且大于零；零距离不发动，所需速度超过当前 `Physics2D.maxTranslationSpeed` 会拒绝并提示调整能力资源，不静默缩短距离，也不改 Project Settings。
- `PlayerController2D` 继续统一写 Rigidbody2D.velocity。普通速度与预算先计算，再由有效冲刺覆盖水平分量；竖直速度与重力保持原样。冲刺允许空中使用、没有冷却；长按不自动重复，进行中再次按 Shift 不重置时间，结束后新的按键可再次发动。
- 冲刺方向优先当前 A/D 输入，否则使用最后水平朝向，初始朝右。不增加翻转动画或视觉效果。
- `CompletePreviousPhysicsStep` 先结算已经模拟完的冲刺时长，再决定本步动作；非整物理步持续时间的末步只执行剩余行程。固定步长为 0.02 秒，动作结束判定在完整物理步边界，实际时长存在最多一个固定步的量化误差。
- `Rigidbody2D.Cast` 用原刚体形状检查本步前方实体，忽略 Trigger，并按碰撞层矩阵筛选；朝向前进反方向的侧面法线才作为墙。速度限制到墙前允许距离，并保留 Continuous 检测。碰墙结束当前冲刺，不做 Transform 瞬移、穿透、攻击判定、无敌帧、粒子或复杂动画。
- `PlayerJump.CalculateVelocity` 新增可选 `previousStepWasDash`：普通跳跃预算只结算普通运动物理步的实际横移，冲刺首末步以及取消后的最后位移都排除。预算耗尽仅限制普通空中横移，不覆盖冲刺速度；目前普通移动没有惯性，仍沿用原即时停止方式。落地恢复地面移动，下一跳重新获得预算。
- Shift＋Space 同批输入优先发动冲刺，Space 请求只保留到该冲刺结束，届时重新检查地面与 Jump 装备，条件不足即丢弃。若没有可用冲刺，Space 直接走原跳跃判断。冲刺途中按 Space 也采用相同的一次结束判断；不允许冲刺中立即起跳。
- UI 打开禁用 Move／Jump／Dash Action，清除新请求和排队跳跃，取消冲刺、停止水平速度并保留竖直运动；不暂停世界。卸载最后一个 Dash 实例或装备会话解除时，原装备事件使冲刺立即取消；卸载一个但仍有同类实例则继续有效。禁用组件也会取消并退订。

### 人工验收（待执行）

1. 打开 `Assets/Scenes/SampleScene.unity`，确认 Player 上有 PlayerDash。Project 中选择 `Assets/Data/Abilities/Dash_Test.asset`，核对 Distance 4、Duration 0.2、Slot Cost 4。进入 Play，空装备下 A/D 正常，Space 和 Left Shift 不发动能力。
2. Tab 打开，WASD 选择冲刺、Space 装备，Esc 关闭。站在地板左侧向右冲刺，无遮挡时前进 4 格；向左输入和松开方向后再次冲刺，检查当前输入优先及最后朝向。碰墙应提前停止，结束后 A/D 立即恢复。
3. 同时装备跳跃和冲刺：跳起后冲刺，确认仍受重力影响。普通横移用完 2 格后仍能冲刺，冲刺不补充也不消耗这份预算，落地后恢复正常移动。
4. 地板上从左侧按 Shift＋Space，先冲刺再跳；从平台边缘冲到空中时，结束后不能追加普通空中跳。无冲刺装备时 Space 仍能正常跳。
5. 冲刺期间 Tab 打开应立即停止横移并继续下落；菜单 Shift 不冲刺、Space 只装卸。Esc 关闭时仍按住的 Shift／Space 不应补发动作。反复开关后检查装备状态、容量和输入都正常。
6. 冲刺中卸载最后一个冲刺实例应立即失效，不残留速度或延迟跳跃；重复实例的管理器路径由自动测试覆盖，本阶段不新增重复装备按键。

### 技术学习要点

| 层次 | 技术与职责 | 选择原因与建议阅读 |
| --- | --- | --- |
| 基础，需掌握 | ScriptableObject 序列化字段、只读属性、距离／时间计算 | 在原数据资源增加 Duration，避免把速度写死；阅读 DashAbilityData |
| 核心，需独立掌握 | FixedUpdate 中的运动优先级、上一物理步标记、剩余时长 | 一个控制器统一写速度，避免冲刺被普通横移覆盖或误扣预算；阅读 PlayerController2D.FixedUpdate 和 PlayerDash.CompletePreviousPhysicsStep |
| 核心，值得深入 | Rigidbody2D.Cast、ContactFilter2D、RaycastHit2D.normal、末步限速 | 复用实体碰撞与当前刚体方案；阅读 PlayerDash.TryCalculateVelocity，理解法线如何区分墙和地面 |
| 暂时了解即可 | C# event / 订阅退订、InputAction 回调、配置合法性校验 | 复用原装备事件取消失效能力、清除菜单请求；本次无需引入状态机框架或新的输入系统 |

### 本阶段实际验证

- 指定 Unity 2022.3 后台编译成功，首轮完整回归 **97/97 通过，退出码 0**。包括原有 75 项、Duration 配置验证 8 项、主场景冲刺集成 14 项；结果与日志位于 `GameJam2026/Logs/ability-stage5/regression-results.xml`、`regression.log`。测试使用真实场景、刚体物理与虚拟键盘，确认实际距离、碰墙、预算、延迟跳跃、装卸取消和菜单输入隔离。
- 主场景 Windows x64 构建成功，退出码 0，见 `GameJam2026/Logs/ability-stage5/windows-build.log`。产物为 `GameJam2026/Builds/Windows/PlayerDashStage5/20261103-validation/PlayerDashStage5.exe`，未启动该 EXE；真实键盘、画面、手感与交互式 Console 待人工复核。
- 后台仍有既有许可握手回退／令牌和无图形设备提示；过快配置测试中的警告属于预期。Test Runner 的 runInBackground 和构建追加的 URP 字段已核对恢复，原有未提交内容均保留，没有 Git 暂存／提交／推送。

第五阶段到此停止，等待审核，不自动进入后续阶段。

## UI 与游戏输入隔离：第六阶段（2026-11-03）

本阶段复核第二、五阶段已接入的隔离链路，仅补齐菜单组件单独失效时的退出处理与边界测试。没有新增输入系统、Manager、能力或场景对象；两个场景仍共用原 `AbilityEquipmentCanvas.prefab`。现有 `GameSystems/GameplayAbilityCoordinator` 与 `AbilityCanvas` 引用已接好，无需修改 Inspector 或重新运行迁移工具。

### 输入状态与生命周期

- `GameplayAbilityCoordinator` 保持 `Gameplay / AbilityMenu` 两种状态。Tab 切换、Esc 关闭；同一输入更新先处理模式切换，再让 `AbilityInput` 派发排队的 WASD/Space 命令，Esc 优先。
- 打开菜单时调用原 `PlayerController2D.SetGameplayInputEnabled(false)`：禁用 Move/Jump/Dash Actions，清除新请求与冲刺后跳跃请求，取消冲刺、停止横移，保留竖直速度。不修改 `Time.timeScale`，角色仍可下落。
- 关闭时禁用 `AbilityInput` 并清空队列，隐藏 Canvas、禁用 GraphicRaycaster，再恢复角色输入。Space/Shift 的 Button Action 不重播跨模式按住的旧按键；A/D 按现有 Value Action 恢复移动。游戏中 Space/Left Shift 与菜单中的 Space 分别由各自输入状态处理。
- 新增 `CanOpenMenu` 检查 UI、装备宿主与实际 Manager 是否可用。菜单打开期间若单独禁用 UI、Input、Canvas 或装备宿主，会在下一次输入更新关闭菜单并解除角色输入门禁；恢复组件后可以正常 Tab 重开。正常关闭会禁用 Canvas/Input，因此开启前不会误把这两项的 disabled 状态当作不可重开。
- 关闭菜单不销毁共享 Canvas、宿主或装备数据。仍只有现有 `AbilityEquipmentController.Awake` 创建本场景的 Manager；Coordinator 只连接并查询它。禁用/启用不重建会话，销毁/重载场景仍按原场景生命周期结束会话，本阶段不增加跨场景存档。
- 两个场景的 EventSystem 继续关闭自动导航，槽位按钮 `Navigation.None`；沿用原 WASD 选择与 Space 装卸，没有新增 Ctrl/Shift 装备操作。

### 人工验收（待执行）

1. 打开 `Assets/Scenes/SampleScene.unity`，进入 Play Mode 并点击 Game 视图。Tab 打开后用 WASD、Space 装上跳跃与冲刺；关闭再打开应保留装备与数量。
2. 菜单打开时按住 A/D、Space、Left Shift：WASD 只操作 UI，Space 只装卸，角色不横移、不起跳、不冲刺；在空中打开菜单时仍下落。Esc 关闭后，松开再按游戏按键应恢复移动/跳跃/冲刺。
3. 快速组合 Tab＋Space/Shift、Esc＋Space/Shift，并按住 Space/Shift 跨菜单切换：不应出现意外装卸、关闭后补发跳跃/冲刺或冲刺后延迟起跳。重新按下的游戏按键仍有效。
4. Play Mode 内打开菜单，在 Hierarchy 选 `AbilityCanvas`，分别临时禁用并恢复 `AbilityUI`、`AbilityInput`、`Canvas`、`AbilityEquipmentController`（每次只测一个，恢复后再 Tab 打开）。预期菜单安全关闭、A/D 恢复，重开后装备还在。再临时禁用/启用 `GameSystems` 上的 Coordinator，验证相同行为及 Space 一次只装卸一次。退出 Play Mode，不保存这些临时禁用状态。
5. 打开 `Assets/Scenes/AbilityEquipmentTest.unity` 重复正常 Tab/Esc/WASD/Space 操作；该场景初始打开菜单、没有玩家，仍复用同一个 UI Prefab。检查两个场景的 Console 是否出现异常，并记录真实键盘/画面结果。

### 技术学习要点

- **基础与核心**：`InputAction.Enable/Disable` 控制输入是否被角色接收；`Canvas.enabled` 只控制显示，不能替代输入门禁。最值得先读 `GameplayAbilityCoordinator.SetMenuOpen` 与 `PlayerController2D.SetGameplayInputEnabled`，理解为何菜单打开后重力仍工作。
- **值得深入**：`InputSystem.onAfterUpdate` 在这一批动作回调结束后统一处理模式与命令队列，避免依赖 Tab/Space 回调碰巧的先后顺序；`OnEnable/OnDisable` 成对订阅/退订，防止反复开关后执行多次。`CanOpenMenu` 使用 C# 短路判断，失效引用不会继续被解引用。
- **暂时了解即可**：虚拟 Keyboard、`QueueStateEvent` 与 EditMode 入口切入 Play Mode 的测试工具。测试用 `Time.fixedTime` 确认真实物理步已推进并 `yield null` 等待，兼容当前 Test Framework；无需为此引入新的运行时框架。

### 本阶段实际验证

- 指定 Unity 2022.3.62f3c1 后台编译及完整回归 **100/100 通过，退出码 0**：上一阶段 97 项与新增 3 项均实际执行；菜单集成共 9 项。通过 EditMode 入口进入 Play Mode，验证真实场景、Rigidbody2D 与虚拟键盘，结果见 `GameJam2026/Logs/ability-stage6/regression-results.xml`、`regression.log` 和 `test-summary.json`。
- 新用例实际覆盖四种必需组件分别禁用后恢复、Manager/装备实例保留、单次输入更新内多条键盘事件、Coordinator 三轮启停后的单次装卸；原跳跃 11 项、冲刺 14 项、装备与 UI 回归全部通过。修复前的单项 `diagnostic-results.xml` 已复现禁用 AbilityUI 后 Canvas 仍开启的断言失败。
- 初期两轮定向测试各 7/9，另 2 项遇当前 Test Framework 的普通协程等待限制；改用实际固定时间推进的 `yield null` 等待后，最终全量没有此错误。失败与复现记录保留在同一 Logs 目录，没有屏蔽错误日志来通过测试。
- 未操作交互式 Console；最终后台没有脚本编译错误、意外脚本异常或测试失败，仍有既有许可握手回退/令牌和 `-nographics` 光照探针提示。未做真实键盘、鼠标、画面或手感验收；本阶段未重建/启动 Windows 包，第五阶段包不包含本次兜底修复。Test Runner 的 `runInBackground` 已核对并恢复，项目设置、Packages、场景和能力配置均无本阶段差异。

第六阶段交付后停止，第七阶段完整验收与构建需另行批准。

## 完整验收与交付：第七阶段（2026-11-03）

代码与场景接线复核、自动回归、Windows 构建和后台启动检查已完成；真实键盘/鼠标、画面/手感、切换窗口焦点与手动退出仍待人工验收。使用原有主场景、共享 Canvas 和装备会话，所有 Inspector 绑定已完成，本阶段无需添加组件或修改项目设置。

### 本轮修改与调用关系

- `Assets/Tests/AbilityInputTests/GameplayAbilityMenuIntegrationTests.cs` 新增一条完整流程测试：从空装备出发，实际发送 Tab、WASD、Space、Esc、Left Shift，完成装备、能力使用、卸载和场景重载；没有直接修改 Manager 或选择状态来绕过 UI。新增等待均有截止时间。
- `Assets/Data/Abilities/Jump_Test.asset` 仅修正已过期的“仅保存参数，不执行跳跃”说明；跳高 2、普通横移累计预算 2、消耗 2 保持原值。README 与 ENV_CHECK 同步交付记录。
- 最短阅读链路：`GameplayAbilityCoordinator.SetMenuOpen/AfterInputUpdate` 管输入状态和命令顺序 → 原 `AbilityEquipmentController` 调用 `AbilityManager` 装卸 → `PlayerAbilityController` 查询实际装备 → `PlayerController2D.FixedUpdate` 统一协调 `PlayerJump/PlayerDash` 并写刚体速度。UI 展示、装备数据和能力执行各自保留原职责。
- 当前装备会话属于场景：打开/关闭菜单和临时禁用组件均保留装备；重载场景建立新空会话。重复实例由原 Manager 管理与测试，玩家 UI 仍只有 Space 装卸入口。

### 已执行的验收

| 验收项 | 实际结果与证据 |
| --- | --- |
| 代码与场景复核 | 本次范围未发现确定 P1/P2；73 个资源 GUID 合法且唯一，两个场景和共享 Prefab 无重复本地 fileID。见 `Logs/ability-stage7/review-summary.json`、`static-integration-report.json` |
| 编译与完整自动测试 | **101/101 通过，退出码 0**。原 100 项及新完整流程均实际执行；经 EditMode 入口进入 Play Mode，使用真实场景/刚体和虚拟键盘。见 `Logs/ability-stage7/regression-results.xml`、`regression.log`、`test-summary.json` |
| Windows x64 构建 | **成功，退出码 0**，原构建列表仍只有 SampleScene。包中包含默认角色程序集及四个能力运行时程序集，未包含 Editor/测试程序集；见 `windows-build.log`、`build-summary.json` |
| Windows 后台启动 | 强制 Direct3D 11，实际创建 NVIDIA 图形设备，持续运行 15 秒，日志未见脚本异常、错误或 Shader 错误；之后由工具结束本次进程。见 `windows-player-d3d11.log`、`windows-player-smoke.json`、`startup-validation-summary.json` |
| 验证边界 | 后台启动没有模拟真实键盘/鼠标，不验证屏幕绘制、手感、焦点恢复或手动退出；未操作交互式 Editor Console。Editor 日志仍有既有许可握手回退/令牌与 `-nographics` 光照探针提示 |

上述日志均位于 Unity 工程的 `GameJam2026/Logs/ability-stage7/`（从仓库根目录查看）。Test Runner 的 runInBackground 与构建追加的 32 个 URP 字段已逐项核对并恢复；没有修改场景、Prefab、运行时 C#、能力数值参数、Packages 或最终 ProjectSettings，没有暂存、提交或推送 Git。阶段前快照与最终范围检查见同目录 `preflight-hashes.json`、`final-scope-audit.json`。

### Windows 交付产物与人工验收（待执行）

产物为仓库内 `GameJam2026/Builds/Windows/AbilityIntegrationStage7/20261103-174533-703/AbilityIntegrationStage7.exe`。分发时保留整个 `20261103-174533-703` 目录及其中的 Data、MonoBleedingEdge、DLL 等文件；不能只复制 EXE。Builds 和 Logs 已被 Git 忽略。

从仓库根目录用 PowerShell 启动，关闭第一轮程序后再运行第二个分辨率：

```powershell
$stage7GameExe = Join-Path $PWD 'GameJam2026/Builds/Windows/AbilityIntegrationStage7/20261103-174533-703/AbilityIntegrationStage7.exe'
& $stage7GameExe -screen-fullscreen 0 -screen-width 1280 -screen-height 720
& $stage7GameExe -screen-fullscreen 0 -screen-width 1920 -screen-height 1080
```

| 人工检查 | 操作与通过信号 |
| --- | --- |
| 空装备与移动 | 进入后等待落地，A/D 正常、松开停止、同时按抵消；Space/Left Shift 不执行能力 |
| 装备与跳跃 | Tab 打开，D 选择跳跃，Space 装备，Esc 关闭；Space 可跳，空中连续按不连跳，落地后重新按可跳。空中向右再向左累计普通横移最多 2 格；落地后下一跳恢复预算 |
| 冲刺与碰墙 | Tab 后 D 选择冲刺并 Space 装备，关闭后 Left Shift 冲刺；输入方向优先、否则沿最后方向，空中可用，碰墙停止。无阻挡时距离约 4 格、时长 0.2 秒；冲刺不消耗普通跳跃横移预算 |
| UI 隔离与数据保留 | 在空中 Tab，横移/冲刺停止而继续下落。菜单中的 WASD/Space 不操控背景角色；Tab/Esc 关闭再打开仍保留装备。按住 Space/Shift 跨模式不补发动作，松开重按正常 |
| 卸载和组合键 | 用 Space 卸掉对应能力后关闭菜单，该能力立即不可再触发。Shift＋Space 先冲刺，结束时只有仍装备跳跃且落地才跳；冲刺空中结束时不自动留到落地执行 |
| 布局、焦点和退出 | 两个分辨率下中文和详情完整、槽位不重叠；切到其他窗口再返回，确认输入正常且无重复装卸；用窗口关闭按钮退出，确认程序结束 |
| Editor 独立测试场景 | 用 Unity 打开 AbilityEquipmentTest，初始显示同一套 UI，WASD/Space/Tab/Esc 正常。检查 Console；如需组件启停边界，按第六阶段步骤临时操作，退出 Play Mode 不保存禁用状态 |

人工反馈请记录分辨率、具体操作、预期/实际表现及 Console/Player.log 错误原文。当前没有交互式验收通过记录；重复实例/容量上限由自动测试覆盖，不新增玩家重复装备快捷键。

### 技术学习报告

- **基础**：Scene 负责本场景对象生命周期，Prefab 复用同一套 UI，ScriptableObject 保存配置与说明。本轮更新资源说明而不修改数值或执行代码，让界面文字与已有玩法保持一致。
- **核心，值得独立掌握**：用真实输入驱动场景的集成测试。阅读新测试 `MainScene_KeyboardEquipmentPlayUnequipmentAndReload_KeepOneConsistentSession`，看键盘事件如何经 UI、Manager、Reader 到达 Rigidbody2D；比单独验证某个查询函数更能发现接线或输入状态之间的问题。
- **值得深入**：`InputSystem.QueueStateEvent` 的虚拟设备、固定物理时间与带截止的协程等待，以及程序集的 Editor/运行时边界。测试明确等待实际状态变化，构建再核对 Managed 输出，避免把 Editor 能运行直接当作 Player 可交付。
- **暂时了解即可**：Unity 命令行的 `-batchmode/-nographics`、Windows 的 `-force-d3d11` 和进程观察。它们让编译、构建、启动可重复记录，但启动日志不能替代真实按键、画面、焦点和退出验收。本轮没有为验证引入新的运行时框架或依赖。

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

当前主场景 Windows 构建与后台启动证据见“完整验收与交付：第七阶段”；真实画面、输入与手动退出仍待人工验收。独立能力装备包的历史构建与启动证据见“能力装备最终验证阶段”。
