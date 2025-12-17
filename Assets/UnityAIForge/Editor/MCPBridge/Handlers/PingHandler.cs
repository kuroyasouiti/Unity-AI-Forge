using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Ping handler for verifying bridge connectivity.
    /// Returns basic Unity Editor information to confirm the connection is working.
    /// This handler does not require an 'operation' parameter - it always responds with pong.
    /// </summary>
    public class PingHandler : BaseCommandHandler
    {
        public override string Category => "ping";

        public override IEnumerable<string> SupportedOperations => new[]
        {
            "ping"
        };

        public PingHandler() : base()
        {
        }

        /// <summary>
        /// Override to skip operation validation since ping doesn't require an operation parameter.
        /// </summary>
        protected override void ValidatePayload(Dictionary<string, object> payload)
        {
            // Ping doesn't require any payload validation
            // It accepts empty payloads
        }

        /// <summary>
        /// Override Execute to handle the case where no operation is provided.
        /// </summary>
        public override object Execute(Dictionary<string, object> payload)
        {
            try
            {
                return ExecutePing(payload ?? new Dictionary<string, object>());
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex);
            }
        }

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            // This method won't be called since we override Execute
            return ExecutePing(payload);
        }

        protected override bool RequiresCompilationWait(string operation)
        {
            // Ping is a read-only operation
            return false;
        }

        private object ExecutePing(Dictionary<string, object> payload)
        {
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["message"] = "pong",
                ["unityVersion"] = Application.unityVersion,
                ["productName"] = Application.productName,
                ["platform"] = Application.platform.ToString(),
                ["isPlaying"] = Application.isPlaying,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
    }
}
