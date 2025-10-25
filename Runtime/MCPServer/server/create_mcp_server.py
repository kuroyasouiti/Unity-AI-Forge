from __future__ import annotations

from mcp.server import Server

from ..resources.register_resources import register_resources
from ..tools.register_tools import register_tools
from ..version import SERVER_NAME, SERVER_VERSION


def create_mcp_server() -> Server:
    server = Server(
        SERVER_NAME,
        version=SERVER_VERSION,
        instructions="\n".join(
            [
                "Unityプロジェクトと連携する拡張版MCPサーバーです。",
                "resourcesからプロジェクト構造、シーン、ログ、アセットを参照し、toolsでUnityEditorを操作します。",
                "",
                "【ツール使用の原則】",
                "- UnityEditor操作は必ずMCPツール（unity.asset.crud、unity.gameobject.crud、unity.component.crud等）を使用する事",
                "- パラメータ取得やファイル読み書きも直接行わず、MCPツールを使用する事",
                "- ツール実行前に対象のオブジェクト/アセットのパスを確認する事",
                "- 接続タイムアウト時はコンパイルエラーを確認する事",
                "",
                "【開発時の注意点】",
                "- 必要最小限の変更に留め、既存アセットを活用する事",
                "- ユーザーが明示的に要求した場合のみエディタ操作を実行する事",
                "",
                "【開発作業の優先順位】",
                "1. C#スクリプトの作成（プロジェクトの基礎）",
                "2. GameObjectとコンポーネントの設定",
                "3. シーン構成とプレハブの作成",
                "",
                "【C#スクリプト作成ルール】",
                "- [SerializeField] private変数には、必ずpublic読み取り専用プロパティを作成",
                "  例: [SerializeField] private int health = 100; → public int Health => health;",
                "- 作成/更新後は必ずunity.project.compileでコンパイル実行",
                "- コンパイルエラー発生時はエラー内容を確認して修正し、再コンパイル",
            ]
        ),
    )

    register_resources(server)
    register_tools(server)

    return server
