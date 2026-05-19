namespace TokenMeter;

/// <summary>
/// Wire format used for tool/function calling in API requests and responses.
/// Determines how tool definitions, invocations, and results are structured.
/// </summary>
public enum ToolCallingFormat
{
    /// <summary>
    /// OpenAI function calling format. Tool invocations appear as
    /// <c>tool_calls</c> in the assistant message; results as <c>tool</c> role messages.
    /// </summary>
    OpenAI = 0,

    /// <summary>
    /// Anthropic tool use format. Tool invocations appear as <c>tool_use</c>
    /// content blocks; results as <c>tool_result</c> content blocks in user messages.
    /// </summary>
    Anthropic,

    /// <summary>
    /// Google Gemini function calling format.
    /// </summary>
    Gemini,
}
