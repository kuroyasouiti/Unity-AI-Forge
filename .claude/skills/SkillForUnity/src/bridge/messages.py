from __future__ import annotations

from typing import Any, Literal, NotRequired, TypedDict


class ComponentSummary(TypedDict, total=False):
    type: str
    enabled: bool
    serializedProperties: dict[str, Any]


class HierarchyNode(TypedDict, total=False):
    id: str
    name: str
    type: Literal["GameObject", "PrefabInstance", "UIElement", "Unknown"]
    components: list[ComponentSummary]
    children: list[HierarchyNode]


class UnityObjectReference(TypedDict, total=False):
    guid: NotRequired[str]
    path: NotRequired[str]
    name: str
    type: str


class AssetIndexEntry(TypedDict, total=False):
    guid: str
    path: str
    label: NotRequired[str]
    type: NotRequired[str]


class UnityContextPayload(TypedDict, total=False):
    activeScene: NotRequired[dict[str, str]]
    hierarchy: NotRequired[HierarchyNode]
    selection: NotRequired[list[UnityObjectReference]]
    assets: NotRequired[list[AssetIndexEntry]]
    gitDiffSummary: NotRequired[str]
    updatedAt: int


class ClientInfo(TypedDict, total=False):
    clientName: str  # "Claude Desktop", "Claude Code", "Unknown"
    clientVersion: NotRequired[str]
    serverName: str  # "SkillForUnity"
    serverVersion: str
    pythonVersion: str
    platform: str
    toolCount: NotRequired[int]


class BridgeHelloMessage(TypedDict, total=False):
    type: Literal["hello"]
    sessionId: str
    token: NotRequired[str]
    unityVersion: NotRequired[str]
    projectName: NotRequired[str]
    clientInfo: NotRequired[ClientInfo]


class BridgeHeartbeatMessage(TypedDict):
    type: Literal["heartbeat"]
    timestamp: int


class BridgeContextUpdateMessage(TypedDict):
    type: Literal["context:update"]
    payload: UnityContextPayload


class BridgeCommandResultMessage(TypedDict, total=False):
    type: Literal["command:result"]
    commandId: str
    ok: bool
    result: NotRequired[Any]
    errorMessage: NotRequired[str]


class BridgeRestartedMessage(TypedDict):
    type: Literal["bridge:restarted"]
    timestamp: int
    reason: str
    sessionId: str


BridgeNotificationMessage = (
    BridgeHelloMessage
    | BridgeHeartbeatMessage
    | BridgeContextUpdateMessage
    | BridgeCommandResultMessage
    | BridgeRestartedMessage
)


class ServerCommandMessage(TypedDict):
    type: Literal["command:execute"]
    commandId: str
    toolName: str
    payload: Any


class ServerPingMessage(TypedDict):
    type: Literal["ping"]
    timestamp: int


class ServerInfoMessage(TypedDict):
    type: Literal["server:info"]
    clientInfo: ClientInfo


ServerMessage = ServerCommandMessage | ServerPingMessage | ServerInfoMessage
