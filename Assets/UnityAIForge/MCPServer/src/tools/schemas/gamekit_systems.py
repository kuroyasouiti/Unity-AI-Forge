"""Schema definitions for GameKit system MCP tools."""

from __future__ import annotations

from typing import Any

from tools.schemas.common import schema_with_required


def gamekit_health_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_health MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "applyDamage",
                        "heal",
                        "kill",
                        "respawn",
                        "setInvincible",
                        "findByHealthId",
                    ],
                    "description": "Health operation to perform.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "healthId": {
                    "type": "string",
                    "description": "Unique health component identifier.",
                },
                "maxHealth": {
                    "type": "number",
                    "description": "Maximum health value.",
                    "default": 100,
                },
                "currentHealth": {"type": "number", "description": "Current health value."},
                "invincibilityDuration": {
                    "type": "number",
                    "description": "Duration of invincibility after taking damage (seconds).",
                    "default": 0.5,
                },
                "canTakeDamage": {
                    "type": "boolean",
                    "description": "Whether the entity can take damage.",
                },
                "onDeath": {
                    "type": "string",
                    "enum": ["destroy", "disable", "respawn", "event"],
                    "description": "Behavior when health reaches zero.",
                },
                "respawnPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Position to respawn at.",
                },
                "respawnDelay": {
                    "type": "number",
                    "description": "Delay before respawning (seconds).",
                },
                "resetHealthOnRespawn": {
                    "type": "boolean",
                    "description": "Reset health to max on respawn.",
                },
                "amount": {
                    "type": "number",
                    "description": "Amount for applyDamage/heal operations.",
                },
                "invincible": {
                    "type": "boolean",
                    "description": "Set invincibility state for setInvincible operation.",
                },
                "duration": {
                    "type": "number",
                    "description": "Invincibility duration for setInvincible operation.",
                },
            },
        },
        ["operation"],
    )


def gamekit_spawner_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_spawner MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "start",
                        "stop",
                        "reset",
                        "spawnOne",
                        "spawnBurst",
                        "despawnAll",
                        "addSpawnPoint",
                        "addWave",
                        "findBySpawnerId",
                    ],
                    "description": "Spawner operation to perform.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "spawnerId": {"type": "string", "description": "Unique spawner identifier."},
                "prefabPath": {"type": "string", "description": "Path to prefab asset to spawn."},
                "spawnMode": {
                    "type": "string",
                    "enum": ["interval", "wave", "burst", "manual"],
                    "description": "Spawning mode.",
                },
                "autoStart": {
                    "type": "boolean",
                    "description": "Start spawning automatically on scene start.",
                },
                "spawnInterval": {
                    "type": "number",
                    "description": "Time between spawns (seconds).",
                    "default": 3.0,
                },
                "initialDelay": {"type": "number", "description": "Delay before first spawn."},
                "maxActive": {
                    "type": "integer",
                    "description": "Maximum active instances at once.",
                    "default": 10,
                },
                "maxTotal": {
                    "type": "integer",
                    "description": "Maximum total spawns (-1 for unlimited).",
                },
                "spawnPointMode": {
                    "type": "string",
                    "enum": ["sequential", "random", "randomNoRepeat"],
                    "description": "How to select spawn points.",
                },
                "usePool": {
                    "type": "boolean",
                    "description": "Use object pooling.",
                    "default": True,
                },
                "poolInitialSize": {"type": "integer", "description": "Initial pool size."},
                "loopWaves": {"type": "boolean", "description": "Loop waves after completing all."},
                "delayBetweenWaves": {
                    "type": "number",
                    "description": "Delay between waves (seconds).",
                },
                "waves": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "count": {
                                "type": "integer",
                                "description": "Number of enemies in wave.",
                            },
                            "delay": {"type": "number", "description": "Delay before wave starts."},
                            "spawnInterval": {
                                "type": "number",
                                "description": "Time between spawns in wave.",
                            },
                        },
                    },
                    "description": "Wave configurations.",
                },
                "positionRandomness": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Random offset range for spawn positions.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Position for new spawn point.",
                },
                "pointPath": {
                    "type": "string",
                    "description": "Path to existing GameObject to use as spawn point.",
                },
                "count": {"type": "integer", "description": "Number to spawn for spawnBurst."},
            },
        },
        ["operation"],
    )


def gamekit_timer_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_timer MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createTimer",
                        "updateTimer",
                        "inspectTimer",
                        "deleteTimer",
                        "createCooldown",
                        "updateCooldown",
                        "inspectCooldown",
                        "deleteCooldown",
                        "createCooldownManager",
                        "addCooldownToManager",
                        "inspectCooldownManager",
                        "findByTimerId",
                        "findByCooldownId",
                    ],
                    "description": "Timer/Cooldown operation to perform.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "timerId": {"type": "string", "description": "Unique timer identifier."},
                "duration": {
                    "type": "number",
                    "description": "Timer duration (seconds).",
                    "default": 5.0,
                },
                "loop": {
                    "type": "boolean",
                    "description": "Loop timer when complete.",
                    "default": False,
                },
                "autoStart": {
                    "type": "boolean",
                    "description": "Start timer automatically.",
                    "default": False,
                },
                "unscaledTime": {
                    "type": "boolean",
                    "description": "Use unscaled time (ignores Time.timeScale).",
                    "default": False,
                },
                "cooldownId": {"type": "string", "description": "Unique cooldown identifier."},
                "cooldownDuration": {
                    "type": "number",
                    "description": "Cooldown duration (seconds).",
                    "default": 1.0,
                },
                "startReady": {
                    "type": "boolean",
                    "description": "Start with cooldown ready.",
                    "default": True,
                },
                "cooldowns": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "id": {"type": "string", "description": "Cooldown ID."},
                            "duration": {"type": "number", "description": "Cooldown duration."},
                            "startReady": {"type": "boolean", "description": "Start ready."},
                        },
                    },
                    "description": "Cooldown configurations for CooldownManager.",
                },
            },
        },
        ["operation"],
    )


def gamekit_ai_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_ai MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "setTarget",
                        "clearTarget",
                        "setState",
                        "addPatrolPoint",
                        "clearPatrolPoints",
                        "findByAIId",
                    ],
                    "description": "AI behavior operation to perform.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "aiId": {"type": "string", "description": "Unique AI behavior identifier."},
                "behaviorType": {
                    "type": "string",
                    "enum": ["patrol", "chase", "flee", "patrolAndChase"],
                    "description": "AI behavior type.",
                },
                "use2D": {"type": "boolean", "description": "Use 2D movement.", "default": True},
                "moveSpeed": {"type": "number", "description": "Movement speed.", "default": 3.0},
                "turnSpeed": {"type": "number", "description": "Turn speed.", "default": 5.0},
                "patrolMode": {
                    "type": "string",
                    "enum": ["loop", "pingPong", "random"],
                    "description": "Patrol point traversal mode.",
                },
                "waitTimeAtPoint": {
                    "type": "number",
                    "description": "Wait time at each patrol point.",
                },
                "patrolPoints": {
                    "type": "array",
                    "items": {
                        "oneOf": [
                            {"type": "string", "description": "Path to existing GameObject."},
                            {
                                "type": "object",
                                "properties": {
                                    "x": {"type": "number"},
                                    "y": {"type": "number"},
                                    "z": {"type": "number"},
                                },
                                "description": "Position to create new patrol point.",
                            },
                        ],
                    },
                    "description": "Patrol point paths or positions.",
                },
                "chaseTargetTag": {
                    "type": "string",
                    "description": "Tag of GameObjects to chase.",
                    "default": "Player",
                },
                "chaseTargetPath": {
                    "type": "string",
                    "description": "Path to specific chase target.",
                },
                "detectionRadius": {
                    "type": "number",
                    "description": "Detection range.",
                    "default": 10.0,
                },
                "loseTargetDistance": {
                    "type": "number",
                    "description": "Distance at which to lose target.",
                    "default": 15.0,
                },
                "fieldOfView": {
                    "type": "number",
                    "description": "Field of view in degrees.",
                    "default": 360,
                },
                "requireLineOfSight": {
                    "type": "boolean",
                    "description": "Require line of sight for detection.",
                },
                "attackRange": {"type": "number", "description": "Attack range.", "default": 2.0},
                "attackCooldown": {
                    "type": "number",
                    "description": "Attack cooldown (seconds).",
                    "default": 1.0,
                },
                "fleeDistance": {"type": "number", "description": "Distance to flee."},
                "safeDistance": {"type": "number", "description": "Distance considered safe."},
                "state": {
                    "type": "string",
                    "enum": ["idle", "patrol", "chase", "attack", "flee", "return"],
                    "description": "AI state to set.",
                },
                "pointPath": {
                    "type": "string",
                    "description": "Path to GameObject for patrol point.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Position for new patrol point.",
                },
            },
        },
        ["operation"],
    )


def gamekit_collectible_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_collectible MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "collect",
                        "respawn",
                        "reset",
                        "findByCollectibleId",
                    ],
                    "description": "Collectible operation to perform.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "collectibleId": {
                    "type": "string",
                    "description": "Unique collectible identifier.",
                },
                "name": {"type": "string", "description": "Name for new collectible GameObject."},
                "collectibleType": {
                    "type": "string",
                    "enum": [
                        "coin",
                        "health",
                        "mana",
                        "powerup",
                        "key",
                        "ammo",
                        "experience",
                        "custom",
                    ],
                    "description": "Type of collectible item.",
                },
                "customTypeName": {
                    "type": "string",
                    "description": "Custom type name for 'custom' collectibleType.",
                },
                "value": {"type": "number", "description": "Float value of collectible."},
                "intValue": {"type": "integer", "description": "Integer value of collectible."},
                "collectionBehavior": {
                    "type": "string",
                    "enum": ["destroy", "disable", "respawn"],
                    "description": "What happens when collected.",
                },
                "respawnDelay": {"type": "number", "description": "Respawn delay in seconds."},
                "collectable": {
                    "type": "boolean",
                    "description": "Whether the item can be collected.",
                },
                "requiredTag": {"type": "string", "description": "Required tag for collector."},
                "is2D": {"type": "boolean", "description": "Use 2D collider instead of 3D."},
                "colliderRadius": {"type": "number", "description": "Collider radius."},
                "enableFloatAnimation": {
                    "type": "boolean",
                    "description": "Enable floating animation.",
                },
                "floatAmplitude": {"type": "number", "description": "Float animation amplitude."},
                "floatFrequency": {"type": "number", "description": "Float animation frequency."},
                "enableRotation": {"type": "boolean", "description": "Enable rotation animation."},
                "rotationSpeed": {
                    "type": "number",
                    "description": "Rotation speed in degrees per second.",
                },
                "rotationAxis": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Rotation axis.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Position for new collectible.",
                },
                "deleteGameObject": {
                    "type": "boolean",
                    "description": "Delete entire GameObject (not just component).",
                },
            },
        },
        ["operation"],
    )


def gamekit_projectile_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_projectile MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "launch",
                        "setHomingTarget",
                        "destroy",
                        "findByProjectileId",
                    ],
                    "description": "Projectile operation to perform.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "projectileId": {"type": "string", "description": "Unique projectile identifier."},
                "name": {"type": "string", "description": "Name for new projectile GameObject."},
                "movementType": {
                    "type": "string",
                    "enum": ["transform", "rigidbody", "rigidbody2d"],
                    "description": "Movement physics type.",
                },
                "speed": {"type": "number", "description": "Projectile speed."},
                "damage": {"type": "number", "description": "Damage dealt on hit."},
                "lifetime": {"type": "number", "description": "Time before auto-destroy."},
                "useGravity": {"type": "boolean", "description": "Apply gravity to projectile."},
                "gravityScale": {"type": "number", "description": "Gravity scale multiplier."},
                "damageOnHit": {"type": "boolean", "description": "Apply damage on collision."},
                "targetTag": {"type": "string", "description": "Tag of valid targets."},
                "canBounce": {"type": "boolean", "description": "Allow bouncing off surfaces."},
                "maxBounces": {"type": "integer", "description": "Maximum bounce count."},
                "bounciness": {"type": "number", "description": "Bounce velocity retention (0-1)."},
                "isHoming": {"type": "boolean", "description": "Enable homing behavior."},
                "homingTargetPath": {"type": "string", "description": "Path to homing target."},
                "homingStrength": {"type": "number", "description": "Homing turning strength."},
                "maxHomingAngle": {"type": "number", "description": "Max homing angle in degrees."},
                "canPierce": {"type": "boolean", "description": "Pass through targets."},
                "maxPierceCount": {"type": "integer", "description": "Maximum pierce count."},
                "pierceDamageReduction": {
                    "type": "number",
                    "description": "Damage reduction per pierce (0-1).",
                },
                "direction": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Launch direction.",
                },
                "targetPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Target position to launch at.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Initial position.",
                },
                "isTrigger": {"type": "boolean", "description": "Use trigger collider."},
                "colliderRadius": {"type": "number", "description": "Collider radius."},
                "deleteGameObject": {"type": "boolean", "description": "Delete entire GameObject."},
            },
        },
        ["operation"],
    )


def gamekit_waypoint_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_waypoint MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "addWaypoint",
                        "removeWaypoint",
                        "clearWaypoints",
                        "startPath",
                        "stopPath",
                        "pausePath",
                        "resumePath",
                        "resetPath",
                        "goToWaypoint",
                        "findByWaypointId",
                    ],
                    "description": "Waypoint operation to perform.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "waypointId": {
                    "type": "string",
                    "description": "Unique waypoint follower identifier.",
                },
                "name": {
                    "type": "string",
                    "description": "Name for new waypoint follower GameObject.",
                },
                "pathMode": {
                    "type": "string",
                    "enum": ["once", "loop", "pingpong"],
                    "description": "Path traversal mode.",
                },
                "movementType": {
                    "type": "string",
                    "enum": ["transform", "rigidbody", "rigidbody2d"],
                    "description": "Movement physics type.",
                },
                "moveSpeed": {"type": "number", "description": "Movement speed."},
                "rotationSpeed": {"type": "number", "description": "Rotation speed."},
                "rotationMode": {
                    "type": "string",
                    "enum": ["none", "lookattarget", "aligntopath"],
                    "description": "Rotation behavior.",
                },
                "autoStart": {"type": "boolean", "description": "Start moving automatically."},
                "waitTimeAtPoint": {"type": "number", "description": "Wait time at each waypoint."},
                "startDelay": {"type": "number", "description": "Delay before starting path."},
                "smoothMovement": {"type": "boolean", "description": "Use smooth movement."},
                "smoothTime": {"type": "number", "description": "Smoothing time."},
                "arrivalThreshold": {
                    "type": "number",
                    "description": "Distance threshold for arrival.",
                },
                "useLocalSpace": {"type": "boolean", "description": "Use local coordinates."},
                "waypointPositions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "x": {"type": "number"},
                            "y": {"type": "number"},
                            "z": {"type": "number"},
                        },
                    },
                    "description": "Initial waypoint positions.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Position for addWaypoint or initial position.",
                },
                "index": {"type": "integer", "description": "Waypoint index for operations."},
                "deleteGameObject": {"type": "boolean", "description": "Delete entire GameObject."},
                "deleteWaypointChildren": {
                    "type": "boolean",
                    "description": "Delete waypoint child objects.",
                },
            },
        },
        ["operation"],
    )


def gamekit_trigger_zone_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_trigger_zone MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "activate",
                        "deactivate",
                        "reset",
                        "setTeleportDestination",
                        "findByZoneId",
                    ],
                    "description": "Trigger zone operation to perform.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "zoneId": {"type": "string", "description": "Unique trigger zone identifier."},
                "name": {"type": "string", "description": "Name for new trigger zone GameObject."},
                "zoneType": {
                    "type": "string",
                    "enum": [
                        "generic",
                        "checkpoint",
                        "damagezone",
                        "healzone",
                        "teleport",
                        "speedboost",
                        "slowdown",
                        "killzone",
                        "safezone",
                        "trigger",
                    ],
                    "description": "Type of trigger zone.",
                },
                "triggerMode": {
                    "type": "string",
                    "enum": ["once", "onceperentity", "repeat", "whileinside"],
                    "description": "Trigger activation mode.",
                },
                "isActive": {"type": "boolean", "description": "Whether zone is active."},
                "requiredTag": {"type": "string", "description": "Required tag for triggering."},
                "cooldown": {"type": "number", "description": "Cooldown between triggers."},
                "maxTriggerCount": {
                    "type": "integer",
                    "description": "Maximum trigger count (0 = unlimited).",
                },
                "effectAmount": {
                    "type": "number",
                    "description": "Damage/heal amount for DamageZone/HealZone.",
                },
                "effectInterval": {
                    "type": "number",
                    "description": "Effect interval for WhileInside mode.",
                },
                "speedMultiplier": {
                    "type": "number",
                    "description": "Speed multiplier for SpeedBoost/SlowDown.",
                },
                "checkpointIndex": {
                    "type": "integer",
                    "description": "Checkpoint index for ordering.",
                },
                "destinationPath": {
                    "type": "string",
                    "description": "Teleport destination GameObject path.",
                },
                "destinationPosition": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Teleport destination position.",
                },
                "is2D": {"type": "boolean", "description": "Use 2D colliders."},
                "colliderShape": {
                    "type": "string",
                    "enum": ["box", "sphere", "circle", "capsule"],
                    "description": "Collider shape.",
                },
                "colliderSize": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Collider size.",
                },
                "showGizmo": {"type": "boolean", "description": "Show editor gizmo."},
                "gizmoColor": {
                    "type": "object",
                    "properties": {
                        "r": {"type": "number"},
                        "g": {"type": "number"},
                        "b": {"type": "number"},
                        "a": {"type": "number"},
                    },
                    "description": "Gizmo color (RGBA 0-1).",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Initial position.",
                },
                "deleteGameObject": {"type": "boolean", "description": "Delete entire GameObject."},
            },
        },
        ["operation"],
    )


def gamekit_animation_sync_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_animation_sync MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "addSyncRule",
                        "removeSyncRule",
                        "addTriggerRule",
                        "removeTriggerRule",
                        "fireTrigger",
                        "setParameter",
                        "findBySyncId",
                    ],
                    "description": "Animation sync operation to perform.",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject hierarchy path.",
                },
                "syncId": {"type": "string", "description": "Unique animation sync identifier."},
                "animatorPath": {
                    "type": "string",
                    "description": "Path to GameObject with Animator component.",
                },
                "autoFindAnimator": {
                    "type": "boolean",
                    "description": "Auto-find Animator on same GameObject.",
                },
                "syncRules": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "parameter": {
                                "type": "string",
                                "description": "Animator parameter name.",
                            },
                            "parameterType": {
                                "type": "string",
                                "enum": ["float", "int", "bool"],
                                "description": "Parameter type.",
                            },
                            "sourceType": {
                                "type": "string",
                                "enum": [
                                    "rigidbody3d",
                                    "rigidbody2d",
                                    "transform",
                                    "health",
                                    "custom",
                                ],
                                "description": "Value source type.",
                            },
                            "sourceProperty": {
                                "type": "string",
                                "description": "Property to read (e.g., 'velocity.magnitude', 'position.y').",
                            },
                            "healthId": {
                                "type": "string",
                                "description": "Health ID when sourceType is 'health'.",
                            },
                            "multiplier": {
                                "type": "number",
                                "description": "Value multiplier (default: 1.0).",
                            },
                            "boolThreshold": {
                                "type": "number",
                                "description": "Threshold for bool parameters.",
                            },
                        },
                    },
                    "description": "Sync rules for animator parameters.",
                },
                "triggers": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "triggerName": {
                                "type": "string",
                                "description": "Animator trigger name.",
                            },
                            "eventSource": {
                                "type": "string",
                                "enum": ["health", "input", "manual"],
                                "description": "Event source type.",
                            },
                            "inputAction": {"type": "string", "description": "Input action name."},
                            "healthId": {"type": "string", "description": "Health component ID."},
                            "healthEvent": {
                                "type": "string",
                                "enum": [
                                    "OnDamaged",
                                    "OnHealed",
                                    "OnDeath",
                                    "OnRespawn",
                                    "OnInvincibilityStart",
                                    "OnInvincibilityEnd",
                                ],
                                "description": "Health event type.",
                            },
                        },
                    },
                    "description": "Trigger rules for animator triggers.",
                },
                "rule": {
                    "type": "object",
                    "properties": {
                        "parameter": {"type": "string"},
                        "parameterType": {"type": "string"},
                        "sourceType": {"type": "string"},
                        "sourceProperty": {"type": "string"},
                        "healthId": {"type": "string"},
                        "multiplier": {"type": "number"},
                        "boolThreshold": {"type": "number"},
                    },
                    "description": "Single sync rule for addSyncRule operation.",
                },
                "trigger": {
                    "type": "object",
                    "properties": {
                        "triggerName": {"type": "string"},
                        "eventSource": {"type": "string"},
                        "inputAction": {"type": "string"},
                        "healthId": {"type": "string"},
                        "healthEvent": {"type": "string"},
                    },
                    "description": "Single trigger rule for addTriggerRule operation.",
                },
                "parameterName": {
                    "type": "string",
                    "description": "Parameter/trigger name for remove/set operations.",
                },
                "triggerName": {"type": "string", "description": "Trigger name to fire."},
                "value": {"type": "number", "description": "Value for setParameter operation."},
            },
        },
        ["operation"],
    )


def gamekit_effect_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_effect MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "addComponent",
                        "removeComponent",
                        "clearComponents",
                        "play",
                        "playAtPosition",
                        "playAtTransform",
                        "shakeCamera",
                        "flashScreen",
                        "setTimeScale",
                        "createManager",
                        "registerEffect",
                        "unregisterEffect",
                        "findByEffectId",
                        "listEffects",
                    ],
                    "description": "Effect operation to perform.",
                },
                "effectId": {"type": "string", "description": "Unique effect identifier."},
                "assetPath": {
                    "type": "string",
                    "description": "Effect asset path (e.g., 'Assets/Effects/HitEffect.asset').",
                },
                "targetPath": {
                    "type": "string",
                    "description": "Target GameObject path for playAtTransform.",
                },
                "managerPath": {
                    "type": "string",
                    "description": "Path to EffectManager GameObject.",
                },
                "newEffectId": {
                    "type": "string",
                    "description": "New effect ID for update operation.",
                },
                "components": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {
                                "type": "string",
                                "enum": [
                                    "particle",
                                    "sound",
                                    "cameraShake",
                                    "screenFlash",
                                    "timeScale",
                                ],
                                "description": "Effect component type.",
                            },
                            "prefabPath": {
                                "type": "string",
                                "description": "Particle prefab path.",
                            },
                            "duration": {"type": "number", "description": "Effect duration."},
                            "attachToTarget": {
                                "type": "boolean",
                                "description": "Attach particle to target.",
                            },
                            "positionOffset": {
                                "type": "object",
                                "properties": {
                                    "x": {"type": "number"},
                                    "y": {"type": "number"},
                                    "z": {"type": "number"},
                                },
                            },
                            "particleScale": {
                                "type": "number",
                                "description": "Particle scale multiplier.",
                            },
                            "clipPath": {"type": "string", "description": "Audio clip path."},
                            "volume": {"type": "number", "description": "Audio volume (0-1)."},
                            "pitchVariation": {
                                "type": "number",
                                "description": "Pitch variation range.",
                            },
                            "spatialBlend": {
                                "type": "number",
                                "description": "3D spatial blend (0=2D, 1=3D).",
                            },
                            "intensity": {
                                "type": "number",
                                "description": "Camera shake intensity.",
                            },
                            "shakeDuration": {
                                "type": "number",
                                "description": "Camera shake duration.",
                            },
                            "frequency": {
                                "type": "number",
                                "description": "Camera shake frequency.",
                            },
                            "color": {
                                "type": "object",
                                "properties": {
                                    "r": {"type": "number"},
                                    "g": {"type": "number"},
                                    "b": {"type": "number"},
                                    "a": {"type": "number"},
                                },
                                "description": "Flash color.",
                            },
                            "flashDuration": {
                                "type": "number",
                                "description": "Screen flash duration.",
                            },
                            "fadeTime": {"type": "number", "description": "Flash fade time."},
                            "targetTimeScale": {
                                "type": "number",
                                "description": "Target time scale for slow-mo.",
                            },
                            "timeScaleDuration": {
                                "type": "number",
                                "description": "Time scale effect duration.",
                            },
                            "timeScaleTransition": {
                                "type": "number",
                                "description": "Time scale transition time.",
                            },
                        },
                    },
                    "description": "Effect components for create operation.",
                },
                "component": {
                    "type": "object",
                    "description": "Single effect component for addComponent operation.",
                },
                "componentIndex": {
                    "type": "integer",
                    "description": "Component index for removeComponent.",
                },
                "position": {
                    "type": "object",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"},
                    },
                    "description": "Position for play operation.",
                },
                "persistent": {
                    "type": "boolean",
                    "description": "Manager persists across scenes (DontDestroyOnLoad).",
                },
            },
        },
        ["operation"],
    )


def gamekit_save_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_save MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createProfile",
                        "updateProfile",
                        "inspectProfile",
                        "deleteProfile",
                        "addTarget",
                        "removeTarget",
                        "clearTargets",
                        "save",
                        "load",
                        "listSlots",
                        "deleteSlot",
                        "createManager",
                        "inspectManager",
                        "deleteManager",
                        "findByProfileId",
                    ],
                    "description": "Save system operation.",
                },
                "profileId": {"type": "string", "description": "Save profile identifier."},
                "assetPath": {
                    "type": "string",
                    "description": "Asset path for profile (e.g., 'Assets/GameKit/SaveProfiles/MainSave.asset').",
                },
                "targetPath": {"type": "string", "description": "GameObject path for manager."},
                "slotId": {
                    "type": "string",
                    "description": "Save slot identifier for save/load operations.",
                },
                "saveTargets": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "type": {
                                "type": "string",
                                "enum": [
                                    "transform",
                                    "component",
                                    "resourceManager",
                                    "health",
                                    "sceneFlow",
                                    "inventory",
                                    "playerPrefs",
                                ],
                                "description": "Type of data to save.",
                            },
                            "saveKey": {
                                "type": "string",
                                "description": "Unique key for this save data.",
                            },
                            "gameObjectPath": {
                                "type": "string",
                                "description": "GameObject path for transform/component saves.",
                            },
                            "savePosition": {
                                "type": "boolean",
                                "description": "Save position (for transform type).",
                            },
                            "saveRotation": {
                                "type": "boolean",
                                "description": "Save rotation (for transform type).",
                            },
                            "saveScale": {
                                "type": "boolean",
                                "description": "Save scale (for transform type).",
                            },
                            "componentType": {
                                "type": "string",
                                "description": "Component type name (for component type).",
                            },
                            "properties": {
                                "type": "array",
                                "items": {"type": "string"},
                                "description": "Properties to save from component.",
                            },
                            "resourceManagerId": {
                                "type": "string",
                                "description": "ResourceManager ID (for resourceManager type).",
                            },
                            "healthId": {
                                "type": "string",
                                "description": "Health ID (for health type).",
                            },
                            "sceneFlowId": {
                                "type": "string",
                                "description": "SceneFlow ID (for sceneFlow type).",
                            },
                            "inventoryId": {
                                "type": "string",
                                "description": "Inventory ID (for inventory type).",
                            },
                        },
                    },
                    "description": "Save targets for createProfile operation.",
                },
                "target": {
                    "type": "object",
                    "description": "Single save target for addTarget operation.",
                },
                "saveKey": {
                    "type": "string",
                    "description": "Save key for removeTarget operation.",
                },
                "autoSave": {
                    "type": "object",
                    "properties": {
                        "enabled": {"type": "boolean", "description": "Enable auto-save."},
                        "intervalSeconds": {
                            "type": "number",
                            "description": "Auto-save interval in seconds.",
                        },
                        "onSceneChange": {
                            "type": "boolean",
                            "description": "Auto-save on scene change.",
                        },
                        "onApplicationPause": {
                            "type": "boolean",
                            "description": "Auto-save on application pause.",
                        },
                        "autoSaveSlotId": {
                            "type": "string",
                            "description": "Slot ID for auto-save.",
                        },
                    },
                    "description": "Auto-save configuration.",
                },
            },
        },
        ["operation"],
    )


def gamekit_inventory_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_inventory MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "defineItem",
                        "updateItem",
                        "inspectItem",
                        "deleteItem",
                        "addItem",
                        "removeItem",
                        "useItem",
                        "equip",
                        "unequip",
                        "getEquipped",
                        "clear",
                        "sort",
                        "findByInventoryId",
                        "findByItemId",
                    ],
                    "description": "Inventory operation.",
                },
                "inventoryId": {"type": "string", "description": "Inventory identifier."},
                "gameObjectPath": {
                    "type": "string",
                    "description": "GameObject path for inventory component.",
                },
                "maxSlots": {
                    "type": "integer",
                    "description": "Maximum inventory slots (default: 20).",
                },
                "categories": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Allowed item categories (e.g., ['weapon', 'armor', 'consumable']).",
                },
                "stackableCategories": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Categories that allow stacking.",
                },
                "maxStackSize": {
                    "type": "integer",
                    "description": "Default max stack size (default: 99).",
                },
                "itemId": {"type": "string", "description": "Item identifier."},
                "assetPath": {
                    "type": "string",
                    "description": "Asset path for item (e.g., 'Assets/GameKit/Items/HealthPotion.asset').",
                },
                "quantity": {
                    "type": "integer",
                    "description": "Quantity to add/remove (default: 1).",
                },
                "slotIndex": {
                    "type": "integer",
                    "description": "Slot index for useItem/equip operations.",
                },
                "equipSlot": {
                    "type": "string",
                    "description": "Equipment slot (mainHand/offHand/head/body/hands/feet/accessory1/accessory2).",
                },
                "displayName": {"type": "string", "description": "Item display name."},
                "description": {"type": "string", "description": "Item description."},
                "category": {
                    "type": "string",
                    "description": "Item category (weapon/armor/consumable/material/key/quest/misc).",
                },
                "itemData": {
                    "type": "object",
                    "properties": {
                        "displayName": {"type": "string", "description": "Item display name."},
                        "description": {"type": "string", "description": "Item description."},
                        "category": {"type": "string", "description": "Item category."},
                        "stackable": {"type": "boolean", "description": "Can items stack."},
                        "maxStack": {"type": "integer", "description": "Max stack size."},
                        "buyPrice": {"type": "integer", "description": "Buy price."},
                        "sellPrice": {"type": "integer", "description": "Sell price."},
                        "equippable": {"type": "boolean", "description": "Can item be equipped."},
                        "equipSlot": {
                            "type": "string",
                            "description": "Equipment slot for equippable items.",
                        },
                        "equipStats": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "statName": {"type": "string"},
                                    "modifierType": {
                                        "type": "string",
                                        "enum": ["flat", "percentAdd", "percentMultiply"],
                                    },
                                    "value": {"type": "number"},
                                },
                            },
                            "description": "Stat modifiers when equipped.",
                        },
                        "onUse": {
                            "type": "object",
                            "properties": {
                                "type": {
                                    "type": "string",
                                    "enum": ["none", "heal", "addResource", "playEffect", "custom"],
                                    "description": "Use action type.",
                                },
                                "healthId": {
                                    "type": "string",
                                    "description": "Health ID for heal action.",
                                },
                                "amount": {
                                    "type": "number",
                                    "description": "Heal/resource amount.",
                                },
                                "resourceManagerId": {
                                    "type": "string",
                                    "description": "ResourceManager ID.",
                                },
                                "resourceName": {
                                    "type": "string",
                                    "description": "Resource name to add.",
                                },
                                "resourceAmount": {
                                    "type": "number",
                                    "description": "Resource amount.",
                                },
                                "effectId": {"type": "string", "description": "Effect ID to play."},
                                "consumeOnUse": {
                                    "type": "boolean",
                                    "description": "Consume item on use (default: true).",
                                },
                            },
                            "description": "Action to perform when item is used.",
                        },
                    },
                    "description": "Item data for defineItem operation.",
                },
            },
        },
        ["operation"],
    )


def gamekit_dialogue_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_dialogue MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createDialogue",
                        "updateDialogue",
                        "inspectDialogue",
                        "deleteDialogue",
                        "addNode",
                        "updateNode",
                        "removeNode",
                        "addChoice",
                        "updateChoice",
                        "removeChoice",
                        "startDialogue",
                        "selectChoice",
                        "advanceDialogue",
                        "endDialogue",
                        "createManager",
                        "inspectManager",
                        "deleteManager",
                        "findByDialogueId",
                    ],
                    "description": "Dialogue operation.",
                },
                "dialogueId": {"type": "string", "description": "Dialogue identifier."},
                "assetPath": {
                    "type": "string",
                    "description": "Asset path for dialogue (e.g., 'Assets/Dialogues/NPC_Greeting.asset').",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "GameObject path for dialogue manager.",
                },
                "managerId": {"type": "string", "description": "Dialogue manager identifier."},
                "displayName": {"type": "string", "description": "Dialogue display name."},
                "description": {"type": "string", "description": "Dialogue description."},
                "nodeId": {"type": "string", "description": "Node identifier."},
                "choiceId": {"type": "string", "description": "Choice identifier."},
                "choiceIndex": {
                    "type": "integer",
                    "description": "Choice index for selectChoice operation.",
                },
                "nodeData": {
                    "type": "object",
                    "properties": {
                        "nodeId": {"type": "string", "description": "Node ID."},
                        "nodeType": {
                            "type": "string",
                            "enum": ["dialogue", "choice", "branch", "action", "exit"],
                            "description": "Node type.",
                        },
                        "speakerName": {
                            "type": "string",
                            "description": "Speaker name for dialogue nodes.",
                        },
                        "dialogueText": {"type": "string", "description": "Dialogue text content."},
                        "nextNodeId": {"type": "string", "description": "Next node ID."},
                        "delaySeconds": {
                            "type": "number",
                            "description": "Delay before auto-advancing.",
                        },
                    },
                    "description": "Node data for addNode/updateNode operations.",
                },
                "choiceData": {
                    "type": "object",
                    "properties": {
                        "choiceId": {"type": "string", "description": "Choice ID."},
                        "choiceText": {"type": "string", "description": "Choice display text."},
                        "targetNodeId": {
                            "type": "string",
                            "description": "Target node when selected.",
                        },
                        "conditions": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "type": {
                                        "type": "string",
                                        "enum": [
                                            "quest",
                                            "resource",
                                            "inventory",
                                            "variable",
                                            "health",
                                            "custom",
                                        ],
                                    },
                                    "questId": {"type": "string"},
                                    "questState": {"type": "string"},
                                    "resourceManagerId": {"type": "string"},
                                    "resourceName": {"type": "string"},
                                    "comparison": {
                                        "type": "string",
                                        "enum": [
                                            "greaterThan",
                                            "lessThan",
                                            "equalTo",
                                            "greaterOrEqual",
                                            "lessOrEqual",
                                            "notEqual",
                                        ],
                                    },
                                    "value": {"type": "number"},
                                },
                            },
                            "description": "Conditions for this choice to be available.",
                        },
                    },
                    "description": "Choice data for addChoice/updateChoice operations.",
                },
            },
        },
        ["operation"],
    )


def gamekit_quest_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_quest MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "createQuest",
                        "updateQuest",
                        "inspectQuest",
                        "deleteQuest",
                        "addObjective",
                        "updateObjective",
                        "removeObjective",
                        "addPrerequisite",
                        "removePrerequisite",
                        "addReward",
                        "removeReward",
                        "startQuest",
                        "completeQuest",
                        "failQuest",
                        "abandonQuest",
                        "updateProgress",
                        "listQuests",
                        "createManager",
                        "inspectManager",
                        "deleteManager",
                        "findByQuestId",
                    ],
                    "description": "Quest operation.",
                },
                "questId": {"type": "string", "description": "Quest identifier."},
                "assetPath": {
                    "type": "string",
                    "description": "Asset path for quest (e.g., 'Assets/Quests/MainQuest_01.asset').",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "GameObject path for quest manager.",
                },
                "managerId": {"type": "string", "description": "Quest manager identifier."},
                "displayName": {"type": "string", "description": "Quest display name."},
                "description": {"type": "string", "description": "Quest description."},
                "category": {
                    "type": "string",
                    "enum": [
                        "main",
                        "side",
                        "daily",
                        "weekly",
                        "event",
                        "tutorial",
                        "hidden",
                        "custom",
                    ],
                    "description": "Quest category.",
                },
                "objectiveId": {"type": "string", "description": "Objective identifier."},
                "progressAmount": {
                    "type": "integer",
                    "description": "Progress amount for updateProgress.",
                },
                "filter": {
                    "type": "string",
                    "enum": ["all", "active", "completed", "failed", "available"],
                    "description": "Filter for listQuests.",
                },
                "objectiveData": {
                    "type": "object",
                    "properties": {
                        "objectiveId": {"type": "string", "description": "Objective ID."},
                        "objectiveType": {
                            "type": "string",
                            "enum": [
                                "kill",
                                "collect",
                                "talk",
                                "location",
                                "interact",
                                "escort",
                                "defend",
                                "deliver",
                                "explore",
                                "craft",
                                "custom",
                            ],
                            "description": "Objective type.",
                        },
                        "description": {"type": "string", "description": "Objective description."},
                        "targetId": {
                            "type": "string",
                            "description": "Target ID (enemy type, item ID, NPC ID, etc.).",
                        },
                        "requiredAmount": {
                            "type": "integer",
                            "description": "Required amount to complete.",
                        },
                        "isOptional": {
                            "type": "boolean",
                            "description": "Whether objective is optional.",
                        },
                        "isSilent": {
                            "type": "boolean",
                            "description": "Whether to hide objective from UI.",
                        },
                    },
                    "description": "Objective data for addObjective/updateObjective operations.",
                },
                "rewardData": {
                    "type": "object",
                    "properties": {
                        "rewardId": {"type": "string", "description": "Reward ID."},
                        "rewardType": {
                            "type": "string",
                            "enum": [
                                "resource",
                                "item",
                                "experience",
                                "reputation",
                                "unlock",
                                "dialogue",
                                "custom",
                            ],
                            "description": "Reward type.",
                        },
                        "resourceManagerId": {
                            "type": "string",
                            "description": "ResourceManager ID for resource rewards.",
                        },
                        "resourceName": {"type": "string", "description": "Resource name."},
                        "amount": {"type": "number", "description": "Reward amount."},
                        "itemId": {"type": "string", "description": "Item ID for item rewards."},
                        "quantity": {"type": "integer", "description": "Item quantity."},
                    },
                    "description": "Reward data for addReward operation.",
                },
                "prerequisiteData": {
                    "type": "object",
                    "properties": {
                        "prerequisiteId": {"type": "string", "description": "Prerequisite ID."},
                        "type": {
                            "type": "string",
                            "enum": [
                                "questComplete",
                                "questActive",
                                "level",
                                "resource",
                                "item",
                                "reputation",
                                "custom",
                            ],
                            "description": "Prerequisite type.",
                        },
                        "questId": {
                            "type": "string",
                            "description": "Quest ID for quest-based prerequisites.",
                        },
                        "resourceManagerId": {
                            "type": "string",
                            "description": "ResourceManager ID for resource prerequisites.",
                        },
                        "resourceName": {"type": "string", "description": "Resource name."},
                        "requiredValue": {"type": "number", "description": "Required value."},
                    },
                    "description": "Prerequisite data for addPrerequisite operation.",
                },
            },
        },
        ["operation"],
    )


def gamekit_status_effect_schema() -> dict[str, Any]:
    """Schema for the unity_gamekit_status_effect MCP tool."""
    return schema_with_required(
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": [
                        "defineEffect",
                        "updateEffect",
                        "inspectEffect",
                        "deleteEffect",
                        "addModifier",
                        "updateModifier",
                        "removeModifier",
                        "clearModifiers",
                        "create",
                        "update",
                        "inspect",
                        "delete",
                        "applyEffect",
                        "removeEffect",
                        "clearEffects",
                        "getActiveEffects",
                        "getStatModifier",
                        "findByEffectId",
                        "findByReceiverId",
                        "listEffects",
                    ],
                    "description": "Status effect operation.",
                },
                "effectId": {"type": "string", "description": "Effect identifier."},
                "receiverId": {"type": "string", "description": "Receiver component identifier."},
                "assetPath": {
                    "type": "string",
                    "description": "Asset path for effect (e.g., 'Assets/Effects/Poison.asset').",
                },
                "gameObjectPath": {
                    "type": "string",
                    "description": "GameObject path for receiver component.",
                },
                "displayName": {"type": "string", "description": "Effect display name."},
                "description": {"type": "string", "description": "Effect description."},
                "effectType": {
                    "type": "string",
                    "enum": ["buff", "debuff", "neutral"],
                    "description": "Effect type.",
                },
                "category": {
                    "type": "string",
                    "enum": [
                        "generic",
                        "poison",
                        "burn",
                        "freeze",
                        "stun",
                        "slow",
                        "haste",
                        "shield",
                        "regeneration",
                        "invincibility",
                        "weakness",
                        "strength",
                        "custom",
                    ],
                    "description": "Effect category.",
                },
                "duration": {"type": "number", "description": "Effect duration in seconds."},
                "isPermanent": {"type": "boolean", "description": "Whether effect is permanent."},
                "stackable": {"type": "boolean", "description": "Whether effect can stack."},
                "maxStacks": {"type": "integer", "description": "Maximum stacks."},
                "stackBehavior": {
                    "type": "string",
                    "enum": ["refreshDuration", "addDuration", "independent", "increaseStacks"],
                    "description": "Behavior when effect is applied while active.",
                },
                "tickInterval": {"type": "number", "description": "Tick interval in seconds."},
                "tickOnApply": {
                    "type": "boolean",
                    "description": "Whether to tick immediately on apply.",
                },
                "stacks": {"type": "integer", "description": "Number of stacks for applyEffect."},
                "modifierId": {"type": "string", "description": "Modifier identifier."},
                "statName": {"type": "string", "description": "Stat name for getStatModifier."},
                "modifierData": {
                    "type": "object",
                    "properties": {
                        "modifierId": {"type": "string", "description": "Modifier ID."},
                        "type": {
                            "type": "string",
                            "enum": [
                                "statModifier",
                                "damageOverTime",
                                "healOverTime",
                                "stun",
                                "silence",
                                "invincible",
                                "custom",
                            ],
                            "description": "Modifier type.",
                        },
                        "targetHealthId": {
                            "type": "string",
                            "description": "Target health component ID.",
                        },
                        "targetStat": {"type": "string", "description": "Target stat name."},
                        "value": {"type": "number", "description": "Modifier value."},
                        "operation": {
                            "type": "string",
                            "enum": [
                                "add",
                                "subtract",
                                "multiply",
                                "divide",
                                "set",
                                "percentAdd",
                                "percentMultiply",
                            ],
                            "description": "Modifier operation.",
                        },
                        "scaleWithStacks": {
                            "type": "boolean",
                            "description": "Scale value with stacks.",
                        },
                        "damagePerTick": {
                            "type": "number",
                            "description": "Damage per tick for DoT.",
                        },
                        "healPerTick": {"type": "number", "description": "Heal per tick for HoT."},
                        "damageType": {
                            "type": "string",
                            "enum": [
                                "physical",
                                "magic",
                                "fire",
                                "ice",
                                "lightning",
                                "poison",
                                "true",
                            ],
                            "description": "Damage type.",
                        },
                    },
                    "description": "Modifier data for addModifier/updateModifier operations.",
                },
                "effectData": {
                    "type": "object",
                    "properties": {
                        "displayName": {"type": "string"},
                        "description": {"type": "string"},
                        "effectType": {"type": "string", "enum": ["buff", "debuff", "neutral"]},
                        "category": {"type": "string"},
                        "duration": {"type": "number"},
                        "isPermanent": {"type": "boolean"},
                        "stackable": {"type": "boolean"},
                        "maxStacks": {"type": "integer"},
                        "stackBehavior": {"type": "string"},
                        "tickInterval": {"type": "number"},
                        "tickOnApply": {"type": "boolean"},
                        "particleEffectId": {"type": "string"},
                        "onApplyEffectId": {"type": "string"},
                        "onRemoveEffectId": {"type": "string"},
                        "onTickEffectId": {"type": "string"},
                    },
                    "description": "Effect data for defineEffect/updateEffect operations.",
                },
            },
        },
        ["operation"],
    )
