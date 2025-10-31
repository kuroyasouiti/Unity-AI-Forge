import unittest

from MCPServer.bridge.bridge_connector import _build_ws_url


class BuildWsUrlTests(unittest.TestCase):
    def test_ipv4_host(self) -> None:
        self.assertEqual(
            _build_ws_url("127.0.0.1", 7070, "/bridge"),
            "ws://127.0.0.1:7070/bridge",
        )

    def test_ipv6_host(self) -> None:
        self.assertEqual(
            _build_ws_url("::1", 7070, "/bridge"),
            "ws://[::1]:7070/bridge",
        )

    def test_bracketed_ipv6_host(self) -> None:
        self.assertEqual(
            _build_ws_url("[::1]", 7070, "/bridge"),
            "ws://[::1]:7070/bridge",
        )

    def test_empty_host_defaults_to_loopback(self) -> None:
        self.assertEqual(
            _build_ws_url("", 7070, "/bridge"),
            "ws://127.0.0.1:7070/bridge",
        )

    def test_path_without_leading_slash(self) -> None:
        self.assertEqual(
            _build_ws_url("127.0.0.1", 7070, "bridge"),
            "ws://127.0.0.1:7070/bridge",
        )


if __name__ == "__main__":
    unittest.main()
