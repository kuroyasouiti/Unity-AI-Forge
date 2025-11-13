import tempfile
from pathlib import Path
from types import SimpleNamespace
import unittest
from unittest.mock import AsyncMock, patch

from MCPServer.resources import register_resources as resources_module


class _DummyServer:
    def __init__(self) -> None:
        self.list_resources_handler = None
        self.list_resource_templates_handler = None
        self.read_resource_handler = None

    def list_resources(self):
        def decorator(func):
            self.list_resources_handler = func
            return func

        return decorator

    def list_resource_templates(self):
        def decorator(func):
            self.list_resource_templates_handler = func
            return func

        return decorator

    def read_resource(self):
        def decorator(func):
            self.read_resource_handler = func
            return func

        return decorator


class RegisterResourcesTests(unittest.IsolatedAsyncioTestCase):
    def setUp(self) -> None:
        self.server = _DummyServer()
        resources_module.register_resources(self.server)
        self.list_handler = self.server.list_resources_handler
        self.read_handler = self.server.read_resource_handler
        self.templates_handler = self.server.list_resource_templates_handler

    async def test_list_resources_includes_assets_from_context(self) -> None:
        asset_context = {
            "assets": [
                {
                    "guid": "abc123",
                    "path": "Assets/Scripts/Foo.cs",
                    "label": "Foo Script",
                }
            ]
        }

        with patch.object(
            resources_module.bridge_manager,
            "get_context",
            return_value=asset_context,
        ):
            resources = await self.list_handler()

        self.assertIsInstance(resources, list)

        uris = [str(resource.uri) for resource in resources]
        self.assertIn("unity://project/structure", uris)
        self.assertIn("unity://editor/log", uris)
        self.assertIn("unity://scene/active", uris)
        self.assertIn("unity://asset/abc123", uris)

    async def test_list_resource_templates_exposes_asset_template(self) -> None:
        templates = await self.templates_handler()
        self.assertIsInstance(templates, list)
        template = templates[0]
        self.assertEqual(template.uriTemplate, "unity://asset/{guid}")

    async def test_read_project_structure_uses_summary_builder(self) -> None:
        with patch.object(
            resources_module,
            "build_project_structure_summary",
            new=AsyncMock(return_value="## summary"),
        ), patch.object(
            resources_module,
            "env",
            SimpleNamespace(unity_project_root=Path("dummy")),
        ):
            contents = await self.read_handler("unity://project/structure")

        self.assertEqual(len(contents), 1)
        self.assertEqual(contents[0].content, "## summary")

    async def test_read_editor_log_returns_snapshot_text(self) -> None:
        snapshot = SimpleNamespace(lines=["line1", "line2"])

        with patch.object(
            resources_module.editor_log_watcher, "get_snapshot", return_value=snapshot
        ):
            contents = await self.read_handler("unity://editor/log")

        self.assertEqual(len(contents), 1)
        self.assertEqual(contents[0].content, "line1\nline2")

    async def test_read_active_scene_renders_hierarchy(self) -> None:
        context = {
            "activeScene": {"name": "SampleScene", "path": "Assets/Scenes/Sample.unity"},
            "hierarchy": {
                "name": "Root",
                "type": "Scene",
                "components": [],
                "children": [
                    {
                        "name": "Player",
                        "type": "GameObject",
                        "components": [{"type": "Transform"}],
                        "children": [],
                    }
                ],
            },
        }

        with patch.object(
            resources_module.bridge_manager, "get_context", return_value=context
        ):
            contents = await self.read_handler("unity://scene/active")

        body = contents[0].content
        self.assertIn("SampleScene", body)
        self.assertIn("Player", body)

    async def test_read_asset_returns_file_contents(self) -> None:
        with tempfile.TemporaryDirectory() as tmp_dir:
            root = Path(tmp_dir)
            asset_rel_path = Path("Assets/Scripts/Foo.cs")
            full_path = root / asset_rel_path
            full_path.parent.mkdir(parents=True, exist_ok=True)
            full_path.write_text("class Foo {}", encoding="utf-8")

            context = {
                "assets": [
                    {"guid": "foo-guid", "path": str(asset_rel_path), "label": "Foo"},
                ]
            }

            with patch.object(
                resources_module.bridge_manager, "get_context", return_value=context
            ), patch.object(
                resources_module, "env", SimpleNamespace(unity_project_root=root)
            ):
                contents = await self.read_handler("unity://asset/foo-guid")

        self.assertEqual(len(contents), 1)
        self.assertEqual(contents[0].content, "class Foo {}")

    async def test_read_asset_missing_guid_returns_message(self) -> None:
        with patch.object(
            resources_module.bridge_manager, "get_context", return_value={"assets": []}
        ):
            contents = await self.read_handler("unity://asset/missing-guid")

        self.assertEqual(len(contents), 1)
        self.assertIn("No asset with GUID missing-guid", contents[0].content)


if __name__ == "__main__":
    unittest.main()
