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
                "resourcesからプロジェクト構造、シーン、ログ、アセットを参照できます。",
                "toolsを利用する際はUnityブリッジの接続状態を確認してください。",
                "UnityEditorの操作が必要な場合は、必ずこのMCPサーバーのツールを使用してください。",
                "",
                "開発作業の優先順位:",
                "1. スクリプトの作成を最優先で行ってください。C#スクリプトはプロジェクトの基礎となります。",
                "2. スクリプト作成後、GameObjectやコンポーネントの設定を行ってください。",
                "3. 最後にシーンの構成やプレハブの作成を行ってください。",
                "",
                "重要: C#スクリプトを作成または更新した後は、必ずunity.project.compileツールを使用してコンパイルを実行してください。",
                "コンパイルエラーが発生した場合は、エラーメッセージを確認してスクリプトを修正し、再度コンパイルしてください。",
            ]
        ),
    )

    register_resources(server)
    register_tools(server)

    return server
