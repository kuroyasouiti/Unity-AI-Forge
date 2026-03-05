using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;

namespace MCP.Editor.Handlers.HighLevel
{
    /// <summary>
    /// Unified dispatcher for all GameKit UI widgets.
    /// Routes by <c>widgetType</c> (command / binding / list / slot / selection)
    /// to the corresponding sub-handler.
    /// </summary>
    public class GameKitUIHandler : BaseCommandHandler
    {
        // ── Union of all operations across 5 widget types ──────────────
        private static readonly string[] Operations =
        {
            // common CRUD
            "create", "inspect",
            // command
            "createCommandPanel", "addCommand",
            // binding
            "setRange", "refresh", "findByBindingId",
            // list
            "setItems", "addItem", "removeItem", "clear",
            "selectItem", "deselectItem", "clearSelection",
            "refreshFromSource", "findByListId",
            // slot
            "setItem", "clearSlot", "setHighlight",
            "createSlotBar", "inspectSlotBar",
            "useSlot", "refreshFromInventory",
            "findBySlotId", "findByBarId",
            // selection
            "selectItemById", "setSelectionActions", "setItemEnabled",
            "findBySelectionId"
        };

        // ── Sub-handlers (unchanged, used as internal delegates) ───────
        private readonly GameKitUICommandHandler _command = new();
        private readonly GameKitUIBindingHandler _binding = new();
        private readonly GameKitUIListHandler _list = new();
        private readonly GameKitUISlotHandler _slot = new();
        private readonly GameKitUISelectionHandler _selection = new();

        public override string Category => "gamekitUI";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation is "create" or "createCommandPanel" or "createSlotBar";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            var widgetType = GetString(payload, "widgetType");
            if (string.IsNullOrEmpty(widgetType))
                return CreateFailureResponse("'widgetType' is required. Use: command, binding, list, slot, selection");

            var sub = ResolveSubHandler(widgetType);
            if (sub == null)
                return CreateFailureResponse(
                    $"Unknown widgetType '{widgetType}'. Use: command, binding, list, slot, selection");

            if (!sub.SupportedOperations.Contains(operation))
                return CreateFailureResponse(
                    $"Operation '{operation}' is not supported by widgetType '{widgetType}'. " +
                    $"Supported: {string.Join(", ", sub.SupportedOperations)}");

            return sub.InvokeOperation(operation, payload);
        }

        private BaseCommandHandler ResolveSubHandler(string widgetType)
        {
            return widgetType switch
            {
                "command"   => _command,
                "binding"   => _binding,
                "list"      => _list,
                "slot"      => _slot,
                "selection" => _selection,
                _           => null
            };
        }
    }
}
