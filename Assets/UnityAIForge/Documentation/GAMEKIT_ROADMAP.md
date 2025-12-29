# GameKit Future Roadmap

This document outlines the planned MCP tools for future GameKit development. These tools are designed to prevent LLMs from writing custom C# logic code by providing declarative, high-level abstractions.

## Current Status (v2.7.0)

### ✅ Implemented Tools

**Phase 1 - Core Game Mechanics:**
- `unity_gamekit_health` - HP/ダメージシステム
- `unity_gamekit_spawner` - スポーンシステム
- `unity_gamekit_timer` - タイマー/クールダウン
- `unity_gamekit_ai` - AI行動

**Phase 2 - Additional Game Mechanics:**
- `unity_gamekit_collectible` - 収集アイテム
- `unity_gamekit_projectile` - 弾丸/ミサイル
- `unity_gamekit_waypoint` - パス追従
- `unity_gamekit_trigger_zone` - トリガーゾーン

**Phase 3 - Animation & Effects:**
- `unity_gamekit_animation_sync` - アニメーション同期
- `unity_gamekit_effect` - エフェクトシステム（パーティクル/サウンド/カメラシェイク/スクリーンフラッシュ）

**Phase 4 - Persistence & Inventory:**
- `unity_gamekit_save` - 宣言的セーブ/ロードシステム（プロファイル・スロット管理）
- `unity_gamekit_inventory` - インベントリシステム（アイテム・スタック・装備）

---

## 🔮 Future Phases

### ~~Phase 3: Animation & Effects~~ ✅ IMPLEMENTED

### ~~Phase 4: Persistence & Inventory~~ ✅ IMPLEMENTED

#### unity_gamekit_animation_sync
**目的:** LLMがAnimatorコントローラーを直接操作するコードを書く代わりに、宣言的なアニメーション同期を提供

**ユースケース:**
- 移動速度とアニメーション速度の同期
- 状態変化に応じたアニメーション切り替え
- イベントトリガーアニメーション

**設計案:**
```python
unity_gamekit_animation_sync(
    operation='create',
    gameObjectPath='Player',
    syncId='player_anim',
    animatorPath='Player',  # Animator component path
    syncRules=[
        {
            'parameter': 'Speed',
            'sourceType': 'rigidbody2d',
            'sourceProperty': 'velocity.magnitude',
            'multiplier': 1.0
        },
        {
            'parameter': 'IsGrounded',
            'sourceType': 'custom',
            'checkMethod': 'groundCheck'  # SerializeField bool
        }
    ],
    triggers=[
        {
            'triggerName': 'Jump',
            'eventSource': 'input',
            'inputAction': 'Jump'
        },
        {
            'triggerName': 'Attack',
            'eventSource': 'health',
            'healthId': 'player_hp',
            'event': 'OnDamaged'
        }
    ]
)
```

**Runtime Component設計:**
```csharp
public class GameKitAnimationSync : MonoBehaviour
{
    [SerializeField] private string syncId;
    [SerializeField] private Animator animator;
    [SerializeField] private List<AnimSyncRule> syncRules;
    [SerializeField] private List<AnimTriggerRule> triggers;

    [System.Serializable]
    public class AnimSyncRule
    {
        public string parameterName;
        public SyncSourceType sourceType;
        public string sourceProperty;
        public float multiplier = 1f;
    }

    public enum SyncSourceType { Rigidbody2D, Rigidbody3D, Transform, Custom }
}
```

---

#### unity_gamekit_effect
**目的:** LLMがパーティクル/サウンド/スクリーンエフェクトを個別に制御するコードを書く代わりに、統合エフェクトシステムを提供

**ユースケース:**
- ダメージ時のヒットエフェクト（パーティクル + サウンド + カメラシェイク）
- 収集時のエフェクト
- 状態変化時のビジュアルフィードバック

**設計案:**
```python
unity_gamekit_effect(
    operation='create',
    effectId='hit_effect',
    assetPath='Assets/Effects/HitEffect.asset',  # ScriptableObject
    components=[
        {
            'type': 'particle',
            'prefabPath': 'Assets/Particles/HitSpark.prefab',
            'duration': 0.5,
            'attachToTarget': False
        },
        {
            'type': 'sound',
            'clipPath': 'Assets/Audio/hit.wav',
            'volume': 0.8,
            'pitchVariation': 0.1
        },
        {
            'type': 'cameraShake',
            'intensity': 0.3,
            'duration': 0.2,
            'frequency': 25
        },
        {
            'type': 'screenFlash',
            'color': {'r': 1, 'g': 0, 'b': 0, 'a': 0.3},
            'duration': 0.1
        }
    ]
)

# エフェクト再生
unity_gamekit_effect(
    operation='play',
    effectId='hit_effect',
    position={'x': 0, 'y': 1, 'z': 0}
)

# GameKitHealthと連携
unity_gamekit_health(
    operation='update',
    healthId='player_hp',
    onDamageEffect='hit_effect',  # 自動再生
    onDeathEffect='death_effect'
)
```

**Runtime Component設計:**
```csharp
[CreateAssetMenu(fileName = "Effect", menuName = "GameKit/Effect")]
public class GameKitEffectAsset : ScriptableObject
{
    public string effectId;
    public List<EffectComponent> components;

    [System.Serializable]
    public class EffectComponent
    {
        public EffectType type;
        public GameObject prefab;
        public AudioClip audioClip;
        public float duration;
        // ... other settings
    }
}

public class GameKitEffectManager : MonoBehaviour
{
    public static GameKitEffectManager Instance { get; private set; }

    public void PlayEffect(string effectId, Vector3 position);
    public void PlayEffect(GameKitEffectAsset asset, Vector3 position);
}
```

---

### Phase 4: Persistence & Inventory

#### unity_gamekit_save
**目的:** LLMがPlayerPrefs/JSONシリアライズコードを書く代わりに、宣言的なセーブ/ロードシステムを提供

**ユースケース:**
- ゲーム進行状態の保存
- プレイヤーデータの永続化
- チェックポイントシステム

**設計案:**
```python
unity_gamekit_save(
    operation='createProfile',
    profileId='main_save',
    saveTargets=[
        {
            'type': 'resourceManager',
            'managerId': 'player_resources',
            'saveKey': 'resources'
        },
        {
            'type': 'transform',
            'gameObjectPath': 'Player',
            'saveKey': 'playerPosition'
        },
        {
            'type': 'custom',
            'componentPath': 'GameManager',
            'componentType': 'GameManager',
            'properties': ['currentLevel', 'score', 'playTime']
        },
        {
            'type': 'sceneFlow',
            'flowId': 'main_flow',
            'saveKey': 'currentScene'
        }
    ],
    autoSave={
        'enabled': True,
        'intervalSeconds': 300,  # 5分ごと
        'onSceneChange': True
    }
)

# セーブ実行
unity_gamekit_save(
    operation='save',
    profileId='main_save',
    slotId='slot_1'
)

# ロード実行
unity_gamekit_save(
    operation='load',
    profileId='main_save',
    slotId='slot_1'
)

# セーブスロット一覧
unity_gamekit_save(
    operation='listSlots',
    profileId='main_save'
)
```

**Runtime Component設計:**
```csharp
[CreateAssetMenu(fileName = "SaveProfile", menuName = "GameKit/SaveProfile")]
public class GameKitSaveProfile : ScriptableObject
{
    public string profileId;
    public List<SaveTarget> saveTargets;
    public AutoSaveSettings autoSave;
}

public class GameKitSaveManager : MonoBehaviour
{
    public static GameKitSaveManager Instance { get; private set; }

    public void Save(string profileId, string slotId);
    public void Load(string profileId, string slotId);
    public List<SaveSlotInfo> GetSlots(string profileId);
    public void DeleteSlot(string profileId, string slotId);

    // Events
    public UnityEvent<string> OnSaveComplete;
    public UnityEvent<string> OnLoadComplete;
    public UnityEvent<string, string> OnSaveError;
}
```

---

#### unity_gamekit_inventory
**目的:** LLMがインベントリ管理ロジックを書く代わりに、宣言的なインベントリシステムを提供

**ユースケース:**
- アイテム収集と管理
- 装備システム
- スタック可能アイテム
- インベントリUI連携

**設計案:**
```python
# インベントリ作成
unity_gamekit_inventory(
    operation='create',
    inventoryId='player_inventory',
    gameObjectPath='Player',
    maxSlots=20,
    categories=['weapon', 'armor', 'consumable', 'key'],
    stackableCategories=['consumable'],
    maxStackSize=99
)

# アイテム定義（ScriptableObject）
unity_gamekit_inventory(
    operation='defineItem',
    itemId='health_potion',
    assetPath='Assets/Items/HealthPotion.asset',
    itemData={
        'displayName': 'Health Potion',
        'description': 'Restores 50 HP',
        'category': 'consumable',
        'stackable': True,
        'maxStack': 10,
        'icon': 'Assets/Sprites/Items/health_potion.png',
        'onUse': {
            'type': 'heal',
            'healthId': 'player_hp',
            'amount': 50
        }
    }
)

# アイテム追加
unity_gamekit_inventory(
    operation='addItem',
    inventoryId='player_inventory',
    itemId='health_potion',
    quantity=3
)

# アイテム使用
unity_gamekit_inventory(
    operation='useItem',
    inventoryId='player_inventory',
    slotIndex=0
)

# 装備スロット
unity_gamekit_inventory(
    operation='equip',
    inventoryId='player_inventory',
    slotIndex=5,
    equipSlot='weapon'
)
```

**Runtime Component設計:**
```csharp
[CreateAssetMenu(fileName = "Item", menuName = "GameKit/Item")]
public class GameKitItemAsset : ScriptableObject
{
    public string itemId;
    public string displayName;
    public string description;
    public ItemCategory category;
    public bool stackable;
    public int maxStack;
    public Sprite icon;
    public ItemUseAction onUse;
}

public class GameKitInventory : MonoBehaviour
{
    [SerializeField] private string inventoryId;
    [SerializeField] private int maxSlots;
    [SerializeField] private List<InventorySlot> slots;

    public bool AddItem(string itemId, int quantity = 1);
    public bool RemoveItem(string itemId, int quantity = 1);
    public bool UseItem(int slotIndex);
    public bool Equip(int slotIndex, string equipSlot);
    public bool Unequip(string equipSlot);

    // Events
    public UnityEvent<string, int> OnItemAdded;
    public UnityEvent<string, int> OnItemRemoved;
    public UnityEvent<string> OnItemUsed;
    public UnityEvent<string, string> OnItemEquipped;
}
```

---

### Phase 5: Story & Quest Systems

#### unity_gamekit_dialogue
**目的:** LLMが対話システムロジックを書く代わりに、宣言的な対話システムを提供

**ユースケース:**
- NPC会話
- 選択肢分岐
- 条件付き対話
- UI連携

**設計案:**
```python
# 対話データ作成
unity_gamekit_dialogue(
    operation='createDialogue',
    dialogueId='npc_merchant',
    assetPath='Assets/Dialogues/Merchant.asset',
    nodes=[
        {
            'nodeId': 'start',
            'speaker': 'Merchant',
            'text': 'Welcome, traveler! What can I do for you?',
            'choices': [
                {'text': 'Show me your wares', 'nextNode': 'shop'},
                {'text': 'Any news?', 'nextNode': 'rumors'},
                {'text': 'Goodbye', 'nextNode': 'end'}
            ]
        },
        {
            'nodeId': 'shop',
            'speaker': 'Merchant',
            'text': 'Take a look!',
            'action': {'type': 'openShop', 'shopId': 'merchant_shop'},
            'nextNode': 'end'
        },
        {
            'nodeId': 'rumors',
            'speaker': 'Merchant',
            'text': 'I heard there is a dragon in the mountain...',
            'conditions': [
                {'type': 'quest', 'questId': 'dragon_slayer', 'state': 'notStarted'}
            ],
            'nextNode': 'start'
        },
        {
            'nodeId': 'end',
            'type': 'exit'
        }
    ]
)

# 対話開始
unity_gamekit_dialogue(
    operation='startDialogue',
    dialogueId='npc_merchant',
    speakerGameObjectPath='NPC_Merchant'
)

# 選択肢選択
unity_gamekit_dialogue(
    operation='selectChoice',
    choiceIndex=0
)
```

**Runtime Component設計:**
```csharp
[CreateAssetMenu(fileName = "Dialogue", menuName = "GameKit/Dialogue")]
public class GameKitDialogueAsset : ScriptableObject
{
    public string dialogueId;
    public List<DialogueNode> nodes;
}

public class GameKitDialogueManager : MonoBehaviour
{
    public static GameKitDialogueManager Instance { get; private set; }

    public void StartDialogue(string dialogueId);
    public void SelectChoice(int choiceIndex);
    public void SkipToNextNode();

    // Events
    public UnityEvent<DialogueNode> OnNodeEnter;
    public UnityEvent<List<DialogueChoice>> OnChoicesAvailable;
    public UnityEvent OnDialogueEnd;
}
```

---

#### unity_gamekit_quest
**目的:** LLMがクエスト管理ロジックを書く代わりに、宣言的なクエストシステムを提供

**ユースケース:**
- クエスト進行管理
- 目標追跡
- 報酬システム
- 前提条件チェック

**設計案:**
```python
# クエスト定義
unity_gamekit_quest(
    operation='createQuest',
    questId='dragon_slayer',
    assetPath='Assets/Quests/DragonSlayer.asset',
    questData={
        'title': 'Dragon Slayer',
        'description': 'Defeat the dragon in the mountain',
        'category': 'main',
        'prerequisites': [
            {'type': 'level', 'minLevel': 10},
            {'type': 'quest', 'questId': 'village_saved', 'state': 'completed'}
        ],
        'objectives': [
            {
                'objectiveId': 'find_cave',
                'type': 'location',
                'description': 'Find the dragon cave',
                'targetLocation': 'DragonCaveEntrance',
                'radius': 5.0
            },
            {
                'objectiveId': 'defeat_dragon',
                'type': 'kill',
                'description': 'Defeat the dragon',
                'targetTag': 'Dragon',
                'requiredCount': 1
            },
            {
                'objectiveId': 'return_hero',
                'type': 'talk',
                'description': 'Return to the village chief',
                'targetNPC': 'VillageChief'
            }
        ],
        'rewards': [
            {'type': 'resource', 'resourceName': 'gold', 'amount': 1000},
            {'type': 'item', 'itemId': 'dragon_sword'},
            {'type': 'experience', 'amount': 500}
        ]
    }
)

# クエスト開始
unity_gamekit_quest(
    operation='startQuest',
    questId='dragon_slayer'
)

# 目標進捗更新（通常は自動追跡）
unity_gamekit_quest(
    operation='updateObjective',
    questId='dragon_slayer',
    objectiveId='defeat_dragon',
    progress=1
)

# クエスト完了
unity_gamekit_quest(
    operation='completeQuest',
    questId='dragon_slayer'
)

# アクティブクエスト一覧
unity_gamekit_quest(
    operation='listQuests',
    filter='active'  # 'active', 'completed', 'available', 'all'
)
```

**Runtime Component設計:**
```csharp
[CreateAssetMenu(fileName = "Quest", menuName = "GameKit/Quest")]
public class GameKitQuestAsset : ScriptableObject
{
    public string questId;
    public string title;
    public string description;
    public QuestCategory category;
    public List<QuestPrerequisite> prerequisites;
    public List<QuestObjective> objectives;
    public List<QuestReward> rewards;
}

public class GameKitQuestManager : MonoBehaviour
{
    public static GameKitQuestManager Instance { get; private set; }

    public bool StartQuest(string questId);
    public bool AbandonQuest(string questId);
    public void UpdateObjective(string questId, string objectiveId, int progress);
    public bool CompleteQuest(string questId);
    public List<GameKitQuestAsset> GetQuests(QuestFilter filter);

    // Events
    public UnityEvent<string> OnQuestStarted;
    public UnityEvent<string, string> OnObjectiveUpdated;
    public UnityEvent<string> OnQuestCompleted;
    public UnityEvent<string> OnQuestFailed;
}
```

---

#### unity_gamekit_status_effect
**目的:** LLMがバフ/デバフロジックを書く代わりに、宣言的なステータス効果システムを提供

**ユースケース:**
- 一時的なステータス変更（バフ/デバフ）
- 毒/燃焼などのDoTエフェクト
- 無敵/スタンなどの状態異常
- 装備による永続効果

**設計案:**
```python
# ステータス効果定義
unity_gamekit_status_effect(
    operation='defineEffect',
    effectId='poison',
    assetPath='Assets/Effects/Poison.asset',
    effectData={
        'displayName': 'Poison',
        'description': 'Deals damage over time',
        'icon': 'Assets/Sprites/Effects/poison.png',
        'type': 'debuff',
        'duration': 10.0,
        'stackable': True,
        'maxStacks': 3,
        'tickInterval': 1.0,
        'effects': [
            {
                'type': 'dot',  # Damage over time
                'healthId': 'target',
                'damagePerTick': 5,
                'scaleWithStacks': True
            }
        ],
        'visualEffect': 'poison_particles',
        'onApply': {'sound': 'poison_apply'},
        'onRemove': {'sound': 'poison_remove'}
    }
)

# 強化効果定義
unity_gamekit_status_effect(
    operation='defineEffect',
    effectId='speed_boost',
    assetPath='Assets/Effects/SpeedBoost.asset',
    effectData={
        'displayName': 'Speed Boost',
        'type': 'buff',
        'duration': 5.0,
        'effects': [
            {
                'type': 'statModifier',
                'stat': 'moveSpeed',
                'modifierType': 'percentAdd',
                'value': 50  # +50%
            }
        ]
    }
)

# 効果適用
unity_gamekit_status_effect(
    operation='applyEffect',
    effectId='poison',
    targetPath='Player',
    source='Enemy'  # 効果の発生源（オプション）
)

# 効果除去
unity_gamekit_status_effect(
    operation='removeEffect',
    effectId='poison',
    targetPath='Player',
    removeAllStacks=True
)

# アクティブ効果確認
unity_gamekit_status_effect(
    operation='getActiveEffects',
    targetPath='Player'
)
```

**Runtime Component設計:**
```csharp
[CreateAssetMenu(fileName = "StatusEffect", menuName = "GameKit/StatusEffect")]
public class GameKitStatusEffectAsset : ScriptableObject
{
    public string effectId;
    public string displayName;
    public string description;
    public Sprite icon;
    public EffectType type;
    public float duration;
    public bool stackable;
    public int maxStacks;
    public float tickInterval;
    public List<EffectModifier> effects;
}

public class GameKitStatusEffectReceiver : MonoBehaviour
{
    [SerializeField] private List<ActiveEffect> activeEffects;

    public bool ApplyEffect(string effectId, GameObject source = null);
    public bool RemoveEffect(string effectId, bool allStacks = false);
    public bool HasEffect(string effectId);
    public int GetStackCount(string effectId);
    public List<ActiveEffect> GetActiveEffects();

    // Events
    public UnityEvent<string, int> OnEffectApplied;  // effectId, stacks
    public UnityEvent<string> OnEffectRemoved;
    public UnityEvent<string> OnEffectExpired;
}
```

---

## 実装優先度

| Phase | Tools | Priority | Status |
|-------|-------|----------|--------|
| 3 | animation_sync | Medium | ✅ Implemented |
| 3 | effect | High | ✅ Implemented |
| 4 | save | High | ✅ Implemented |
| 4 | inventory | High | ✅ Implemented |
| 5 | dialogue | Medium | Pending |
| 5 | quest | Medium | Pending |
| 5 | status_effect | Medium | Pending |

## 設計原則

### 1. 宣言的API
- LLMがC#ロジックを書かなくて済むように
- JSON/Dictionary形式でのデータ定義
- 手続き的なコードは内部実装に隠蔽

### 2. ScriptableObject活用
- 再利用可能なデータアセット
- Inspector編集可能
- プリセット/テンプレート化

### 3. UnityEvent連携
- 他のGameKitコンポーネントとの疎結合
- Inspector上でのワイヤリング
- コード不要の連携

### 4. ID基底システム
- 全コンポーネントはユニークIDで識別
- パス指定よりも安定した参照
- findById操作のサポート

---

*Last Updated: 2024-12 (v2.7.0)*
*Document Status: Phase 4 Implemented*
