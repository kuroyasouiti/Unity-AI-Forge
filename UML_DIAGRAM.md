# Unity-AI-Forge UML図（デザインパターン版）

## 1. 全体アーキテクチャ概要

```mermaid
graph TB
    subgraph "MCP Server (Python)"
        PY[main.py]
        TOOLS[register_tools.py]
        BRIDGE_CLIENT[WebSocket Client]
    end

    subgraph "MCP Bridge (C# Editor)"
        SERVICE[McpBridgeService<br/>«Singleton + Facade»]
        PROCESSOR[McpCommandProcessor<br/>«Mediator»]
        FACTORY[CommandHandlerFactory<br/>«Factory»]
        HANDLERS[Command Handlers<br/>«Strategy + Template Method»]
    end

    subgraph "GameKit Runtime"
        COMPONENTS[Runtime Components<br/>«Registry»]
        MANAGERS[Manager Components<br/>«Abstract Factory»]
        ASSETS[ScriptableObjects<br/>«Prototype»]
    end

    PY --> TOOLS
    TOOLS --> BRIDGE_CLIENT
    BRIDGE_CLIENT <-->|WebSocket| SERVICE
    SERVICE --> PROCESSOR
    PROCESSOR --> FACTORY
    FACTORY --> HANDLERS
    HANDLERS --> COMPONENTS
    HANDLERS --> MANAGERS
    HANDLERS --> ASSETS
```

---

## 2. Singleton + Facade Pattern: McpBridgeService

WebSocket通信の複雑さを隠蔽し、単一のアクセスポイントを提供

```mermaid
classDiagram
    direction TB

    class McpBridgeService {
        <<Singleton + Facade>>
        -static McpBridgeService _instance$
        -TcpListener _listener
        -TcpClient _client
        -WebSocket _webSocket
        -CancellationTokenSource _cts
        +static McpConnectionState State$
        +static bool IsConnected$
        +static string SessionId$
        +static event Action~McpConnectionState~ StateChanged$
        +static Connect()$
        +static Disconnect()$
        +static Send(Dictionary message)$
        -StartListener()
        -HandleClientAsync()
        -ReceiveLoopAsync()
        -SendInternal()
    }

    class McpConnectionState {
        <<enumeration>>
        Disconnected
        Connecting
        Connected
    }

    class ExternalClient {
        <<Client>>
    }

    note for McpBridgeService "Singleton: static instanceで単一アクセス\nFacade: WebSocket/TCP複雑性を隠蔽"

    ExternalClient --> McpBridgeService : uses simplified API
    McpBridgeService --> McpConnectionState
```

---

## 3. Factory Pattern: CommandHandlerFactory

ハンドラーの生成・登録・取得を一元管理

```mermaid
classDiagram
    direction TB

    class CommandHandlerFactory {
        <<Factory>>
        -static Dictionary~string, ICommandHandler~ _handlers$
        -static bool _initialized$
        +static Initialize()$
        +static Register(string toolName, ICommandHandler handler)$
        +static GetHandler(string toolName)$ ICommandHandler
        +static TryGetHandler(string toolName, out ICommandHandler)$ bool
        +static IsRegistered(string toolName)$ bool
        +static Clear()$
        +static GetRegisteredToolNames()$ IEnumerable~string~
    }

    class CommandHandlerInitializer {
        <<Factory Helper>>
        -static bool _initialized$
        +static InitializeHandlers()$
        -static RegisterPhase3Handlers()$
        -static RegisterPhase5Handlers()$
        -static RegisterPhase7Handlers()$
        -static RegisterMidLevelHandlers()$
        -static RegisterGameKitHandlers()$
    }

    class ICommandHandler {
        <<interface>>
        +Execute(Dictionary payload)* object
        +SupportedOperations* IEnumerable~string~
        +Category* string
    }

    class ConcreteHandlerA {
        <<Product>>
    }
    class ConcreteHandlerB {
        <<Product>>
    }

    note for CommandHandlerFactory "Factory Pattern:\n文字列キーでハンドラーを登録・取得\n実行時にハンドラーを動的に解決"

    CommandHandlerInitializer --> CommandHandlerFactory : registers
    CommandHandlerFactory --> ICommandHandler : creates
    ICommandHandler <|.. ConcreteHandlerA
    ICommandHandler <|.. ConcreteHandlerB
```

---

## 4. Strategy Pattern: ICommandHandler

操作の実行方法を交換可能なアルゴリズムとしてカプセル化

```mermaid
classDiagram
    direction TB

    class McpCommandProcessor {
        <<Context>>
        +static Execute(McpIncomingCommand command)$ object
        -static GetHandler(string toolName)$ ICommandHandler
    }

    class ICommandHandler {
        <<Strategy Interface>>
        +Execute(Dictionary payload)* object
        +SupportedOperations* IEnumerable~string~
        +Category* string
        +Version* string
    }

    class SceneCommandHandler {
        <<Concrete Strategy>>
        +Execute(Dictionary payload) object
        +SupportedOperations: ["create", "load", "save"...]
        +Category: "scene"
    }

    class GameObjectCommandHandler {
        <<Concrete Strategy>>
        +Execute(Dictionary payload) object
        +SupportedOperations: ["create", "find", "modify"...]
        +Category: "gameobject"
    }

    class ComponentCommandHandler {
        <<Concrete Strategy>>
        +Execute(Dictionary payload) object
        +SupportedOperations: ["add", "configure", "remove"]
        +Category: "component"
    }

    class GameKitActorHandler {
        <<Concrete Strategy>>
        +Execute(Dictionary payload) object
        +SupportedOperations: ["setup_actor"]
        +Category: "gamekit"
    }

    note for ICommandHandler "Strategy Pattern:\n各ハンドラーが異なるコマンド処理戦略を実装\nContextは実行時に適切な戦略を選択"

    McpCommandProcessor --> ICommandHandler : uses strategy
    ICommandHandler <|.. SceneCommandHandler
    ICommandHandler <|.. GameObjectCommandHandler
    ICommandHandler <|.. ComponentCommandHandler
    ICommandHandler <|.. GameKitActorHandler
```

---

## 5. Template Method Pattern: BaseCommandHandler

アルゴリズムの骨格を定義し、サブクラスで具体的な処理を実装

```mermaid
classDiagram
    direction TB

    class BaseCommandHandler {
        <<Abstract Class - Template Method>>
        #IPayloadValidator Validator
        #IGameObjectResolver GameObjectResolver
        #IAssetResolver AssetResolver
        #ITypeResolver TypeResolver
        +Execute(Dictionary payload) object
        #ValidatePayload(Dictionary payload)
        +abstract ExecuteOperation(string op, Dictionary payload)* object
        +abstract SupportedOperations* IEnumerable~string~
        +abstract Category* string
        #RequiresCompilationWait(string op) bool
        #WaitForCompilationAfterOperation(string op) Dictionary
        #CreateSuccessResponse(params) Dictionary
        #CreateFailureResponse(string error) Dictionary
        #GetOperation(Dictionary payload) string
        #ResolveGameObject(string path) GameObject
    }

    class SceneCommandHandler {
        <<Concrete Class>>
        +ExecuteOperation(string op, Dictionary payload) object
        +SupportedOperations: ["create", "load", "save"...]
        +Category: "scene"
        -CreateScene(Dictionary payload) object
        -LoadScene(Dictionary payload) object
        -SaveScene(Dictionary payload) object
    }

    class GameObjectCommandHandler {
        <<Concrete Class>>
        +ExecuteOperation(string op, Dictionary payload) object
        +SupportedOperations: ["create", "find", "modify"...]
        +Category: "gameobject"
        -CreateGameObject(Dictionary payload) object
        -FindGameObject(Dictionary payload) object
    }

    class ComponentCommandHandler {
        <<Concrete Class>>
        +ExecuteOperation(string op, Dictionary payload) object
        +SupportedOperations: ["add", "configure", "remove"]
        +Category: "component"
        -AddComponent(Dictionary payload) object
        -ConfigureComponent(Dictionary payload) object
    }

    note for BaseCommandHandler "Template Method Pattern:\nExecute()がアルゴリズムの骨格を定義\n1. ValidatePayload()\n2. ExecuteOperation() ← abstract\n3. WaitForCompilationAfterOperation()\nサブクラスはExecuteOperation()のみ実装"

    BaseCommandHandler <|-- SceneCommandHandler
    BaseCommandHandler <|-- GameObjectCommandHandler
    BaseCommandHandler <|-- ComponentCommandHandler
```

**Template Method 実行フロー：**

```mermaid
sequenceDiagram
    participant Client
    participant Base as BaseCommandHandler
    participant Concrete as ConcreteHandler

    Client->>Base: Execute(payload)
    Base->>Base: ValidatePayload(payload)
    Base->>Concrete: ExecuteOperation(op, payload)
    Note right of Concrete: サブクラスで実装
    Concrete-->>Base: result
    Base->>Base: RequiresCompilationWait(op)?
    alt requires wait
        Base->>Base: WaitForCompilationAfterOperation(op)
    end
    Base-->>Client: response
```

---

## 6. Observer Pattern: UnityEvent System

イベント発生時に購読者へ自動通知

```mermaid
classDiagram
    direction TB

    class GameKitHealth {
        <<Subject (Observable)>>
        +string healthId
        +float maxHealth
        +float currentHealth
        +UnityEvent~float, float~ OnHealthChanged
        +UnityEvent~float~ OnDamaged
        +UnityEvent~float~ OnHealed
        +UnityEvent OnDeath
        +UnityEvent OnRespawn
        +TakeDamage(float amount)
        +Heal(float amount)
        -NotifyHealthChanged()
    }

    class UnityEvent~T~ {
        <<Observer Infrastructure>>
        +AddListener(Action~T~ callback)
        +RemoveListener(Action~T~ callback)
        +Invoke(T value)
    }

    class UIHealthBar {
        <<Observer>>
        +UpdateHealthDisplay(float current, float max)
    }

    class AudioManager {
        <<Observer>>
        +PlayDamageSound(float damage)
    }

    class GameManager {
        <<Observer>>
        +OnPlayerDeath()
    }

    note for GameKitHealth "Observer Pattern:\nUnityEventで疎結合な通知を実現\nSubjectは購読者を知らない"

    GameKitHealth --> UnityEvent~T~ : contains
    UIHealthBar ..> GameKitHealth : subscribes OnHealthChanged
    AudioManager ..> GameKitHealth : subscribes OnDamaged
    GameManager ..> GameKitHealth : subscribes OnDeath
```

---

## 7. Registry Pattern: Component Lookup

IDによるコンポーネントの高速検索を提供

```mermaid
classDiagram
    direction TB

    class GameKitHealth {
        <<Registry>>
        -static Dictionary~string, GameKitHealth~ _registry$
        +string healthId
        +static FindById(string id)$ GameKitHealth
        +static Register(GameKitHealth component)$
        +static Unregister(string id)$
        -OnEnable()
        -OnDisable()
    }

    class GameKitInventory {
        <<Registry>>
        -static Dictionary~string, GameKitInventory~ _registry$
        +string inventoryId
        +static FindById(string id)$ GameKitInventory
        +static Register(GameKitInventory component)$
        +static Unregister(string id)$
    }

    class GameKitActor {
        <<Registry>>
        -static Dictionary~string, GameKitActor~ _registry$
        +string actorId
        +static FindById(string id)$ GameKitActor
    }

    class ClientCode {
        <<Client>>
        +GetPlayerHealth()
    }

    note for GameKitHealth "Registry Pattern:\n静的Dictionaryでインスタンスを管理\nIDによるO(1)アクセスを提供\nOnEnable/OnDisableで自動登録"

    ClientCode --> GameKitHealth : FindById("player")
    ClientCode --> GameKitInventory : FindById("player_inventory")
```

---

## 8. Abstract Factory Pattern: GameKitManager

ManagerTypeに応じて関連するコンポーネント群を生成

```mermaid
classDiagram
    direction TB

    class GameKitManager {
        <<Abstract Factory>>
        +string managerId
        +ManagerType managerType
        +bool persistent
        +Component modeComponent
        +Initialize(string id, ManagerType type, bool isPersistent)
        +AttachModeComponent() Component
        -CreateTurnBasedComponents() Component
        -CreateResourcePoolComponents() Component
        -CreateEventHubComponents() Component
        -CreateStateManagerComponents() Component
    }

    class ManagerType {
        <<enumeration>>
        TurnBased
        ResourcePool
        EventHub
        StateManager
        Realtime
    }

    class GameKitTurnManager {
        <<Product - TurnBased>>
        +int currentTurn
        +List~string~ turnOrder
        +StartTurn()
        +EndTurn()
    }

    class GameKitResourceManager {
        <<Product - ResourcePool>>
        +Dictionary~string, int~ resources
        +AddResource(string, int)
        +GetResource(string) int
    }

    class GameKitEventManager {
        <<Product - EventHub>>
        +Publish(string eventName, object data)
        +Subscribe(string eventName, Action callback)
    }

    class GameKitStateManager {
        <<Product - StateManager>>
        +string currentState
        +SetState(string state)
        +GetStateData(string key) object
    }

    note for GameKitManager "Abstract Factory Pattern:\nManagerTypeに応じて\n適切なManagerコンポーネントを生成\n関連するコンポーネント群の一貫性を保証"

    GameKitManager --> ManagerType
    GameKitManager ..> GameKitTurnManager : creates when TurnBased
    GameKitManager ..> GameKitResourceManager : creates when ResourcePool
    GameKitManager ..> GameKitEventManager : creates when EventHub
    GameKitManager ..> GameKitStateManager : creates when StateManager
```

---

## 9. Strategy Pattern: BehaviorProfile (Movement System)

移動方式を交換可能なアルゴリズムとして実装

```mermaid
classDiagram
    direction TB

    class GameKitActor {
        <<Context>>
        +string actorId
        +BehaviorProfile behaviorProfile
        +ControlMode controlMode
        -IMovementStrategy _movementStrategy
        +Initialize(string id, BehaviorProfile behavior, ControlMode control)
        +SendMoveInput(Vector3 direction)
        -SelectMovementStrategy() IMovementStrategy
    }

    class IMovementStrategy {
        <<Strategy Interface>>
        +Move(Vector3 direction)*
        +CanMove()* bool
    }

    class TwoDLinearMovement {
        <<Concrete Strategy>>
        +Move(Vector3 direction)
        +CanMove() bool
    }

    class TwoDPhysicsMovement {
        <<Concrete Strategy>>
        -Rigidbody2D _rb
        +Move(Vector3 direction)
        +CanMove() bool
    }

    class TileGridMovement {
        <<Concrete Strategy>>
        -Vector2Int _currentTile
        +Move(Vector3 direction)
        +CanMove() bool
    }

    class GraphNodeMovement {
        <<Concrete Strategy>>
        -GraphNode _currentNode
        +Move(Vector3 direction)
        +CanMove() bool
    }

    class SplineMovement {
        <<Concrete Strategy>>
        -float _splinePosition
        +Move(Vector3 direction)
        +CanMove() bool
    }

    class BehaviorProfile {
        <<enumeration>>
        TwoDLinear
        TwoDPhysics
        TwoDTileGrid
        GraphNode
        SplineMovement
        ThreeDCharacter
        TopDown
        Platformer
    }

    note for GameKitActor "Strategy Pattern:\nBehaviorProfileに応じて\n移動アルゴリズムを切り替え"

    GameKitActor --> BehaviorProfile
    GameKitActor --> IMovementStrategy
    IMovementStrategy <|.. TwoDLinearMovement
    IMovementStrategy <|.. TwoDPhysicsMovement
    IMovementStrategy <|.. TileGridMovement
    IMovementStrategy <|.. GraphNodeMovement
    IMovementStrategy <|.. SplineMovement
```

---

## 10. Mediator Pattern: McpCommandProcessor

ハンドラー間の直接依存を排除し、中央で調整

```mermaid
classDiagram
    direction TB

    class McpCommandProcessor {
        <<Mediator>>
        +static Execute(McpIncomingCommand command)$ object
        +static ExecuteLegacy(McpIncomingCommand command)$ object
        +static GetHandlerMode(string toolName)$ string
        +static GetCompilationResult()$ Dictionary
        -static DispatchToHandler(string toolName, Dictionary payload)$ object
        -static HandleCompilationDependency(string toolName)$ void
    }

    class ICommandHandler {
        <<Colleague Interface>>
        +Execute(Dictionary payload) object
    }

    class SceneCommandHandler {
        <<Colleague>>
    }

    class GameObjectCommandHandler {
        <<Colleague>>
    }

    class ComponentCommandHandler {
        <<Colleague>>
    }

    class CompilationAwaitHandler {
        <<Colleague>>
    }

    note for McpCommandProcessor "Mediator Pattern:\nハンドラー間の通信を仲介\nコンパイル待機などの調整を担当\nハンドラーは互いを知らない"

    McpCommandProcessor --> ICommandHandler : coordinates
    ICommandHandler <|.. SceneCommandHandler
    ICommandHandler <|.. GameObjectCommandHandler
    ICommandHandler <|.. ComponentCommandHandler
    ICommandHandler <|.. CompilationAwaitHandler
    SceneCommandHandler ..> McpCommandProcessor : notifies
    GameObjectCommandHandler ..> McpCommandProcessor : notifies
```

---

## 11. Dependency Injection: Resource Resolvers

依存性を外部から注入し、テスタビリティを向上

```mermaid
classDiagram
    direction TB

    class BaseCommandHandler {
        <<Client>>
        #IPayloadValidator Validator
        #IGameObjectResolver GameObjectResolver
        #IAssetResolver AssetResolver
        #ITypeResolver TypeResolver
        +BaseCommandHandler()
        +BaseCommandHandler(IPayloadValidator, IGameObjectResolver, IAssetResolver, ITypeResolver)
    }

    class IPayloadValidator {
        <<Interface>>
        +Validate(Dictionary, string) ValidationResult
    }

    class IGameObjectResolver {
        <<Interface>>
        +Resolve(string) GameObject
        +TryResolve(string) GameObject
        +FindByPattern(string, bool, int) IEnumerable~GameObject~
    }

    class IAssetResolver {
        <<Interface>>
        +Resolve(string) Object
        +TryResolve(string) Object
    }

    class ITypeResolver {
        <<Interface>>
        +Resolve(string) Type
        +FindDerivedTypes(Type) IEnumerable~Type~
    }

    class StandardPayloadValidator {
        <<Concrete Implementation>>
        +Validate(Dictionary, string) ValidationResult
    }

    class UnityGameObjectResolver {
        <<Concrete Implementation>>
        +Resolve(string) GameObject
        +FindByPattern(string, bool, int) IEnumerable~GameObject~
    }

    class MockGameObjectResolver {
        <<Test Double>>
        +Resolve(string) GameObject
    }

    note for BaseCommandHandler "Dependency Injection:\nコンストラクタで依存性を注入\nテスト時にMockを注入可能"

    BaseCommandHandler --> IPayloadValidator
    BaseCommandHandler --> IGameObjectResolver
    BaseCommandHandler --> IAssetResolver
    BaseCommandHandler --> ITypeResolver
    IPayloadValidator <|.. StandardPayloadValidator
    IGameObjectResolver <|.. UnityGameObjectResolver
    IGameObjectResolver <|.. MockGameObjectResolver
```

---

## 12. Prototype Pattern: ScriptableObject Assets

既存オブジェクトをコピーして新インスタンスを生成

```mermaid
classDiagram
    direction TB

    class ScriptableObject {
        <<Unity Base>>
        +static CreateInstance~T~()$ T
        +static Instantiate(Object original)$ Object
    }

    class GameKitItemAsset {
        <<Prototype>>
        +string itemId
        +string displayName
        +Sprite icon
        +string category
        +int maxStack
        +Dictionary~string, object~ properties
        +Clone() GameKitItemAsset
    }

    class GameKitQuestAsset {
        <<Prototype>>
        +string questId
        +string title
        +List~QuestObjective~ objectives
        +Clone() GameKitQuestAsset
    }

    class GameKitEffectAsset {
        <<Prototype>>
        +string effectId
        +EffectType type
        +float duration
        +Clone() GameKitEffectAsset
    }

    class GameKitMachinationsAsset {
        <<Prototype>>
        +List~ResourceDefinition~ resources
        +List~NodeDefinition~ nodes
        +Clone() GameKitMachinationsAsset
    }

    note for GameKitItemAsset "Prototype Pattern:\nScriptableObjectをテンプレートとして\nInstantiate()で複製して使用\nランタイムでの動的生成に活用"

    ScriptableObject <|-- GameKitItemAsset
    ScriptableObject <|-- GameKitQuestAsset
    ScriptableObject <|-- GameKitEffectAsset
    ScriptableObject <|-- GameKitMachinationsAsset
```

---

## 13. Command Pattern: MCP Tool Execution

リクエストをオブジェクトとしてカプセル化

```mermaid
classDiagram
    direction TB

    class McpIncomingCommand {
        <<Command>>
        +string ToolName
        +string Operation
        +Dictionary~string, object~ Payload
        +string RequestId
        +DateTime Timestamp
    }

    class McpCommandProcessor {
        <<Invoker>>
        +static Execute(McpIncomingCommand command)$ object
        +static Queue~McpIncomingCommand~ _pendingCommands$
        +static EnqueueCommand(McpIncomingCommand command)$
        +static ProcessPendingCommands()$
    }

    class ICommandHandler {
        <<Receiver Interface>>
        +Execute(Dictionary payload) object
    }

    class McpPendingCommandStorage {
        <<Command Store>>
        +Store(McpIncomingCommand command)
        +Retrieve() McpIncomingCommand
        +HasPending() bool
    }

    note for McpIncomingCommand "Command Pattern:\nMCPリクエストをオブジェクト化\n遅延実行、キュー管理、Undo可能"

    McpCommandProcessor --> McpIncomingCommand : executes
    McpCommandProcessor --> ICommandHandler : delegates to
    McpCommandProcessor --> McpPendingCommandStorage : stores pending
```

---

## 14. Composite Pattern: Scene Hierarchy

ツリー構造でGameObjectを統一的に扱う

```mermaid
classDiagram
    direction TB

    class ISceneElement {
        <<Component Interface>>
        +GetName() string
        +SetActive(bool active)
        +GetChildren() IEnumerable~ISceneElement~
    }

    class GameObjectWrapper {
        <<Leaf / Composite>>
        -GameObject _gameObject
        -List~GameObjectWrapper~ _children
        +GetName() string
        +SetActive(bool active)
        +GetChildren() IEnumerable~ISceneElement~
        +AddChild(GameObjectWrapper child)
        +RemoveChild(GameObjectWrapper child)
    }

    class SceneCommandHandler {
        <<Client>>
        +InspectScene() object
        -TraverseHierarchy(Transform root) List~object~
    }

    note for GameObjectWrapper "Composite Pattern:\nGameObject階層をツリー構造で表現\n親子を統一インターフェースで操作"

    ISceneElement <|.. GameObjectWrapper
    GameObjectWrapper o-- GameObjectWrapper : children
    SceneCommandHandler --> ISceneElement : uses
```

---

## 15. State Pattern: GameKitSceneFlow

シーン状態に応じて振る舞いを変更

```mermaid
classDiagram
    direction TB

    class GameKitSceneFlow {
        <<Context>>
        +string flowId
        +string currentSceneName
        -ISceneState _currentState
        +TransitionTo(string sceneName)
        +Update()
        -SetState(ISceneState state)
    }

    class ISceneState {
        <<State Interface>>
        +Enter(GameKitSceneFlow context)*
        +Exit(GameKitSceneFlow context)*
        +Update(GameKitSceneFlow context)*
        +CanTransitionTo(string sceneName)* bool
    }

    class LoadingState {
        <<Concrete State>>
        +Enter(GameKitSceneFlow context)
        +Exit(GameKitSceneFlow context)
        +Update(GameKitSceneFlow context)
        +CanTransitionTo(string sceneName) bool
    }

    class PlayingState {
        <<Concrete State>>
        +Enter(GameKitSceneFlow context)
        +Exit(GameKitSceneFlow context)
        +Update(GameKitSceneFlow context)
        +CanTransitionTo(string sceneName) bool
    }

    class PausedState {
        <<Concrete State>>
        +Enter(GameKitSceneFlow context)
        +Exit(GameKitSceneFlow context)
        +Update(GameKitSceneFlow context)
        +CanTransitionTo(string sceneName) bool
    }

    class TransitioningState {
        <<Concrete State>>
        -string _targetScene
        +Enter(GameKitSceneFlow context)
        +Exit(GameKitSceneFlow context)
        +Update(GameKitSceneFlow context)
        +CanTransitionTo(string sceneName) bool
    }

    note for GameKitSceneFlow "State Pattern:\nシーン状態をオブジェクト化\n状態遷移ロジックをカプセル化"

    GameKitSceneFlow --> ISceneState
    ISceneState <|.. LoadingState
    ISceneState <|.. PlayingState
    ISceneState <|.. PausedState
    ISceneState <|.. TransitioningState
```

---

## 16. 完全なコマンド実行シーケンス（パターン適用）

```mermaid
sequenceDiagram
    participant AI as AI Client
    participant PY as MCP Server
    participant Facade as McpBridgeService<br/>«Singleton+Facade»
    participant Mediator as McpCommandProcessor<br/>«Mediator»
    participant Factory as CommandHandlerFactory<br/>«Factory»
    participant Strategy as ICommandHandler<br/>«Strategy»
    participant Template as BaseCommandHandler<br/>«Template Method»
    participant Registry as GameKitHealth<br/>«Registry»

    AI->>PY: Tool Call
    PY->>Facade: WebSocket Message
    Note over Facade: Facade hides complexity

    Facade->>Mediator: Execute(command)
    Note over Mediator: Mediator coordinates

    Mediator->>Factory: GetHandler(toolName)
    Note over Factory: Factory creates/retrieves

    Factory-->>Mediator: ConcreteHandler

    Mediator->>Strategy: Execute(payload)
    Note over Strategy: Strategy selected

    Strategy->>Template: Execute(payload)
    Note over Template: Template Method executes

    Template->>Template: 1. ValidatePayload()
    Template->>Template: 2. ExecuteOperation()
    Template->>Registry: FindById(id)
    Note over Registry: Registry lookup O(1)
    Registry-->>Template: component
    Template->>Template: 3. WaitForCompilation()

    Template-->>Strategy: result
    Strategy-->>Mediator: result
    Mediator-->>Facade: response
    Facade-->>PY: WebSocket Response
    PY-->>AI: Tool Result
```

---

## デザインパターン一覧

| パターン | 分類 | 適用箇所 | 目的 |
|----------|------|----------|------|
| **Singleton** | 生成 | McpBridgeService | 単一のWebSocket接続管理 |
| **Factory** | 生成 | CommandHandlerFactory | ハンドラーの動的生成・取得 |
| **Abstract Factory** | 生成 | GameKitManager | Manager種別に応じたコンポーネント群生成 |
| **Prototype** | 生成 | ScriptableObject Assets | テンプレートからの複製生成 |
| **Facade** | 構造 | McpBridgeService | WebSocket通信の複雑性隠蔽 |
| **Composite** | 構造 | Scene Hierarchy | ツリー構造の統一操作 |
| **Strategy** | 振舞 | ICommandHandler, BehaviorProfile | アルゴリズムの交換可能性 |
| **Template Method** | 振舞 | BaseCommandHandler | 処理フローの骨格定義 |
| **Observer** | 振舞 | UnityEvent System | イベント駆動の疎結合通知 |
| **Mediator** | 振舞 | McpCommandProcessor | ハンドラー間調整 |
| **Command** | 振舞 | McpIncomingCommand | リクエストのオブジェクト化 |
| **State** | 振舞 | GameKitSceneFlow | 状態に応じた振る舞い変更 |
| **Registry** | その他 | GameKitHealth, GameKitInventory | IDによる高速検索 |
| **DI** | その他 | BaseCommandHandler | 依存性の外部注入 |
