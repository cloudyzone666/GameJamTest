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

以上为操作步骤，不代表构建或启动已经验证通过。
