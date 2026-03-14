using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Unified dispatcher for animation tools.
    /// Routes 2D-specific operations (sprite clips) to <see cref="Animation2DBundleHandler"/>
    /// and all other operations to <see cref="Animation3DBundleHandler"/>.
    /// Normalizes operation names: 'addParameter' maps to 3D handler's 'setParameter',
    /// 'inspect' maps to 2D handler's 'inspectController'.
    /// </summary>
    public class AnimationBundleHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            // shared (routed to 3D handler which is superset)
            "setupAnimator",
            "createController",
            "addState",
            "addTransition",
            "addParameter",
            "inspect",
            "listParameters",
            "listStates",
            // 2D-specific (sprite clip management)
            "createClipFromSprites",
            "updateClip",
            "inspectClip",
            // 3D-specific (blend trees, avatar masks)
            "addBlendTree",
            "createAvatarMask",
        };

        private static readonly HashSet<string> Operations2D = new()
        {
            "createClipFromSprites", "updateClip", "inspectClip"
        };

        private readonly Animation2DBundleHandler _handler2D = new();
        private readonly Animation3DBundleHandler _handler3D = new();

        public override string Category => "animationBundle";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            if (Operations2D.Contains(operation))
                return _handler2D.InvokeOperation(operation, payload);

            // Normalize: unified 'addParameter' → 3D handler's 'setParameter'
            if (operation == "addParameter")
                return _handler3D.InvokeOperation("setParameter", payload);

            // Normalize: unified 'inspect' → 3D handler's 'inspect'
            return _handler3D.InvokeOperation(operation, payload);
        }
    }
}
